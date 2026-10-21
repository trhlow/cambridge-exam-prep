using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Monitoring;
using ExamPrep.Api.Realtime;
using ExamPrep.Api.Security;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>Where the candidate stands in a section: the server's clock, every saved answer and the invigilator's messages.</summary>
/// <param name="AudioPositionMs">Listening only: where the recording is now; null until the candidate presses Play.</param>
/// <param name="Selections">"Answering this question?" answers other than Undecided (B1 Writing Part 2), by question number.</param>
public sealed record AttemptState(
    int SectionId,
    string Status,
    DateTime? EndsAt,
    int RemainingSeconds,
    DateTime ServerNow,
    IReadOnlyDictionary<int, string> Answers,
    int? AudioPositionMs,
    IReadOnlyList<CandidateMessageView> Messages,
    IReadOnlyDictionary<int, string>? Selections = null);

public sealed record ResponseItem([Range(1, 1000)] int QuestionNumber, [StringLength(20_000)] string Value);

public sealed record SaveResponsesRequest([Required, MinLength(1), MaxLength(100)] List<ResponseItem> Responses);

/// <param name="Selection">Yes, No or Undecided.</param>
public sealed record SelectionRequest([Range(1, 1000)] int QuestionNumber, [Required] string Selection);

/// <summary>
/// Starting a section, reading the clock and saving answers. The clock runs on the server (<c>AttemptClock</c>),
/// so a reload, a crash or a change of computer never gives the candidate more time.
/// </summary>
[ApiController]
[Route("api/candidate/sections/{sectionId:int}")]
[Authorize(Policy = CandidateAuthDefaults.ExamPolicy)]
public sealed class CandidateAttemptsController(ExamPrepDbContext db, CandidateSectionQuery sectionQuery, PaperAssignment papers, TimeProvider clock, RoomNotifier notifier, SecurityEventRecorder events) : ControllerBase
{
    /// <summary>Answers that arrive this soon after the end (network delay) are still kept.</summary>
    private static readonly TimeSpan AutosaveGrace = TimeSpan.FromSeconds(5);

    /// <summary>The browser's clock in Unix milliseconds, sent with every heartbeat (VE-29).</summary>
    public const string ClientTimeHeader = "X-Client-Time";

    /// <summary>A computer clock further than this from the server's raises a warning.</summary>
    private static readonly TimeSpan ClockSkewLimit = TimeSpan.FromMinutes(2);

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
        var attempt = await FindAttemptAsync(sectionId, cancellationToken) ?? await TryCreateAttemptAsync(section, candidate, now, cancellationToken);
        if (attempt is null)
        {
            // Another request (double click, second tab) created it a moment ago: carry on with that one.
            attempt = await FindAttemptAsync(sectionId, cancellationToken) ?? throw new InvalidOperationException("Attempt vanished after a duplicate insert.");
        }

