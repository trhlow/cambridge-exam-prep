using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>Where the candidate stands in a section: the server's clock and every saved answer.</summary>
public sealed record AttemptState(int SectionId, string Status, DateTime? EndsAt, int RemainingSeconds, DateTime ServerNow, IReadOnlyDictionary<int, string> Answers);

public sealed record ResponseItem([Range(1, 1000)] int QuestionNumber, [StringLength(20_000)] string Value);

public sealed record SaveResponsesRequest([Required, MinLength(1), MaxLength(100)] List<ResponseItem> Responses);

/// <summary>
/// Starting a section, reading the clock and saving answers. The clock runs on the server (<c>AttemptClock</c>),
/// so a reload, a crash or a change of computer never gives the candidate more time.
/// </summary>
[ApiController]
[Route("api/candidate/sections/{sectionId:int}")]
[Authorize(Policy = CandidateAuthDefaults.ExamPolicy)]
public sealed class CandidateAttemptsController(ExamPrepDbContext db, CandidateSectionQuery sectionQuery, TimeProvider clock) : ControllerBase
{
    /// <summary>Answers that arrive this soon after the end (network delay) are still kept.</summary>
    private static readonly TimeSpan AutosaveGrace = TimeSpan.FromSeconds(5);

    /// <summary>Starts the section, or returns the running attempt unchanged (reload, other computer).</summary>
    [HttpPost("attempt")]
    public async Task<ActionResult<AttemptState>> Start(int sectionId, CancellationToken cancellationToken)
    {
        var summary = await FindSectionAsync(sectionId, cancellationToken);
        if (summary is null)
        {
            return NotFound();
        }

        if (summary.Status == CandidateSectionQuery.LockedStatus)
        {
            return Problem(title: CandidateSectionsController.LockedTitle, statusCode: StatusCodes.Status403Forbidden);
        }

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        var now = Now();
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            attempt = new Attempt { CandidateId = candidate.Id, ExamSectionId = section.Id };
            AttemptExpiry.ClockFor(attempt, section, candidate).Start(candidate.PlannedExtraPercent, now);
            db.Attempts.Add(attempt);
        }
        else
        {
            AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await StateAsync(attempt, section, candidate, now, cancellationToken);
    }

    /// <summary>The clock and saved answers; ends the section first if its time is up.</summary>
    [HttpGet("attempt")]
    public async Task<ActionResult<AttemptState>> Get(int sectionId, CancellationToken cancellationToken)
    {
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return NotFound();
        }

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        var now = Now();
        if (AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now))
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await StateAsync(attempt, section, candidate, now, cancellationToken);
    }

    /// <summary>Saves (inserts or replaces) the candidate's answers while the section is running.</summary>
    [HttpPut("responses")]
    public async Task<IActionResult> Save(int sectionId, SaveResponsesRequest request, CancellationToken cancellationToken)
    {
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return Problem(title: "This part of the test has not started.", statusCode: StatusCodes.Status409Conflict);
        }

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        var now = Now();
        if (AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now - AutosaveGrace))
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            var title = attempt.Status == AttemptStatus.Paused ? "Your test is paused." : "This part of the test has finished.";
            return Problem(title: title, statusCode: StatusCodes.Status409Conflict);
        }

        var questionIds = await db.Questions
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == section.TestPaperId)
            .ToDictionaryAsync(q => q.Number, q => q.Id, cancellationToken);
        var unknown = request.Responses.Select(r => r.QuestionNumber).Where(n => !questionIds.ContainsKey(n)).Distinct().ToList();
        if (unknown.Count > 0)
        {
            return Problem(title: $"Question {string.Join(", ", unknown)} is not in this part of the test.", statusCode: StatusCodes.Status400BadRequest);
        }

        await UpsertAsync(attempt, request.Responses, questionIds, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task UpsertAsync(Attempt attempt, List<ResponseItem> items, Dictionary<int, int> questionIds, DateTime now, CancellationToken cancellationToken)
    {
        var latest = items.GroupBy(r => r.QuestionNumber).ToDictionary(g => questionIds[g.Key], g => g.Last().Value);
        var ids = latest.Keys.ToList();
        var existing = await db.Responses
            .Where(r => r.AttemptId == attempt.Id && ids.Contains(r.QuestionId))
            .ToDictionaryAsync(r => r.QuestionId, cancellationToken);

        foreach (var (questionId, value) in latest)
        {
            if (existing.TryGetValue(questionId, out var response))
            {
                response.Value = value;
                response.UpdatedAt = now;
            }
            else
            {
                db.Responses.Add(new Response { AttemptId = attempt.Id, QuestionId = questionId, Value = value, UpdatedAt = now });
            }
        }
    }

    private async Task<AttemptState> StateAsync(Attempt attempt, ExamSection section, Candidate candidate, DateTime now, CancellationToken cancellationToken)
    {
        var answers = await db.Responses
            .AsNoTracking()
            .Where(r => r.AttemptId == attempt.Id && r.Value != null)
            .Select(r => new { r.Question!.Number, r.Value })
            .ToDictionaryAsync(r => r.Number, r => r.Value!, cancellationToken);
        var remaining = AttemptExpiry.ClockFor(attempt, section, candidate).Remaining(now);

        return new AttemptState(section.Id, attempt.Status.ToString(), attempt.EndsAt, (int)Math.Ceiling(remaining.TotalSeconds), now, answers);
    }

    private async Task<CandidateSectionSummary?> FindSectionAsync(int sectionId, CancellationToken cancellationToken) =>
        (await sectionQuery.LoadAsync(User.GetCandidateId(), User.GetExamId(), cancellationToken)).SingleOrDefault(s => s.Id == sectionId);

    private Task<Attempt?> FindAttemptAsync(int sectionId, CancellationToken cancellationToken)
    {
        var candidateId = User.GetCandidateId();
        var examId = User.GetExamId();
        return db.Attempts.SingleOrDefaultAsync(
            a => a.CandidateId == candidateId && a.ExamSectionId == sectionId && a.ExamSection!.ExamId == examId,
            cancellationToken);
    }

    private async Task<(ExamSection Section, Candidate Candidate)> LoadSectionAndCandidateAsync(int sectionId, CancellationToken cancellationToken)
    {
        var candidateId = User.GetCandidateId();
        var section = await db.ExamSections.AsNoTracking().SingleAsync(s => s.Id == sectionId, cancellationToken);
        var candidate = await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidateId, cancellationToken);
        return (section, candidate);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
