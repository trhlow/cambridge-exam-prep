using ExamPrep.Api.Approvals;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

/// <summary>What the administrator sees after a successful import.</summary>
public sealed record PaperImportSummary(int Id, string Title, string ExamType, string Section, string Status, int Parts, int Questions);

/// <summary>A paper in the library, with how many sessions use it (those cannot lose it).</summary>
/// <summary>VE-25: a new paper with the exam's layout and every text, picture and recording still to write.</summary>
public sealed record BlankPaperRequest(int SectionTemplateId, [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(256, MinimumLength = 1)] string Title);

/// <summary>A section a blank paper can be started for.</summary>
public sealed record BlankLayoutView(int SectionTemplateId, string ExamType, string Section);

public sealed record PaperListItem(int Id, string Title, string ExamType, int SectionTemplateId, string Section, string Status, int Parts, int Questions, DateTime CreatedAt, int UsedByExams);

[ApiController]
[Route("api/admin/papers")]
[Authorize(Policy = StaffAuthDefaults.PapersPolicy)]
public sealed class AdminPapersController(PaperPackageImporter importer, ExamPrepDbContext db, ILogger<AdminPapersController> logger) : ControllerBase
{
    /// <summary>Five Listening tracks plus pictures fit comfortably; the unpacked size is limited separately by the reader.</summary>
    public const long MaxPackageBytes = 300L * 1024 * 1024;

    /// <summary>
    /// Imports an exam package (.zip with paper.json, pictures and audio) as a draft paper. An invalid package
    /// creates nothing and returns every problem found in <c>errors</c>.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(MaxPackageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxPackageBytes)]
    public async Task<ActionResult<PaperImportSummary>> Import(IFormFile? package, CancellationToken cancellationToken)
    {
        if (package is null || package.Length == 0)
        {
            return InvalidPackage(["Choose an exam package (.zip) to upload."]);
        }

        await using var stream = package.OpenReadStream();
        var result = await importer.ImportAsync(stream, User.GetStaffId(), cancellationToken);
        if (result.Errors.Count > 0)
        {
            logger.LogInformation("Exam package {FileName} rejected with {ErrorCount} errors", package.FileName, result.Errors.Count);
            return InvalidPackage(result.Errors);
        }

        var summary = Summary(result);
        logger.LogInformation("Imported paper {PaperId} ({Title}) from {FileName}", summary.Id, summary.Title, package.FileName);
        return Created($"/api/admin/papers/{summary.Id}", summary);
    }

    /// <summary>
    /// VE-25: writing a paper without a package. The draft has the exam's parts, question types and numbering, with
    /// placeholder texts, pictures and recordings to replace in the editor; publishing refuses it until they are gone.
    /// </summary>
    [HttpGet("blank-layouts")]
    public async Task<IReadOnlyList<BlankLayoutView>> BlankLayouts(CancellationToken cancellationToken)
    {
        var templates = await db.SectionTemplates.AsNoTracking().Include(t => t.ExamType).OrderBy(t => t.ExamTypeId).ThenBy(t => t.OrderNo).ToListAsync(cancellationToken);
        return templates.Where(t => PaperSkeletons.Has(t.ExamType!.Code, t.OrderNo)).Select(t => new BlankLayoutView(t.Id, t.ExamType!.Name, t.Title)).ToList();
    }

    [HttpPost("blank")]
    public async Task<ActionResult<PaperImportSummary>> Blank(BlankPaperRequest request, CancellationToken cancellationToken)
    {
        var template = await db.SectionTemplates.AsNoTracking().Include(t => t.ExamType).SingleOrDefaultAsync(t => t.Id == request.SectionTemplateId, cancellationToken);
        if (template is null)
        {
            return Problems.Invalid("Choose the exam and section of the paper.");
        }

        var zip = PaperSkeletons.Zip(template.ExamType!.Code, template.OrderNo, request.Title.Trim());
        if (zip is null)
        {
            return Problems.Invalid($"There is no blank layout for {template.ExamType.Name} {template.Title} yet. Import a package or the PDFs instead.");
        }

        var result = await importer.ImportAsync(new MemoryStream(zip), User.GetStaffId(), cancellationToken);
        if (result.Errors.Count > 0)
        {
            logger.LogError("The blank layout for section template {TemplateId} is invalid: {Errors}", template.Id, string.Join(" | ", result.Errors));
            return InvalidPackage(result.Errors);
        }

        var summary = Summary(result);
        logger.LogInformation("Staff {UserId} started blank paper {PaperId} ({Title})", User.GetStaffId(), summary.Id, summary.Title);
        return Created($"/api/admin/papers/{summary.Id}", summary);
    }

    private static PaperImportSummary Summary(PaperImportResult result)
    {
        var paper = result.Paper!;
        return new PaperImportSummary(
            paper.Id,
            paper.Title,
            result.ExamType!.Code,
            result.Section!.Title,
            paper.Status.ToString(),
            paper.Parts.Count,
            paper.Parts.SelectMany(p => p.Groups).Sum(g => g.Questions.Count));
    }

