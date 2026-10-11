using ExamPrep.Api.Auth;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>One section of the candidate's exam, with how far the candidate has got in it.</summary>
public sealed record CandidateSectionSummary(int Id, int OrderNo, string Title, int DurationMinutes, string Status);

[ApiController]
[Route("api/candidate/sections")]
[Authorize(Policy = CandidateAuthDefaults.ExamPolicy)]
public sealed class CandidateSectionsController(ExamPrepDbContext db) : ControllerBase
{
    /// <summary>The exam's sections in delivery order (A2: Reading &amp; Writing, then Listening).</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CandidateSectionSummary>> List(CancellationToken cancellationToken)
    {
        var candidateId = User.GetCandidateId();
        var examId = User.GetExamId();

        var sections = await db.ExamSections
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Id, s.OrderNo, s.SectionTemplate!.Title, s.DurationMinutes })
            .ToListAsync(cancellationToken);

        var attemptStatuses = await db.Attempts
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .ToDictionaryAsync(a => a.ExamSectionId, a => a.Status, cancellationToken);

        return sections
            .Select(s => new CandidateSectionSummary(
                s.Id,
                s.OrderNo,
                s.Title,
                s.DurationMinutes,
                attemptStatuses.GetValueOrDefault(s.Id, AttemptStatus.NotStarted).ToString()))
            .ToList();
    }
}
