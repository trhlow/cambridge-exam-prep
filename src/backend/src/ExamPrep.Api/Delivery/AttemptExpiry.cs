using ExamPrep.Domain.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// Ends sections whose time has run out. The candidate's own requests check their attempt; a background sweep
/// (<see cref="AttemptExpiryWorker"/>) catches candidates whose computer is off or disconnected, so the next
/// section still opens on time.
/// </summary>
public sealed class AttemptExpiry(ExamPrepDbContext db, TimeProvider clock, ILogger<AttemptExpiry> logger)
{
    /// <summary>Clock for an attempt, honouring its section duration and any parent-level end.</summary>
    public static AttemptClock ClockFor(Attempt attempt, ExamSection section, Candidate candidate) =>
        new(attempt, section.DurationMinutes, candidate.ParentEndOverrideAt);

    /// <summary>Submits the attempt with reason TimeExpired if its time is up. Returns whether it did.</summary>
    public static bool SubmitIfExpired(Attempt attempt, ExamSection section, Candidate candidate, DateTime now)
    {
        var attemptClock = ClockFor(attempt, section, candidate);
        if (!attemptClock.IsExpired(now))
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
            .Where(a => a.Status == AttemptStatus.InProgress && a.EndsAt <= now)
            .ToListAsync(cancellationToken);

        var submitted = due.Count(a => SubmitIfExpired(a, a.ExamSection!, a.Candidate!, now));
        if (submitted == 0)
        {
            return 0;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Submitted {Count} attempt(s) whose time ran out", submitted);
            return submitted;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another writer (the candidate's own request or an invigilator) changed one of them; the next sweep retries.
            return 0;
        }
    }
}

/// <summary>Runs <see cref="AttemptExpiry.SubmitExpiredAsync"/> every few seconds.</summary>
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
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Sweep for expired attempts failed; retrying on the next tick");
            }
        }
    }
}
