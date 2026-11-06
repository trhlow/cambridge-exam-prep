using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Api.Delivery;
using ExamPrep.Api.Exams;
using ExamPrep.Api.Realtime;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Monitoring;

/// <summary>One candidate on the monitoring page of a section.</summary>
/// <param name="EndTimeEdited">End now / End at / End in is in force (Use test end time clears it).</param>
/// <param name="Answered">Questions with a saved answer.</param>
public sealed record MonitorRow(
    int CandidateId,
    string CandidateNumber,
    string FullName,
    string Username,
    int? AttemptId,
    string Status,
    bool IsOnline,
    DateTime? StartedAt,
    DateTime? EndsAt,
    int? RemainingSeconds,
    int PlannedExtraSeconds,
    int IncidentExtraSeconds,
    bool EndTimeEdited,
    string? SubmitReason,
    int? AudioPositionSeconds,
    int Answered,
    int Warnings,
    int? PaperVersion = null);

/// <summary>VE-29: a warning in the Warnings column, with who resolved it.</summary>
public sealed record WarningView(long Id, int CandidateId, string FullName, string Type, string? Detail, DateTime DetectedAt, DateTime? ResolvedAt, string? ResolvedBy);

/// <param name="StartAnnouncedAt">When the invigilator told the room to begin (VE-29); null until then.</param>
/// <param name="PaperMixTitle">Set when the section gives mixed papers by seat; each row then has its <c>PaperVersion</c> (VE-67).</param>
public sealed record MonitorView(int ExamId, string ExamName, int SectionId, string SectionTitle, bool HasAudio, int DurationMinutes, DateTime ServerNow, IReadOnlyList<MonitorRow> Candidates, DateTime? StartAnnouncedAt, string? PaperMixTitle = null);

public sealed record InvigilatorActionRequest(
    [Required] string Action,
    [Required, MinLength(1), MaxLength(500)] List<int> CandidateIds,
    [Range(1, InvigilatorMonitorController.MaxMinutes)] int? Minutes = null,
    DateTime? EndAt = null,
    bool DisregardExtraTime = false,
    [StringLength(200)] string? Reason = null);

/// <param name="CandidateIds">Who the test ends for; empty or missing means everyone entered for the exam.</param>
public sealed record EndAllRequest([MaxLength(500)] List<int>? CandidateIds = null, [StringLength(200)] string? Reason = null);

public sealed record SkippedCandidate(int CandidateId, string FullName, string Reason);

/// <param name="PausedUntilResumed">End all only: how many of the candidates it ended are paused, so they keep Paused and end when resumed.</param>
public sealed record ActionOutcome(int Applied, IReadOnlyList<SkippedCandidate> Skipped, int PausedUntilResumed = 0);

public sealed record SendMessageRequest([Required, StringLength(InvigilatorMonitorController.MaxMessageLength)] string Text, List<int>? CandidateIds = null);

