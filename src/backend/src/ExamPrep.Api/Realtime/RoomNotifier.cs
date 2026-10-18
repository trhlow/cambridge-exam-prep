using System.Collections.Concurrent;
using ExamPrep.Api.Monitoring;
using ExamPrep.Api.Security;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Realtime;

/// <summary>
/// VE-13: tells the monitoring pages and the candidates' browsers that something changed. Sections are collected and
/// sent together by <see cref="RoomBroadcaster"/> (a room of candidates beating every few seconds becomes at most
/// one message per section per tick); candidates are told at once.
/// </summary>
public sealed class RoomNotifier(IHubContext<StaffRoomHub> staff, IHubContext<CandidateRoomHub> candidates)
{
    /// <summary>Staff: the monitoring page of this section is out of date (argument: section id).</summary>
    public const string SectionChangedMethod = "sectionChanged";

    /// <summary>Candidate: ask the server for the clock and messages now.</summary>
    public const string RefreshMethod = "refresh";

    private readonly ConcurrentDictionary<int, byte> dirtySections = new();
    private readonly ConcurrentDictionary<int, int> connections = new();

    public static string SectionGroup(int sectionId) => $"section:{sectionId}";

    public static string CandidateGroup(int candidateId) => $"candidate:{candidateId}";

    public static string ExamGroup(int examId) => $"exam:{examId}";

    public void SectionChanged(int sectionId) => dirtySections.TryAdd(sectionId, 0);

    public Task RefreshCandidatesAsync(IEnumerable<int> candidateIds) =>
        candidates.Clients.Groups(candidateIds.Distinct().Select(CandidateGroup).ToList()).SendAsync(RefreshMethod);

    public Task RefreshExamAsync(int examId) => candidates.Clients.Group(ExamGroup(examId)).SendAsync(RefreshMethod);

    /// <summary>Counts a candidate's open connections; true for the first one.</summary>
    public bool Connected(int candidateId) => connections.AddOrUpdate(candidateId, 1, (_, n) => n + 1) == 1;

    /// <summary>True when the candidate's last open connection has closed.</summary>
    public bool Disconnected(int candidateId)
    {
        while (connections.TryGetValue(candidateId, out var count))
        {
            if (count <= 1 ? connections.TryRemove(new KeyValuePair<int, int>(candidateId, count)) : connections.TryUpdate(candidateId, count - 1, count))
            {
                return count <= 1;
            }
        }

        return true;
    }

    /// <summary>Sends one message per changed section since the last flush.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        foreach (var sectionId in dirtySections.Keys)
        {
            if (dirtySections.TryRemove(sectionId, out _))
            {
                await staff.Clients.Group(SectionGroup(sectionId)).SendAsync(SectionChangedMethod, sectionId, cancellationToken);
            }
        }
    }
}

/// <summary>
/// Flushes <see cref="RoomNotifier"/> twice a second, and notices candidates whose heartbeats have just stopped
/// (no live connection to tell us), so the invigilator sees them go offline without reloading.
/// </summary>
public sealed class RoomBroadcaster(RoomNotifier notifier, IServiceScopeFactory scopes, TimeProvider clock, ILogger<RoomBroadcaster> logger) : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500);
    private const int TicksPerSilenceCheck = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        var tick = 0;
        var lastCheck = clock.GetUtcNow().UtcDateTime;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (++tick % TicksPerSilenceCheck == 0)
                {
                    var now = clock.GetUtcNow().UtcDateTime;
                    await MarkSilencedAsync(lastCheck, now, stoppingToken);
                    lastCheck = now;
                }

                await notifier.FlushAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Live update failed; retrying on the next tick");
            }
        }
    }

    /// <summary>
    /// Running attempts whose last heartbeat crossed the online window since the last check: their section's page is
    /// out of date, and each raises a <see cref="SecurityEventType.ConnectionLost"/> warning (VE-29).
    /// </summary>
    private async Task MarkSilencedAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var window = InvigilatorMonitorController.OnlineWindow;
        var (after, upTo) = (from - window, to - window);
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var silenced = await db.Attempts.AsNoTracking()
            .Where(a => a.Status != AttemptStatus.Submitted && a.LastHeartbeatAt > after && a.LastHeartbeatAt <= upTo)
            .Select(a => new { a.Id, a.ExamSectionId })
            .ToListAsync(cancellationToken);
        var events = scope.ServiceProvider.GetRequiredService<SecurityEventRecorder>();
        foreach (var attempt in silenced)
        {
            notifier.SectionChanged(attempt.ExamSectionId);
            await events.RecordAsync(attempt.Id, attempt.ExamSectionId, SecurityEventType.ConnectionLost,
                "The computer stopped responding (no heartbeat for 15 seconds).", TimeSpan.FromMinutes(1), cancellationToken);
        }
    }
}
