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