/// <summary>
/// VE-14…VE-17: the invigilator's monitoring page for one section (Inspera "child test"). Lists every candidate with
/// status, connection and time left; applies Pause/Resume, incident extra time, Edit End Time and Listening rewinds to
/// the selected candidates; sends messages. Every operation is written to <c>invigilator_actions</c>.
/// </summary>
[ApiController]
[Route("api/staff/exams/{examId:int}")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class InvigilatorMonitorController(ExamPrepDbContext db, TimeProvider clock, RoomNotifier notifier, ILogger<InvigilatorMonitorController> logger) : ControllerBase
{
    public const int MaxMinutes = 180;
    public const int MaxMessageLength = 500;

    /// <summary>The candidate's browser sends a heartbeat every 5 s; three missed beats count as offline.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(15);

    private const int MaxSaveAttempts = 3;

    [HttpGet("sections/{sectionId:int}/monitor")]
    public async Task<ActionResult<MonitorView>> Monitor(int examId, int sectionId, CancellationToken cancellationToken)
    {
        var section = await FindSectionAsync(examId, sectionId, cancellationToken);
        if (section is null)
        {
            return NotFound();
        }

        var now = Now();
        var candidates = await db.Candidates.AsNoTracking().Where(c => c.ExamId == examId).OrderBy(c => c.CandidateNumber).ToListAsync(cancellationToken);
        var attempts = await db.Attempts.AsNoTracking().Where(a => a.ExamSectionId == sectionId).ToDictionaryAsync(a => a.CandidateId, cancellationToken);
        var answered = await db.Responses.AsNoTracking()
            .Where(r => r.Attempt!.ExamSectionId == sectionId && r.Value != null && r.Value != "")
            .GroupBy(r => r.AttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AttemptId, g => g.Count, cancellationToken);
        var hasAudio = await db.PaperParts.AnyAsync(p => p.TestPaperId == section.TestPaperId && p.AudioAssetId != null, cancellationToken);
        var warnings = await db.SecurityEvents.AsNoTracking()
            .Where(e => e.Attempt!.ExamSectionId == sectionId && e.ResolvedAt == null)
            .GroupBy(e => e.AttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AttemptId, g => g.Count, cancellationToken);

        var lengths = await RecordingLength.OfPapersAsync(db, attempts.Values.Select(a => a.TestPaperId), cancellationToken);
        var rows = candidates.Select(c => Row(c, attempts.GetValueOrDefault(c.Id), section, answered, now, lengths) is var row && row.AttemptId is { } id
            ? row with { Warnings = warnings.GetValueOrDefault(id) }
            : row).ToList();

        string? mixTitle = null;
        if (section.PaperMixId is { } mixId)
        {
            // The started attempt's version; before that, the one the candidate's seat will get.
            mixTitle = await db.PaperMixes.Where(x => x.Id == mixId).Select(x => x.Title).SingleAsync(cancellationToken);
            var bySeat = PaperAssignment.VersionsOf(candidates, section.Exam!.SeatsPerRow, await PaperAssignment.ShapeOfAsync(db, mixId, cancellationToken));
            var started = await PaperAssignment.MixVersionsAsync(db, attempts.Values.Select(a => a.TestPaperId), cancellationToken);
            rows = rows.Select(r => r with
            {
                PaperVersion = attempts.TryGetValue(r.CandidateId, out var a) && started.TryGetValue(a.TestPaperId, out var v) ? v : bySeat[r.CandidateId],
            }).ToList();
        }

        return new MonitorView(examId, section.Exam!.Name, sectionId, section.SectionTemplate!.Title, hasAudio, section.DurationMinutes, now, rows, section.StartAnnouncedAt, mixTitle);
    }

    [HttpPost("sections/{sectionId:int}/actions")]
    public async Task<ActionResult<ActionOutcome>> Act(int examId, int sectionId, InvigilatorActionRequest request, CancellationToken cancellationToken)
    {
        var section = await FindSectionAsync(examId, sectionId, cancellationToken);
        if (section is null)
        {
            return NotFound();
        }

        var (command, error) = Parse(request);
        if (command is null)
        {
            return Problems.Invalid(error!);
        }

        var ids = request.CandidateIds.Distinct().ToList();
        var foreign = ids.Except(await db.Candidates.Where(c => c.ExamId == examId && ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(cancellationToken)).ToList();
        if (foreign.Count > 0)
        {
            return Problems.Invalid($"Candidate {string.Join(", ", foreign)} is not entered for this exam.");
        }

        for (var round = 1; ; round++)
        {
            try
            {
                return await ApplyAsync(section, ids, command, request.Reason, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (round < MaxSaveAttempts)
            {
                // The candidate's browser or the time-up sweep changed an attempt meanwhile: start again from fresh data.
                db.ChangeTracker.Clear();
            }
        }
    }

    /// <summary>
    /// The parent-level end of the Inspera guide: the whole test (every section) ends for the candidates now. It sets
    /// <c>candidates.parent_end_override_at</c>, which cuts every section's end short, and brings the running attempts'
    /// end in line through the clock; the expiry sweep then submits them. "Use test end time" for the whole test
    /// (<see cref="UseTestEndTimeForAll"/>) gives the end back. A candidate already ended this way keeps the earlier moment.
    /// </summary>
    [HttpPost("end-all")]
    public Task<ActionResult<ActionOutcome>> EndAll(int examId, EndAllRequest request, CancellationToken cancellationToken) =>
        ParentLevelAsync(examId, request, "The whole test can be ended only while the exam is Active.", "End the whole test", EndAllAsync, cancellationToken);

    /// <summary>
    /// "Use test end time" at the parent level: undoes <see cref="EndAll"/> for the candidates (empty: everyone) by
    /// clearing <c>candidates.parent_end_override_at</c>, whatever state their sections are in. The section-level action
    /// cannot do it once the sweep has submitted the running section. A section already submitted stays submitted
    /// (attempts are exam records); a running one gets its own end back through the clock, and later sections run with
    /// their full time. Allowed to the same people as End all.
    /// </summary>
    [HttpPost("use-test-end-time")]
    public Task<ActionResult<ActionOutcome>> UseTestEndTimeForAll(int examId, EndAllRequest request, CancellationToken cancellationToken) =>
        ParentLevelAsync(examId, request, "The test end time can be given back only while the exam is Active.", "Use test end time", RestoreAllAsync, cancellationToken);

    /// <summary>The checks and the retry shared by the two parent-level actions.</summary>
    private async Task<ActionResult<ActionOutcome>> ParentLevelAsync(
        int examId, EndAllRequest request, string notActive, string button, Func<int, List<int>, string?, CancellationToken, Task<ActionOutcome>> apply, CancellationToken cancellationToken)
    {
        var status = await db.Exams.MonitorableBy(User).Where(e => e.Id == examId).Select(e => (ExamStatus?)e.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        if (status != ExamStatus.Active)
        {
            return Problems.Conflict(notActive);
        }

        var ids = request.CandidateIds?.Distinct().ToList() ?? [];
        if (ids.Count > 0 && await db.Candidates.CountAsync(c => c.ExamId == examId && ids.Contains(c.Id), cancellationToken) != ids.Count)
        {
            return Problems.Invalid("Some of the selected candidates are not entered for this exam.");
        }

        for (var round = 1; ; round++)
        {
            try
            {
                return await apply(examId, ids, request.Reason, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                if (round >= MaxSaveAttempts)
                {
                    return Problems.Conflict($"Candidates' tests were updated at the same moment. Press {button} again.");
                }
            }
        }
    }

    private async Task<ActionOutcome> RestoreAllAsync(int examId, List<int> candidateIds, string? reason, CancellationToken cancellationToken)
    {
        var now = Now();
        var candidates = await db.Candidates.Where(c => c.ExamId == examId && (candidateIds.Count == 0 || candidateIds.Contains(c.Id))).OrderBy(c => c.CandidateNumber).ToListAsync(cancellationToken);
        var ids = candidates.Select(c => c.Id).ToList();
        var running = (await db.Attempts.Include(a => a.ExamSection)
            .Where(a => ids.Contains(a.CandidateId) && (a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Paused))
            .ToListAsync(cancellationToken)).ToLookup(a => a.CandidateId);
        var batchId = Guid.NewGuid();
        var userId = User.GetStaffId();
        var skipped = new List<SkippedCandidate>();
        var applied = 0;

        foreach (var candidate in candidates)
        {
            if (candidate.ParentEndOverrideAt is null)
            {
                skipped.Add(new SkippedCandidate(candidate.Id, FullName(candidate), "has not had the whole test ended"));
                continue;
            }

            candidate.ParentEndOverrideAt = null;
            var attempts = running[candidate.Id].ToList();
            attempts.ForEach(a => AttemptExpiry.ClockFor(a, a.ExamSection!, candidate).RefreshEnd());
            applied++;
            db.InvigilatorActions.Add(new InvigilatorAction
            {
                ExamId = examId,
                AttemptId = attempts.Count == 1 ? attempts[0].Id : null,
                UserId = userId,
                ActionType = InvigilatorActionType.UseTestEndTime,
                PayloadJson = JsonSerializer.Serialize(new { level = "test", candidateId = candidate.Id }),
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                BatchId = batchId,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        running.SelectMany(g => g).Select(a => a.ExamSectionId).Distinct().ToList().ForEach(notifier.SectionChanged);
        if (applied > 0)
        {
            await notifier.RefreshCandidatesAsync(candidates.Select(c => c.Id).Except(skipped.Select(s => s.CandidateId)));
        }

        logger.LogInformation("Staff {UserId} gave the test end time back in exam {ExamId} to {Applied} candidate(s); {Skipped} had not been ended", userId, examId, applied, skipped.Count);
        return new ActionOutcome(applied, skipped);
    }

    private async Task<ActionOutcome> EndAllAsync(int examId, List<int> candidateIds, string? reason, CancellationToken cancellationToken)
    {
        var now = Now();
        var candidates = await db.Candidates.Where(c => c.ExamId == examId && (candidateIds.Count == 0 || candidateIds.Contains(c.Id))).OrderBy(c => c.CandidateNumber).ToListAsync(cancellationToken);
        var ids = candidates.Select(c => c.Id).ToList();
        var running = (await db.Attempts.Include(a => a.ExamSection)
            .Where(a => ids.Contains(a.CandidateId) && (a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Paused))
            .ToListAsync(cancellationToken)).ToLookup(a => a.CandidateId);
        var batchId = Guid.NewGuid();
        var userId = User.GetStaffId();
        var skipped = new List<SkippedCandidate>();
        var applied = 0;
        var pausedUntilResumed = 0;

        foreach (var candidate in candidates)
        {
            if (candidate.ParentEndOverrideAt is { } earlier && earlier <= now)
            {
                skipped.Add(new SkippedCandidate(candidate.Id, FullName(candidate), "has already been ended"));
                continue;
            }

            candidate.ParentEndOverrideAt = now;
            var attempts = running[candidate.Id].ToList();
            attempts.ForEach(a => AttemptExpiry.ClockFor(a, a.ExamSection!, candidate).RefreshEnd());
            applied++;
            if (attempts.Any(a => a.Status == AttemptStatus.Paused))
            {
                pausedUntilResumed++;
            }

            db.InvigilatorActions.Add(new InvigilatorAction
            {
                ExamId = examId,
                AttemptId = attempts.Count == 1 ? attempts[0].Id : null,
                UserId = userId,
                ActionType = InvigilatorActionType.EndAll,
                PayloadJson = JsonSerializer.Serialize(new { candidateId = candidate.Id, endsAt = now }),
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                BatchId = batchId,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        running.SelectMany(g => g).Select(a => a.ExamSectionId).Distinct().ToList().ForEach(notifier.SectionChanged);
        if (applied > 0)
        {
            await notifier.RefreshCandidatesAsync(candidates.Select(c => c.Id).Except(skipped.Select(s => s.CandidateId)));
        }

        logger.LogInformation("Staff {UserId} ended the whole test of exam {ExamId} for {Applied} candidate(s); {Skipped} already ended", userId, examId, applied, skipped.Count);
        return new ActionOutcome(applied, skipped, pausedUntilResumed);
    }

    /// <summary>
    /// VE-29: the invigilator tells the room to begin. Inspera warns about candidates who started before that, so every
    /// attempt already started gets a StartedEarly warning; it never blocks anyone. Announcing twice changes nothing.
    /// </summary>
    [HttpPost("sections/{sectionId:int}/announce-start")]
    public async Task<ActionResult<MonitorView>> AnnounceStart(int examId, int sectionId, [FromServices] Security.SecurityEventRecorder events, CancellationToken cancellationToken)
    {
        if (await FindSectionAsync(examId, sectionId, cancellationToken) is null)
        {
            return NotFound();
        }

        var section = await db.ExamSections.SingleAsync(s => s.Id == sectionId, cancellationToken);
        if (section.StartAnnouncedAt is null)
        {
            var now = Now();
            section.StartAnnouncedAt = now;
            db.InvigilatorActions.Add(new InvigilatorAction
            {
                ExamId = examId,
                UserId = User.GetStaffId(),
                ActionType = InvigilatorActionType.AnnounceStart,
                PayloadJson = JsonSerializer.Serialize(new { sectionId }),
                BatchId = Guid.NewGuid(),
                CreatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken);

            var early = await db.Attempts.AsNoTracking()
                .Where(a => a.ExamSectionId == sectionId && a.StartedAt != null && a.StartedAt < now)
                .Select(a => new { a.Id, StartedAt = a.StartedAt!.Value })
                .ToListAsync(cancellationToken);
            foreach (var attempt in early)
            {
                var minutes = Math.Max(1, (int)Math.Round((now - attempt.StartedAt).TotalMinutes));
                await events.RecordAsync(attempt.Id, sectionId, SecurityEventType.StartedEarly,
                    $"Started {minutes} min before the invigilator announced the start.", TimeSpan.FromDays(1), cancellationToken);
            }

            notifier.SectionChanged(sectionId);
            logger.LogInformation("Staff {UserId} announced the start of section {SectionId}; {Early} candidate(s) had started early", User.GetStaffId(), sectionId, early.Count);
        }

        return await Monitor(examId, sectionId, cancellationToken);
    }

    /// <summary>VE-29: the section's warnings, unresolved first, newest first.</summary>
    [HttpGet("sections/{sectionId:int}/warnings")]
    public async Task<ActionResult<IReadOnlyList<WarningView>>> Warnings(int examId, int sectionId, CancellationToken cancellationToken)
    {
        if (await FindSectionAsync(examId, sectionId, cancellationToken) is null)
        {
            return NotFound();
        }

        var warnings = await WarningRows(db.SecurityEvents.Where(e => e.Attempt!.ExamSectionId == sectionId)).ToListAsync(cancellationToken);
        return warnings.OrderBy(w => w.ResolvedAt is not null).ThenByDescending(w => w.DetectedAt).ToList();
    }

    /// <summary>The invigilator has looked into the warning; it leaves the candidate's count but stays in the log.</summary>
    [HttpPost("sections/{sectionId:int}/warnings/{warningId:long}/resolve")]
    public async Task<ActionResult<WarningView>> ResolveWarning(int examId, int sectionId, long warningId, CancellationToken cancellationToken)
    {
        if (await FindSectionAsync(examId, sectionId, cancellationToken) is null)
        {
            return NotFound();
        }

        var warning = await db.SecurityEvents.SingleOrDefaultAsync(e => e.Id == warningId && e.Attempt!.ExamSectionId == sectionId, cancellationToken);
        if (warning is null)
        {
            return NotFound();
        }

        if (warning.ResolvedAt is null)
        {
            warning.ResolvedAt = Now();
            warning.ResolvedByUserId = User.GetStaffId();
            await db.SaveChangesAsync(cancellationToken);
            notifier.SectionChanged(sectionId);
            logger.LogInformation("Staff {UserId} resolved warning {WarningId} in section {SectionId}", warning.ResolvedByUserId, warningId, sectionId);
        }

        return await WarningRows(db.SecurityEvents.Where(e => e.Id == warningId)).SingleAsync(cancellationToken);
    }

    private IQueryable<WarningView> WarningRows(IQueryable<SecurityEvent> events) =>
        events.AsNoTracking().Select(e => new WarningView(
            e.Id,
            e.Attempt!.CandidateId,
            e.Attempt.Candidate!.FirstName + " " + e.Attempt.Candidate.LastName,
            e.Type.ToString(),
            e.Detail,
            e.DetectedAt,
            e.ResolvedAt,
            db.Users.Where(u => u.Id == e.ResolvedByUserId).Select(u => u.FullName).FirstOrDefault()));

    [HttpGet("messages")]
    public async Task<ActionResult<IReadOnlyList<CandidateMessageView>>> Messages(int examId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.MonitorableBy(User).AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return NotFound();
        }

        return await db.CandidateMessages.AsNoTracking()
            .Where(m => m.ExamId == examId)
            .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
            .Select(m => new CandidateMessageView(m.Id, m.Text, m.SentAt, m.CandidateId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>To the whole room (no candidates given) or to each selected candidate. Shown under the bell icon.</summary>
    [HttpPost("messages")]
    public async Task<ActionResult<IReadOnlyList<CandidateMessageView>>> SendMessage(int examId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Exams.MonitorableBy(User).AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return NotFound();
        }

        var text = request.Text.Trim();
        if (text.Length == 0)
        {
            return Problems.Invalid("Type a message.");
        }

        var ids = request.CandidateIds?.Distinct().ToList() ?? [];
        if (ids.Count > 0 && await db.Candidates.CountAsync(c => c.ExamId == examId && ids.Contains(c.Id), cancellationToken) != ids.Count)
        {
            return Problems.Invalid("Some of the selected candidates are not entered for this exam.");
        }

        var now = Now();
        var userId = User.GetStaffId();
        int?[] recipients = ids.Count == 0 ? [null] : ids.Select(id => (int?)id).ToArray();
        var messages = recipients
            .Select(candidateId => new CandidateMessage { ExamId = examId, CandidateId = candidateId, Text = text, SentByUserId = userId, SentAt = now })
            .ToList();
        db.CandidateMessages.AddRange(messages);
        db.InvigilatorActions.Add(new InvigilatorAction
        {
            ExamId = examId,
            UserId = userId,
            ActionType = InvigilatorActionType.SendMessage,
            PayloadJson = JsonSerializer.Serialize(new { text, candidateIds = ids }),
            BatchId = Guid.NewGuid(),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await (ids.Count == 0 ? notifier.RefreshExamAsync(examId) : notifier.RefreshCandidatesAsync(ids));
        logger.LogInformation("Staff {UserId} sent a message in exam {ExamId} to {Recipients}", userId, examId, ids.Count == 0 ? "the room" : $"{ids.Count} candidate(s)");
        return Created($"/api/staff/exams/{examId}/messages", messages.Select(m => new CandidateMessageView(m.Id, m.Text, m.SentAt, m.CandidateId)).ToList());
    }

    private async Task<ActionOutcome> ApplyAsync(ExamSection section, List<int> candidateIds, MonitorCommand command, string? reason, CancellationToken cancellationToken)
    {
        var now = Now();
        var candidates = await db.Candidates.Where(c => candidateIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var attempts = await db.Attempts.Where(a => a.ExamSectionId == section.Id && candidateIds.Contains(a.CandidateId)).ToDictionaryAsync(a => a.CandidateId, cancellationToken);
        var lengths = command.Action == MonitorAction.RewindAudio
            ? await RecordingLength.OfPapersAsync(db, attempts.Values.Select(a => a.TestPaperId), cancellationToken)
            : [];
        var batchId = Guid.NewGuid();
        var userId = User.GetStaffId();
        var skipped = new List<SkippedCandidate>();
        var applied = 0;

        foreach (var id in candidateIds)
        {
            var candidate = candidates[id];
            var attempt = attempts.GetValueOrDefault(id);
            // "Use test end time" also undoes a parent-level end (End all); it has to happen before the clock is built.
            var restoredParentEnd = command.Action == MonitorAction.UseTestEndTime && attempt?.Status != AttemptStatus.Submitted && candidate.ParentEndOverrideAt is not null;
            if (restoredParentEnd)
            {
                candidate.ParentEndOverrideAt = null;
            }

            if (restoredParentEnd && attempt is null)
            {
                applied++;
                db.InvigilatorActions.Add(new InvigilatorAction
                {
                    ExamId = section.ExamId,
                    UserId = userId,
                    ActionType = InvigilatorActionType.UseTestEndTime,
                    PayloadJson = JsonSerializer.Serialize(new { action = command.Action.ToString(), candidateId = id }),
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                    BatchId = batchId,
                    CreatedAt = now,
                });
                continue;
            }

            var attemptClock = attempt is null ? null : AttemptExpiry.ClockFor(attempt, section, candidate);
            if (attempt is not null && AttemptExpiry.SubmitIfExpired(attempt, section, candidate, now))
            {
                skipped.Add(new SkippedCandidate(id, FullName(candidate), "has already finished this section"));
                continue;
            }

            var skipReason = InvigilatorActionRules.Apply(command, attempt, attemptClock, IsOnline(attempt, now), now, attempt is not null && lengths.TryGetValue(attempt.TestPaperId, out var length) ? length : null);
            if (skipReason is not null)
            {
                skipped.Add(new SkippedCandidate(id, FullName(candidate), skipReason));
                continue;
            }

            applied++;
            db.InvigilatorActions.Add(new InvigilatorAction
            {
                ExamId = section.ExamId,
                AttemptId = attempt!.Id,
                UserId = userId,
                ActionType = InvigilatorActionRules.LogType(command.Action),
                PayloadJson = JsonSerializer.Serialize(new { action = command.Action.ToString(), command.Minutes, command.EndAt, command.DisregardExtraTime }),
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                BatchId = batchId,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        notifier.SectionChanged(section.Id);
        if (applied > 0)
        {
            // The candidates' screens pause, resume or move their clock now rather than at the next heartbeat.
            await notifier.RefreshCandidatesAsync(candidateIds.Except(skipped.Select(s => s.CandidateId)));
        }

        logger.LogInformation("Staff {UserId} applied {Action} to {Applied} candidate(s) in section {SectionId}; {Skipped} skipped", userId, command.Action, applied, section.Id, skipped.Count);
        return new ActionOutcome(applied, skipped);
    }

    private static (MonitorCommand? Command, string? Error) Parse(InvigilatorActionRequest request)
    {
        if (!Enum.GetNames<MonitorAction>().Contains(request.Action))
        {
            return (null, $"Action must be one of {string.Join(", ", Enum.GetNames<MonitorAction>())}.");
        }

        var action = Enum.Parse<MonitorAction>(request.Action);
        var needsMinutes = action is MonitorAction.AddIncidentTime or MonitorAction.EndIn or MonitorAction.RewindAudio;
        if (needsMinutes && request.Minutes is null)
        {
            return (null, "Enter the number of minutes.");
        }

        if (action == MonitorAction.EndAt && request.EndAt is null)
        {
            return (null, "Enter the new end time.");
        }

        var endAt = request.EndAt is { } at ? at.ToUniversalTime() : (DateTime?)null;
        return (new MonitorCommand(action, request.Minutes, endAt, request.DisregardExtraTime), null);
    }

    private static MonitorRow Row(Candidate candidate, Attempt? attempt, ExamSection section, Dictionary<int, int> answered, DateTime now, Dictionary<int, TimeSpan> recordingLengths)
    {
        if (attempt is null)
        {
            return new MonitorRow(candidate.Id, candidate.CandidateNumber, FullName(candidate), candidate.Username, null, nameof(AttemptStatus.NotStarted), false, null, null, null, 0, 0, false, null, null, 0, 0);
        }

        var attemptClock = AttemptExpiry.ClockFor(attempt, section, candidate);
        var isRunning = attempt.Status != AttemptStatus.Submitted;
        // Once the section is submitted the recording is over: the position would otherwise keep counting (mock exam).
        // It also stops at the end of the recording once every track's length is known.
        var audio = attempt.AudioStartedAt is null || !isRunning ? (int?)null : (int)InvigilatorActionRules.Played(attemptClock, now, recordingLengths.TryGetValue(attempt.TestPaperId, out var length) ? length : null).TotalSeconds;
        return new MonitorRow(
            candidate.Id,
            candidate.CandidateNumber,
            FullName(candidate),
            candidate.Username,
            attempt.Id,
            attempt.Status.ToString(),
            isRunning && IsOnline(attempt, now),
            attempt.StartedAt,
            attempt.EndsAt,
            isRunning ? (int)Math.Ceiling(attemptClock.Remaining(now).TotalSeconds) : 0,
            attempt.PlannedExtraSeconds,
            attempt.IncidentExtraSeconds,
            attempt.EndAdjustSeconds != 0 || attempt.DisregardExtraTime,
            attempt.SubmitReason?.ToString(),
            audio,
            answered.GetValueOrDefault(attempt.Id),
            0);
    }

    /// <summary>A recent heartbeat, and no closed live connection since (VE-13: closing the tab shows at once).</summary>
    public static bool IsOnline(Attempt? attempt, DateTime now) =>
        attempt?.LastHeartbeatAt is { } beat && now - beat <= OnlineWindow && attempt.ConnectionStatus == ConnectionStatus.Online;

    private static string FullName(Candidate candidate) => $"{candidate.FirstName} {candidate.LastName}";

    /// <summary>The section, if its session is visible to this staff member (invigilators: assigned sessions only).</summary>
    private async Task<ExamSection?> FindSectionAsync(int examId, int sectionId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.MonitorableBy(User).AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return null;
        }

        return await db.ExamSections.AsNoTracking()
            .Include(s => s.Exam)
            .Include(s => s.SectionTemplate)
            .SingleOrDefaultAsync(s => s.Id == sectionId && s.ExamId == examId, cancellationToken);
    }

    /// <summary>Now, to the millisecond, so times read back from MySQL equal the ones just written.</summary>
    private DateTime Now()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