    /// <summary>What still comes from a blank layout: marked texts and placeholder pictures or recordings.</summary>
    private async Task<List<string>> UnwrittenAsync(int paperId, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers.AsNoTracking()
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.AnswerKeys)
            .AsSplitQuery()
            .SingleAsync(p => p.Id == paperId, cancellationToken);
        var placeholderHashes = PaperSkeletons.PlaceholderHashes.ToList();
        var placeholderIds = await db.Assets.AsNoTracking()
            .Where(a => placeholderHashes.Contains(a.Sha256))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        bool Unwritten(string? text) => text?.Contains(PaperSkeletons.Marker, StringComparison.Ordinal) == true;
        bool Placeholder(string? html) => html is not null && placeholderIds.Any(id => html.Contains($"{PaperPackageImporter.AssetUrlPrefix}{id}\"", StringComparison.Ordinal));

        var problems = new List<string>();
        foreach (var part in paper.Parts.OrderBy(p => p.PartNo))
        {
            var label = $"Part {part.PartNo}";
            if (Unwritten(part.Title) || Unwritten(part.StimulusHtml))
            {
                problems.Add($"{label}: the title or passage is still to write.");
            }

            if (part.AudioAssetId is { } audio && placeholderIds.Contains(audio))
            {
                problems.Add($"{label}: the recording is still the placeholder.");
            }

            foreach (var group in part.Groups)
            {
                if (Unwritten(group.Instructions) || Unwritten(group.BodyHtml) || Unwritten(group.OptionPoolJson))
                {
                    problems.Add($"{label}: the instructions, text or cards are still to write.");
                }

                foreach (var question in group.Questions.OrderBy(q => q.Number))
                {
                    var stored = question.OptionsJson is null ? [] : System.Text.Json.JsonSerializer.Deserialize<List<StoredOptionImage>>(question.OptionsJson, System.Text.Json.JsonSerializerOptions.Web)!;
                    if (Unwritten(question.PromptHtml) || Unwritten(question.StimulusHtml) || Unwritten(question.OptionsJson) || question.AnswerKeys.Any(k => Unwritten(k.Value)))
                    {
                        problems.Add($"Question {question.Number}: the wording, options or answer are still to write.");
                    }
                    else if (stored.Any(o => o.ImageAssetId is { } id && placeholderIds.Contains(id)) || Placeholder(question.StimulusHtml))
                    {
                        problems.Add($"Question {question.Number}: a picture is still the placeholder.");
                    }
                }
            }
        }

        return problems;
    }

    private sealed record StoredOptionImage(int? ImageAssetId);

    [HttpGet]
    public async Task<IReadOnlyList<PaperListItem>> List(CancellationToken cancellationToken)
    {
        var papers = await PaperRows(db, db.TestPapers).ToListAsync(cancellationToken);
        return papers.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).ToList();
    }

    /// <summary>Every part and question with its answer key, pictures and audio, for review before publishing.</summary>
    [HttpGet("{paperId:int}")]
    public async Task<ActionResult<PaperDetail>> Detail(int paperId, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers.AsNoTracking()
            .Include(p => p.SectionTemplate!).ThenInclude(t => t.ExamType)
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.AnswerKeys)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        return paper is null ? NotFound() : PaperDetailMapper.From(paper);
    }

    /// <summary>A reviewed draft becomes available for scheduling. Publishing twice changes nothing.</summary>
    [HttpPost("{paperId:int}/publish")]
    public async Task<ActionResult<PaperListItem>> Publish(int paperId, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers.SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper is null)
        {
            return NotFound();
        }

        if (paper.Status != PaperStatus.Published)
        {
            var unwritten = await UnwrittenAsync(paperId, cancellationToken);
            if (unwritten.Count > 0)
            {
                return Problems.Invalid("The paper still has places to write.", unwritten.Take(20).ToList());
            }

            paper.Status = PaperStatus.Published;
            paper.PublishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Staff {UserId} published paper {PaperId}", User.GetStaffId(), paperId);
        }

        return await PaperRows(db, db.TestPapers.Where(p => p.Id == paperId)).SingleAsync(cancellationToken);
    }

    /// <summary>
    /// VE-60: asks for the paper (with its parts, questions and keys) to be deleted; an administrator other than the
    /// requester approves it. Refused at once while a session uses the paper.
    /// </summary>
    [HttpDelete("{paperId:int}")]
    public async Task<ActionResult<ApprovalView>> Delete(int paperId, [FromServices] ApprovalService approvals, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers.AsNoTracking().SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper is null)
        {
            return NotFound();
        }

        if (await ApprovalActions.PaperProblemAsync(db, paperId, cancellationToken) is { } problem)
        {
            return Problems.Conflict(problem);
        }

        var (request, conflict) = await approvals.RequestAsync(ApprovalKind.DeletePaper, paperId, $"Delete paper “{paper.Title}”", null, User.GetStaffId(), cancellationToken);
        return request is null ? Problems.Conflict(conflict!) : Accepted(request);
    }

    internal static IQueryable<PaperListItem> PaperRows(ExamPrepDbContext db, IQueryable<TestPaper> papers) =>
        papers.AsNoTracking().Select(p => new PaperListItem(
            p.Id,
            p.Title,
            p.SectionTemplate!.ExamType!.Name,
            p.SectionTemplateId,
            p.SectionTemplate.Title,
            p.Status.ToString(),
            p.Parts.Count,
            db.Questions.Count(q => q.QuestionGroup!.PaperPart!.TestPaperId == p.Id),
            p.CreatedAt,
            db.ExamSections.Where(s => s.TestPaperId == p.Id).Select(s => s.ExamId).Distinct().Count()));

    private BadRequestObjectResult InvalidPackage(IReadOnlyList<string> errors)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The exam package is invalid.",
        };
        problem.Extensions["errors"] = errors;
        return BadRequest(problem);
    }
}
