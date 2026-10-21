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
public sealed class CandidateSectionsController(ExamPrepDbContext db, CandidateSectionQuery sectionQuery, PaperAssignment papers) : ControllerBase
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
        // The started attempt's paper; before that, the one the candidate's seat will get.
        var paperId = await db.Attempts.Where(a => a.CandidateId == User.GetCandidateId() && a.ExamSectionId == sectionId).Select(a => (int?)a.TestPaperId).SingleOrDefaultAsync(cancellationToken)
            ?? await papers.PaperIdForAsync(section, await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == User.GetCandidateId(), cancellationToken), cancellationToken);
        var paper = await db.TestPapers
            .AsNoTracking()
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions)
            .AsSplitQuery()
            .SingleAsync(p => p.Id == paperId, cancellationToken);

        return CandidatePaperMapper.From(section, paper);
    }
}
