using ExamPrep.Api.Realtime;
using ExamPrep.Domain.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// Ends sections whose time has run out. The candidate's own requests check their attempt; a background sweep
/// (<see cref="AttemptExpiryWorker"/>) catches candidates whose computer is off or disconnected, so the next
/// section still opens on time.
/// </summary>
public sealed class AttemptExpiry(ExamPrepDbContext db, TimeProvider clock, RoomNotifier notifier, ILogger<AttemptExpiry> logger)
{
    /// <summary>Answers that arrive this soon after the end (network delay) are still kept, so nothing submits earlier.</summary>
    public static readonly TimeSpan AutosaveGrace = TimeSpan.FromSeconds(5);

    /// <summary>Clock for an attempt, honouring its section duration and any parent-level end.</summary>
    public static AttemptClock ClockFor(Attempt attempt, ExamSection section, Candidate candidate) =>
        new(attempt, section.DurationMinutes, candidate.ParentEndOverrideAt);

    /// <summary>
    /// Submits the attempt with reason TimeExpired once its time is up plus <see cref="AutosaveGrace"/>. Returns whether it
    /// did. The clock the candidate sees is unchanged: only the moment the server stops taking answers moves.
    /// </summary>
    public static bool SubmitIfExpired(Attempt attempt, ExamSection section, Candidate candidate, DateTime now)
    {
        var attemptClock = ClockFor(attempt, section, candidate);
        if (!attemptClock.IsExpired(now - AutosaveGrace))
        {
            return false;
        }

        attemptClock.Submit(SubmitReason.TimeExpired, now);
        return true;
    }

    /// <summary>One sweep over every running attempt whose stored end has passed.</summary>
    public async Task<int> SubmitExpiredAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var due = await db.Attempts
            .Include(a => a.ExamSection)
            .Include(a => a.Candidate)
            .Where(a => a.Status == AttemptStatus.InProgress && a.EndsAt <= now - AutosaveGrace)
            .ToListAsync(cancellationToken);

        var submitted = due.Count(a => SubmitIfExpired(a, a.ExamSection!, a.Candidate!, now));
        if (submitted == 0)
        {
            return 0;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await NotifyAsync(due.Where(a => a.Status == AttemptStatus.Submitted).ToList());
            logger.LogInformation("Submitted {Count} attempt(s) whose time ran out", submitted);
            return submitted;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another writer (a heartbeat, the candidate's own request or an invigilator) changed one of them. That must
            // not hold back the others until the next sweep: go on one attempt at a time, each from fresh data, as the
            // marker does (final review 8).
            db.ChangeTracker.Clear();
        }

        var ended = new List<Attempt>();
        foreach (var id in due.Select(a => a.Id))
        {
            var attempt = await db.Attempts.Include(a => a.ExamSection).Include(a => a.Candidate)
                .SingleOrDefaultAsync(a => a.Id == id && a.Status == AttemptStatus.InProgress, cancellationToken);
            if (attempt is null || !SubmitIfExpired(attempt, attempt.ExamSection!, attempt.Candidate!, now))
            {
                // Submitted, paused or given more time by the other writer meanwhile.
                continue;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                ended.Add(attempt);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Lost again; the next sweep looks at it from fresh data.
                db.ChangeTracker.Clear();
            }
        }

        await NotifyAsync(ended);
        logger.LogInformation("Submitted {Count} attempt(s) whose time ran out, one at a time after a concurrent change", ended.Count);
        return ended.Count;
    }

    private async Task NotifyAsync(List<Attempt> ended)
    {
        ended.Select(a => a.ExamSectionId).Distinct().ToList().ForEach(notifier.SectionChanged);
        await notifier.RefreshCandidatesAsync(ended.Select(a => a.CandidateId));
    }
}

/// <summary>Every few seconds: ends sections whose time is up, then marks submitted attempts from the answer keys.</summary>
public sealed class AttemptExpiryWorker(IServiceScopeFactory scopes, ILogger<AttemptExpiryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<Infrastructure.Marking.MarkingAllocator>().EnsureAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Sweep for expired attempts failed; retrying on the next tick");
            }
        }
    }
}