        if (AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now))
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        notifier.SectionChanged(sectionId);
        return await StateAsync(attempt, section, candidate, now, cancellationToken);
    }

    /// <summary>Starts the clock; null when a concurrent request has already started this section.</summary>
    private async Task<Attempt?> TryCreateAttemptAsync(ExamSection section, Candidate candidate, DateTime now, CancellationToken cancellationToken)
    {
        // The paper is chosen once, here: a later change of seat or computer keeps it.
        var paperId = await papers.PaperIdForAsync(section, candidate, cancellationToken);
        var attempt = new Attempt { CandidateId = candidate.Id, ExamSectionId = section.Id, TestPaperId = paperId };
        AttemptExpiry.ClockFor(attempt, section, candidate).Start(candidate.PlannedExtraPercent, now);
        db.Attempts.Add(attempt);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return attempt;
        }
        catch (DbUpdateException)
        {
            db.Entry(attempt).State = EntityState.Detached;
            if (await db.Attempts.AnyAsync(a => a.CandidateId == candidate.Id && a.ExamSectionId == section.Id, cancellationToken))
            {
                return null;
            }

            throw;
        }
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

    /// <summary>
    /// Sent every few seconds while the section is open: marks the candidate online for the invigilator and returns
    /// the clock, so a pause, an end-time change, a rewind or a message reaches the candidate within seconds.
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<AttemptState>> Heartbeat(int sectionId, CancellationToken cancellationToken)
    {
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return NotFound();
        }

        var now = Now();
        if (!InvigilatorMonitorController.IsOnline(attempt, now))
        {
            // Back online (or online for the first time): the invigilator sees it without waiting for a poll.
            notifier.SectionChanged(sectionId);
        }

        // A direct update: the heartbeat must not rotate the attempt's concurrency stamp and clash with timer changes.
        await db.Attempts.Where(a => a.Id == attempt.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.LastHeartbeatAt, now).SetProperty(a => a.ConnectionStatus, ConnectionStatus.Online), cancellationToken);

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        if (AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now))
        {
            await db.SaveChangesAsync(cancellationToken);
            notifier.SectionChanged(sectionId);
        }

        await CheckClockAsync(attempt, now, cancellationToken);

        return await StateAsync(attempt, section, candidate, now, cancellationToken);
    }

    /// <summary>
    /// Listening: the candidate pressed Play. The recording starts once; calling again (after a reload or on
    /// another computer) leaves it running where it is, so it can never be heard from the start again.
    /// </summary>
    [HttpPost("audio")]
    public async Task<ActionResult<AttemptState>> PlayAudio(int sectionId, CancellationToken cancellationToken)
    {
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return Problem(title: "This part of the test has not started.", statusCode: StatusCodes.Status409Conflict);
        }

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        var hasAudio = await db.PaperParts.AnyAsync(p => p.TestPaperId == attempt.TestPaperId && p.AudioAssetId != null, cancellationToken);
        if (!hasAudio)
        {
            return Problem(title: "This part of the test has no recording.", statusCode: StatusCodes.Status400BadRequest);
        }

        var now = Now();
        AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now);
        if (attempt.Status != AttemptStatus.InProgress)
        {
            return Problem(title: "The recording can only start while you are working on the test.", statusCode: StatusCodes.Status409Conflict);
        }

        if (attempt.AudioStartedAt is null)
        {
            AttemptExpiry.ClockFor(attempt, section, candidate).StartAudio(now);
        }

        await db.SaveChangesAsync(cancellationToken);
        notifier.SectionChanged(sectionId);
        return await StateAsync(attempt, section, candidate, now, cancellationToken);
    }

    /// <summary>Saves (inserts or replaces) the candidate's answers while the section is running.</summary>
    [HttpPut("responses")]
    public async Task<IActionResult> Save(int sectionId, SaveResponsesRequest request, CancellationToken cancellationToken)
    {
        var (attempt, refusal, now) = await RunningAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return refusal!;
        }

        var questionIds = await db.Questions
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId)
            .ToDictionaryAsync(q => q.Number, q => q.Id, cancellationToken);
        var unknown = request.Responses.Select(r => r.QuestionNumber).Where(n => !questionIds.ContainsKey(n)).Distinct().ToList();
        if (unknown.Count > 0)
        {
            return Problem(title: $"Question {string.Join(", ", unknown)} is not in this part of the test.", statusCode: StatusCodes.Status400BadRequest);
        }

        await UpsertAsync(attempt, request.Responses, questionIds, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        notifier.SectionChanged(sectionId);
        return NoContent();
    }

    /// <summary>
    /// "Answering this question?" (B1 Writing Part 2): Yes, No or Undecided for a question of a group where the
    /// candidate answers only some questions. Refused (409) when Yes would choose more questions than the group allows.
    /// </summary>
    [HttpPut("selections")]
    public async Task<IActionResult> Select(int sectionId, SelectionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<QuestionSelection>(request.Selection, out var selection) || !Enum.IsDefined(selection))
        {
            return Problem(title: "Choose Yes, No or Undecided.", statusCode: StatusCodes.Status400BadRequest);
        }

        var (attempt, refusal, now) = await RunningAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return refusal!;
        }

        var question = await db.Questions.Include(q => q.QuestionGroup!).ThenInclude(g => g.Questions)
            .SingleOrDefaultAsync(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId && q.Number == request.QuestionNumber, cancellationToken);
        if (question?.QuestionGroup!.ChooseCount is not { } chooseCount)
        {
            return Problem(title: $"Question {request.QuestionNumber} is not one to choose.", statusCode: StatusCodes.Status400BadRequest);
        }

        var groupIds = question.QuestionGroup.Questions.Select(q => q.Id).ToList();
        var responses = await db.Responses.Where(r => r.AttemptId == attempt.Id && groupIds.Contains(r.QuestionId)).ToListAsync(cancellationToken);
        if (selection == QuestionSelection.Yes && responses.Count(r => r.QuestionId != question.Id && r.Selection == QuestionSelection.Yes) >= chooseCount)
        {
            return Problem(title: $"You can answer only {chooseCount} of these questions. Change your other choice first.", statusCode: StatusCodes.Status409Conflict);
        }

        var response = responses.SingleOrDefault(r => r.QuestionId == question.Id);
        if (response is null)
        {
            response = new Response { AttemptId = attempt.Id, QuestionId = question.Id, UpdatedAt = now };
            db.Responses.Add(response);
        }

        response.Selection = selection;
        response.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        notifier.SectionChanged(sectionId);
        return NoContent();
    }

    /// <summary>The candidate's attempt when answers may be saved now; otherwise why not (409).</summary>
    private async Task<(Attempt? Attempt, IActionResult? Refusal, DateTime Now)> RunningAttemptAsync(int sectionId, CancellationToken cancellationToken)
    {
        var now = Now();
        var attempt = await FindAttemptAsync(sectionId, cancellationToken);
        if (attempt is null)
        {
            return (null, Problem(title: "This part of the test has not started.", statusCode: StatusCodes.Status409Conflict), now);
        }

        var (section, candidate) = await LoadSectionAndCandidateAsync(sectionId, cancellationToken);
        if (AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now - AutosaveGrace))
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            var title = attempt.Status == AttemptStatus.Paused ? "Your test is paused." : "This part of the test has finished.";
            return (null, Problem(title: title, statusCode: StatusCodes.Status409Conflict), now);
        }

        return (attempt, null, now);
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

    /// <summary>
    /// The candidate's own clock does not decide anything, but a computer set far off is worth the invigilator's look
    /// (Cambridge: an accurate clock in the room; a wrong clock can hide a wrong time zone or a tampered machine).
    /// </summary>
    private async Task CheckClockAsync(Attempt attempt, DateTime now, CancellationToken cancellationToken)
    {
        if (attempt.Status == AttemptStatus.Submitted
            || !long.TryParse(Request.Headers[ClientTimeHeader].FirstOrDefault(), out var clientMs)
            || clientMs <= 0)
        {
            return;
        }

        var skew = DateTimeOffset.FromUnixTimeMilliseconds(clientMs).UtcDateTime - now;
        if (skew.Duration() > ClockSkewLimit)
        {
            var minutes = (int)Math.Round(skew.Duration().TotalMinutes);
            await events.RecordAsync(attempt.Id, attempt.ExamSectionId, SecurityEventType.ClockSkew,
                $"The computer's clock is {minutes} min {(skew > TimeSpan.Zero ? "ahead of" : "behind")} the server.", TimeSpan.FromMinutes(30), cancellationToken);
        }
    }

    private async Task<AttemptState> StateAsync(Attempt attempt, ExamSection section, Candidate candidate, DateTime now, CancellationToken cancellationToken)
    {
        var saved = await db.Responses
            .AsNoTracking()
            .Where(r => r.AttemptId == attempt.Id && (r.Value != null || r.Selection != QuestionSelection.Undecided))
            .Select(r => new { r.Question!.Number, r.Value, r.Selection })
            .ToListAsync(cancellationToken);
        var answers = saved.Where(r => r.Value is not null).ToDictionary(r => r.Number, r => r.Value!);
        var selections = saved.Where(r => r.Selection != QuestionSelection.Undecided).ToDictionary(r => r.Number, r => r.Selection.ToString());
        var attemptClock = AttemptExpiry.ClockFor(attempt, section, candidate);
        var remaining = attemptClock.Remaining(now);
        int? audioPosition = attempt.AudioStartedAt is null ? null : (int)attemptClock.AudioPosition(now).TotalMilliseconds;

        var messages = await CandidateMessages.ForCandidateAsync(db, section.ExamId, candidate.Id, cancellationToken);
        return new AttemptState(section.Id, attempt.Status.ToString(), attempt.EndsAt, (int)Math.Ceiling(remaining.TotalSeconds), now, answers, audioPosition, messages, selections);
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

    /// <summary>Now, to the millisecond, so times read back from MySQL equal the ones just written.</summary>
    private DateTime Now()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
