using ExamPrep.Domain.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Delivery;

/// <summary>
/// The server-authoritative clock of one attempt. Every operation updates the attempt's fields and
/// recomputes <see cref="Attempt.EndsAt"/>; the candidate's browser only displays what this computes.
/// </summary>
/// <remarks>
/// End time = start + section duration + time spent paused + <see cref="Attempt.EndAdjustSeconds"/>
/// + planned and incident extra time (unless disregarded), cut short by a parent-level end if one is set.
/// </remarks>
public sealed class AttemptClock(Attempt attempt, int sectionDurationMinutes, DateTime? parentEndOverrideAt = null)
{
    private const int PercentBase = 100;

    public void Start(int plannedExtraPercent, DateTime now)
    {
        if (attempt.Status != AttemptStatus.NotStarted)
        {
            throw new DomainRuleException("This section has already been started.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(plannedExtraPercent);

        attempt.Status = AttemptStatus.InProgress;
        attempt.StartedAt = now;
        attempt.PlannedExtraSeconds = sectionDurationMinutes * 60 * plannedExtraPercent / PercentBase;
        Recompute();
    }

    public void Pause(DateTime now)
    {
        RequireStatus(AttemptStatus.InProgress, "Only a candidate who is working can be paused.");
        attempt.Status = AttemptStatus.Paused;
        attempt.PausedAt = now;
    }

    public void Resume(DateTime now)
    {
        RequireStatus(AttemptStatus.Paused, "Only a paused candidate can be resumed.");
        var pausedSeconds = WholeSeconds(now - attempt.PausedAt!.Value);

        attempt.PausedSeconds += pausedSeconds;
        if (attempt.AudioStartedAt is { } audioStart && audioStart <= attempt.PausedAt)
        {
            attempt.AudioAdjustMs += pausedSeconds * 1000;
        }

        attempt.PausedAt = null;
        attempt.Status = AttemptStatus.InProgress;
        Recompute();
    }

    public void AddIncidentExtraTime(TimeSpan extra)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(extra, TimeSpan.Zero);
        RequireActive();
        attempt.IncidentExtraSeconds += WholeSeconds(extra);
        Recompute();
    }

    /// <summary>"End in": the candidate has exactly <paramref name="remaining"/> left from now.</summary>
    public void EndIn(TimeSpan remaining, DateTime now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(remaining, TimeSpan.Zero);
        RequireStatus(AttemptStatus.InProgress, "End time can only be edited for a candidate who is working.");
        attempt.EndAdjustSeconds += WholeSeconds(now + remaining - ScheduledEnd());
        Recompute();
    }

    /// <summary>"End at": the attempt ends at <paramref name="endTime"/>, which must be in the future.</summary>
    public void EndAt(DateTime endTime, DateTime now)
    {
        if (endTime <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(endTime), "The end time must be in the future; use End now instead.");
        }

        EndIn(endTime - now, now);
    }

    /// <summary>
    /// "End now": standard time ends immediately. Planned and incident extra time still run unless
    /// <paramref name="disregardExtraTime"/> is set. Irreversible once the attempt is submitted.
    /// </summary>
    public void EndNow(bool disregardExtraTime, DateTime now)
    {
        if (attempt.Status == AttemptStatus.Paused)
        {
            Resume(now);
        }

        RequireStatus(AttemptStatus.InProgress, "Only a candidate who is working can be ended.");
        attempt.EndAdjustSeconds += WholeSeconds(now - StandardEnd());
        attempt.DisregardExtraTime = disregardExtraTime;
        Recompute();

        if (Remaining(now) == TimeSpan.Zero)
        {
            Submit(SubmitReason.InvigilatorEndNow, now);
        }
    }

    /// <summary>"Use test end time": drop every End now / End at / End in adjustment.</summary>
    public void UseTestEndTime()
    {
        RequireActive();
        attempt.EndAdjustSeconds = 0;
        attempt.DisregardExtraTime = false;
        Recompute();
    }

    public void Submit(SubmitReason reason, DateTime now)
    {
        RequireActive();
        attempt.Status = AttemptStatus.Submitted;
        attempt.SubmitReason = reason;
        attempt.SubmittedAt = now;
        attempt.PausedAt = null;
    }

    public TimeSpan Remaining(DateTime now)
    {
        if (attempt.Status == AttemptStatus.Submitted)
        {
            return TimeSpan.Zero;
        }

        if (attempt.StartedAt is null)
        {
            return TimeSpan.FromMinutes(sectionDurationMinutes);
        }

        var clockTime = attempt.PausedAt ?? now;
        var remaining = EffectiveEnd() - clockTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public bool IsExpired(DateTime now) =>
        attempt.Status == AttemptStatus.InProgress && Remaining(now) == TimeSpan.Zero;

    public void StartAudio(DateTime now)
    {
        RequireStatus(AttemptStatus.InProgress, "Audio can only start while the candidate is working.");
        if (attempt.AudioStartedAt is not null)
        {
            throw new DomainRuleException("The audio has already started; it cannot be played again.");
        }

        attempt.AudioStartedAt = now;
    }

    public TimeSpan AudioPosition(DateTime now)
    {
        if (attempt.AudioStartedAt is not { } audioStart)
        {
            return TimeSpan.Zero;
        }

        var position = (attempt.PausedAt ?? now) - audioStart - TimeSpan.FromMilliseconds(attempt.AudioAdjustMs);
        return position > TimeSpan.Zero ? position : TimeSpan.Zero;
    }

    /// <summary>Rewind the Listening audio. Allowed only while not paused, by at most the time already played.</summary>
    public void RewindAudio(TimeSpan by, DateTime now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(by, TimeSpan.Zero);
        RequireStatus(AttemptStatus.InProgress, "Audio cannot be rewound while the candidate is paused.");
        if (by > AudioPosition(now))
        {
            throw new ArgumentOutOfRangeException(nameof(by), "Cannot rewind further than the audio has played.");
        }

        attempt.AudioAdjustMs += (int)by.TotalMilliseconds;
    }

    private DateTime StandardEnd() =>
        attempt.StartedAt!.Value
            .AddMinutes(sectionDurationMinutes)
            .AddSeconds(attempt.PausedSeconds + attempt.EndAdjustSeconds);

    private DateTime ScheduledEnd()
    {
        var extraSeconds = attempt.DisregardExtraTime ? 0 : attempt.PlannedExtraSeconds + attempt.IncidentExtraSeconds;
        return StandardEnd().AddSeconds(extraSeconds);
    }

    private DateTime EffectiveEnd()
    {
        var scheduled = ScheduledEnd();
        return parentEndOverrideAt is { } parentEnd && parentEnd < scheduled ? parentEnd : scheduled;
    }

    private void Recompute() => attempt.EndsAt = EffectiveEnd();

    private void RequireStatus(AttemptStatus expected, string message)
    {
        if (attempt.Status != expected)
        {
            throw new DomainRuleException(message);
        }
    }

    private void RequireActive()
    {
        if (attempt.Status is not (AttemptStatus.InProgress or AttemptStatus.Paused))
        {
            throw new DomainRuleException("The attempt is not in progress.");
        }
    }

    private static int WholeSeconds(TimeSpan span) => (int)Math.Round(span.TotalSeconds);
}
