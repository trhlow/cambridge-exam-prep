using ExamPrep.Api.Auth;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Realtime;

/// <summary>
/// VE-13: the monitoring page's live line. A staff member who may monitor the session joins the section's group and
/// is told when anything on the page changed (<see cref="RoomNotifier.SectionChangedMethod"/>); the page then reloads
/// it. Polling stays as the fallback when the connection drops.
/// </summary>
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class StaffRoomHub(ExamPrepDbContext db) : Hub
{
    public const string Path = "/hubs/staff";

    /// <summary>Starts sending changes of one section; false (and nothing joined) outside the caller's sessions.</summary>
    public async Task<bool> WatchSection(int examId, int sectionId)
    {
        var user = Context.User!;
        var allowed = await db.Exams.MonitorableBy(user).AnyAsync(e => e.Id == examId)
            && await db.ExamSections.AnyAsync(s => s.Id == sectionId && s.ExamId == examId);
        if (allowed)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomNotifier.SectionGroup(sectionId));
        }

        return allowed;
    }

    public Task StopWatching(int sectionId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomNotifier.SectionGroup(sectionId));
}

/// <summary>
/// VE-13: the candidate's live line. While connected the candidate counts as online; an invigilator's pause, time
/// change, rewind or message tells the browser to ask for the clock at once instead of at the next heartbeat.
/// Closing the tab or losing the network shows the candidate offline straight away.
/// </summary>
[Authorize(Policy = CandidateAuthDefaults.ExamPolicy)]
public sealed class CandidateRoomHub(ExamPrepDbContext db, RoomNotifier notifier, TimeProvider clock) : Hub
{
    public const string Path = "/hubs/candidate";

    public override async Task OnConnectedAsync()
    {
        var candidateId = Context.User!.GetCandidateId();
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomNotifier.CandidateGroup(candidateId));
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomNotifier.ExamGroup(Context.User!.GetExamId()));
        if (notifier.Connected(candidateId))
        {
            await SetConnectionAsync(candidateId, ConnectionStatus.Online);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var candidateId = Context.User!.GetCandidateId();
        // A reload connects the new page before the old one is gone: only the last connection to go marks offline.
        if (notifier.Disconnected(candidateId))
        {
            await SetConnectionAsync(candidateId, ConnectionStatus.Offline);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Direct updates, like the heartbeat, so the connection never rotates an attempt's concurrency stamp. Only
    /// attempts that have had a heartbeat change, so going online never makes a stale attempt look alive.
    /// </summary>
    private async Task SetConnectionAsync(int candidateId, ConnectionStatus status)
    {
        var running = db.Attempts.Where(a => a.CandidateId == candidateId && a.Status != AttemptStatus.Submitted && a.LastHeartbeatAt != null);
        var sections = await running.Select(a => a.ExamSectionId).ToListAsync();
        if (sections.Count == 0)
        {
            return;
        }

        if (status == ConnectionStatus.Online)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            await running.ExecuteUpdateAsync(set => set.SetProperty(a => a.ConnectionStatus, status).SetProperty(a => a.LastHeartbeatAt, now));
        }
        else
        {
            await running.ExecuteUpdateAsync(set => set.SetProperty(a => a.ConnectionStatus, status));
        }

        sections.ForEach(notifier.SectionChanged);
    }
}
