using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Infrastructure.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPrep.Api.Content;

/// <summary>The upload: a title, the two PDFs and, for Listening, one track per part.</summary>
public sealed class PaperConversionForm
{
    public string? Title { get; set; }
    public IFormFile? Questions { get; set; }
    public IFormFile? AnswerKey { get; set; }
    public IFormFile? Audio1 { get; set; }
    public IFormFile? Audio2 { get; set; }
    public IFormFile? Audio3 { get; set; }
    public IFormFile? Audio4 { get; set; }
    public IFormFile? Audio5 { get; set; }

    public IEnumerable<(int Part, IFormFile File)> Audio() =>
        new[] { Audio1, Audio2, Audio3, Audio4, Audio5 }
            .Select((file, index) => (Part: index + 1, File: file))
            .Where(item => item.File is { Length: > 0 })
            .Select(item => (item.Part, item.File!));
}

/// <summary>A converted and imported draft, with the points the converter asks a person to check.</summary>
public sealed record PaperConversionResult(int Id, string Title, string ExamType, string Section, string Status, int Parts, int Questions, IReadOnlyList<string> Checks);

/// <summary>
/// Imports a paper straight from the Cambridge PDFs: the server runs setup/exam-package/pdf2package.py, then imports
/// the folder it writes exactly as an uploaded package. The administrator reviews the draft before publishing.
/// </summary>
[ApiController]
[Route("api/admin/papers")]
[Authorize(Policy = StaffAuthDefaults.PapersPolicy)]
public sealed class AdminPaperConversionController(IPaperConverter converter, PaperPackageImporter importer, ILogger<AdminPaperConversionController> logger) : ControllerBase
{
    private const int MaxTitleLength = 256;
    private static readonly string[] AudioExtensions = [".mp3", ".m4a", ".ogg", ".wav"];
    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    /// <summary>Conversions are CPU-heavy and rare: one at a time.</summary>
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    [HttpPost("convert")]
    [RequestSizeLimit(AdminPapersController.MaxPackageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AdminPapersController.MaxPackageBytes)]
    public async Task<ActionResult<PaperConversionResult>> Convert([FromForm] PaperConversionForm form, CancellationToken cancellationToken)
    {
        var problems = await CheckAsync(form, cancellationToken);
        if (problems.Count > 0)
        {
            return Problems.Invalid("Check the uploaded files.", problems);
        }

        var work = Directory.CreateTempSubdirectory("examprep-convert-").FullName;
        await OneAtATime.WaitAsync(cancellationToken);
        try
        {
            // Every name on disk is ours; nothing from the upload's file names reaches a path.
            var input = new PaperConversionInput(
                Path.Combine(work, "questions.pdf"),
                Path.Combine(work, "answer-key.pdf"),
                Directory.CreateDirectory(Path.Combine(work, "audio")).FullName,
                form.Title!.Trim(),
                Path.Combine(work, "package"));
            await SaveAsync(form.Questions!, input.QuestionsPdf, cancellationToken);
            await SaveAsync(form.AnswerKey!, input.AnswerKeyPdf, cancellationToken);
            foreach (var (part, file) in form.Audio())
            {
                await SaveAsync(file, Path.Combine(input.AudioFolder, $"part{part}{Path.GetExtension(file.FileName).ToLowerInvariant()}"), cancellationToken);
            }

            PaperConversionReport report;
            try
            {
                report = await converter.ConvertAsync(input, cancellationToken);
            }
            catch (PaperConverterUnavailableException e)
            {
                logger.LogWarning("PDF conversion unavailable: {Reason}", e.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = e.Message });
            }

            if (report.Errors.Count > 0 || !System.IO.File.Exists(Path.Combine(input.OutputFolder, PaperPackageReader.PaperFileName)))
            {
                logger.LogInformation("PDF conversion of {Title} found {ErrorCount} errors", input.Title, report.Errors.Count);
                return WithChecks(Problems.Invalid("The PDF could not be converted.", report.Errors.Count > 0 ? report.Errors : ["The converter wrote no paper."]), report.Checks);
            }

            var result = await importer.ImportAsync(new MemoryStream(PackageFolder.Zip(input.OutputFolder, fillMissing: false)), User.GetStaffId(), cancellationToken);
            if (result.Errors.Count > 0)
            {
                return WithChecks(Problems.Invalid("The converted paper is invalid.", result.Errors), report.Checks);
            }

            var paper = result.Paper!;
            logger.LogInformation("Staff {UserId} converted and imported paper {PaperId} ({Title})", User.GetStaffId(), paper.Id, paper.Title);
            return Created($"/api/admin/papers/{paper.Id}", new PaperConversionResult(
                paper.Id,
                paper.Title,
                result.ExamType!.Code,
                result.Section!.Title,
                paper.Status.ToString(),
                paper.Parts.Count,
                paper.Parts.SelectMany(p => p.Groups).Sum(g => g.Questions.Count),
                report.Checks));
        }
        finally
        {
            OneAtATime.Release();
            try
            {
                Directory.Delete(work, recursive: true);
            }
            catch (IOException e)
            {
                logger.LogWarning(e, "Could not delete the conversion folder {Folder}", work);
            }
        }
    }

    private static async Task<List<string>> CheckAsync(PaperConversionForm form, CancellationToken cancellationToken)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(form.Title) || form.Title.Trim().Length > MaxTitleLength)
        {
            problems.Add($"Give the paper a title (at most {MaxTitleLength} characters).");
        }

        await CheckPdfAsync(form.Questions, "question paper", problems, cancellationToken);
        await CheckPdfAsync(form.AnswerKey, "answer key", problems, cancellationToken);
        foreach (var (part, file) in form.Audio())
        {
            if (!AudioExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            {
                problems.Add($"The Part {part} track must be an .mp3, .m4a, .ogg or .wav file.");
            }
        }

        return problems;
    }

    private static async Task CheckPdfAsync(IFormFile? file, string name, List<string> problems, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            problems.Add($"Choose the {name} (PDF).");
            return;
        }

        var start = new byte[PdfMagic.Length];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(start, start.Length, throwOnEndOfStream: false, cancellationToken);
        if (read < start.Length || !start.AsSpan().SequenceEqual(PdfMagic))
        {
            problems.Add($"The {name} must be a PDF file.");
        }
    }

    private static async Task SaveAsync(IFormFile file, string path, CancellationToken cancellationToken)
    {
        await using var target = System.IO.File.Create(path);
        await file.CopyToAsync(target, cancellationToken);
    }

    private static BadRequestObjectResult WithChecks(BadRequestObjectResult result, IReadOnlyList<string> checks)
    {
        ((ProblemDetails)result.Value!).Extensions["checks"] = checks;
        return result;
    }
}
