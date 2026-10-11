using ExamPrep.Api.Auth;
using ExamPrep.Infrastructure.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPrep.Api.Content;

/// <summary>What the administrator sees after a successful import.</summary>
public sealed record PaperImportSummary(int Id, string Title, string ExamType, string Section, string Status, int Parts, int Questions);

[ApiController]
[Route("api/admin/papers")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminPapersController(PaperPackageImporter importer, ILogger<AdminPapersController> logger) : ControllerBase
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

        var paper = result.Paper!;
        var summary = new PaperImportSummary(
            paper.Id,
            paper.Title,
            result.ExamType!.Code,
            result.Section!.Title,
            paper.Status.ToString(),
            paper.Parts.Count,
            paper.Parts.SelectMany(p => p.Groups).Sum(g => g.Questions.Count));
        logger.LogInformation("Imported paper {PaperId} ({Title}) from {FileName}", paper.Id, paper.Title, package.FileName);
        return Created($"/api/admin/papers/{paper.Id}", summary);
    }

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
