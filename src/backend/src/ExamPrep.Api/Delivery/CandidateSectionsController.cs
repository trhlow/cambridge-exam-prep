using ExamPrep.Api.Auth;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// One section of the candidate's exam with how far the candidate has got in it: an attempt status, or
/// <c>Locked</c> while an earlier section has not ended.
/// </summary>
public sealed record CandidateSectionSummary(int Id, int OrderNo, string Title, int DurationMinutes, string Status);

[ApiController]
[Route("api/candidate/sections")]
[Authorize(Policy = CandidateAuthDefaults.ExamPolicy)]
public sealed class CandidateSectionsController(ExamPrepDbContext db, CandidateSectionQuery sectionQuery) : ControllerBase
{
    public const string LockedTitle = "This part of the test opens when the previous part has finished.";

    /// <summary>The exam's sections in delivery order (A2: Reading &amp; Writing, then Listening).</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CandidateSectionSummary>> List(CancellationToken cancellationToken) =>
        await sectionQuery.LoadAsync(User.GetCandidateId(), User.GetExamId(), cancellationToken);

    /// <summary>
    /// The paper of one of the candidate's sections, without answer keys. 404 for any other exam's section,
    /// 403 while an earlier section is still running.
    /// </summary>
    [HttpGet("{sectionId:int}/paper")]
    public async Task<ActionResult<CandidatePaper>> Paper(int sectionId, CancellationToken cancellationToken)
    {
        var sections = await sectionQuery.LoadAsync(User.GetCandidateId(), User.GetExamId(), cancellationToken);
        var summary = sections.SingleOrDefault(s => s.Id == sectionId);
        if (summary is null)
        {
            return NotFound();
        }

        if (summary.Status == CandidateSectionQuery.LockedStatus)
        {
            return Problem(title: LockedTitle, statusCode: StatusCodes.Status403Forbidden);
        }

        var section = await db.ExamSections
            .AsNoTracking()
            .Include(s => s.SectionTemplate)
            .SingleAsync(s => s.Id == sectionId, cancellationToken);
        var paper = await db.TestPapers
            .AsNoTracking()
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions)
            .AsSplitQuery()
            .SingleAsync(p => p.Id == section.TestPaperId, cancellationToken);

        return CandidatePaperMapper.From(section, paper);
    }
}
