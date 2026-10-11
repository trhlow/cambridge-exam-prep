using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Monitoring;

/// <summary>
/// VE-14…VE-17: the invigilator's child-test monitoring page. Candidates are listed per section; selected candidates
/// can be paused and resumed, given incident time, ended or given a new end time, and (Listening) have the audio
/// rewound. Actions apply only where Inspera allows them; the others are reported back as skipped.
/// </summary>
public sealed class InvigilatorMonitorTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

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
        int Answered);

    public sealed record MonitorView(int ExamId, string ExamName, int SectionId, string SectionTitle, bool HasAudio, int DurationMinutes, DateTime ServerNow, List<MonitorRow> Candidates);

    public sealed record Skipped(int CandidateId, string FullName, string Reason);

    public sealed record ActionOutcome(int Applied, List<Skipped> Skipped);

    public sealed record MessageView(int Id, string Text, DateTime SentAt, int? CandidateId);

    public sealed record CandidateAttemptState(int SectionId, string Status, int RemainingSeconds, List<MessageView> Messages);

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record Room(ApiFixture.SeededCandidate First, int SecondId, HttpClient Invigilator);

    /// <summary>A session with two candidates and an invigilator assigned to it.</summary>
    private async Task<Room> RoomAsync()
    {
        var first = await api.CreateCandidateAsync(Password);
        var (secondId, _) = await api.AddCandidateAsync(first.ExamId, Password);
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{first.ExamId}/invigilators", new { userIds = new[] { invigilator.Id } }, ct)).EnsureSuccessStatusCode();
        return new Room(first, secondId, await api.StaffClientAsync(invigilator.Username, Password));
    }

    private Task<MonitorView?> MonitorAsync(HttpClient client, ApiFixture.SeededCandidate seeded, int sectionId) =>
        client.GetFromJsonAsync<MonitorView>($"/api/staff/exams/{seeded.ExamId}/sections/{sectionId}/monitor", ct);

    private async Task<ActionOutcome> ActAsync(HttpClient client, ApiFixture.SeededCandidate seeded, int sectionId, object action)
    {
        var response = await client.PostAsJsonAsync($"/api/staff/exams/{seeded.ExamId}/sections/{sectionId}/actions", action, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ActionOutcome>(ct))!;
    }

    [Fact]
    public async Task Monitor_ListsEveryCandidateOfTheSectionWithStatusAndTimeLeft()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, sectionId);

        var monitor = await MonitorAsync(room.Invigilator, room.First, sectionId);

        Assert.Equal("Reading & Writing", monitor!.SectionTitle);
        Assert.False(monitor.HasAudio);
        var working = monitor.Candidates.Single(c => c.CandidateId == room.First.CandidateId);
        var waiting = monitor.Candidates.Single(c => c.CandidateId == room.SecondId);
        Assert.Equal("InProgress", working.Status);
        Assert.True(working.IsOnline);
        Assert.InRange(working.RemainingSeconds!.Value, 3500, 3600);
        Assert.Equal("NotStarted", waiting.Status);
        Assert.Null(waiting.AttemptId);
        Assert.False(waiting.IsOnline);
    }

    [Fact]
    public async Task Monitor_IsHiddenFromInvigilatorsOfOtherSessions()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var outsider = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = await api.StaffClientAsync(outsider.Username, Password);

        var monitor = await client.GetAsync($"/api/staff/exams/{room.First.ExamId}/sections/{room.First.ReadingWritingSectionId}/monitor", ct);
        var action = await client.PostAsJsonAsync($"/api/staff/exams/{room.First.ExamId}/sections/{room.First.ReadingWritingSectionId}/actions", new { action = "Pause", candidateIds = new[] { room.First.CandidateId } }, ct);

        Assert.Equal(HttpStatusCode.NotFound, monitor.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, action.StatusCode);
    }

    [Fact]
    public async Task Pause_AppliesToOnlineCandidatesOnlyAndResumeGivesThePausedTimeBack()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, sectionId);
        await api.StartAttemptAsync(room.SecondId, sectionId, isOnline: false);
        var both = new[] { room.First.CandidateId, room.SecondId };

        var pause = await ActAsync(room.Invigilator, room.First, sectionId, new { action = "Pause", candidateIds = both, reason = "Fire alarm" });
        var paused = (await MonitorAsync(room.Invigilator, room.First, sectionId))!.Candidates.Single(c => c.CandidateId == room.First.CandidateId);
        var resume = await ActAsync(room.Invigilator, room.First, sectionId, new { action = "Resume", candidateIds = both });

        Assert.Equal(1, pause.Applied);
        Assert.Equal(room.SecondId, Assert.Single(pause.Skipped).CandidateId);
        Assert.Contains("offline", pause.Skipped[0].Reason);
        Assert.Equal("Paused", paused.Status);
        Assert.Equal(1, resume.Applied);
        Assert.Equal(AttemptStatus.InProgress, (await api.GetAttemptAsync(room.First.CandidateId, sectionId)).Status);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var log = await db.InvigilatorActions.Where(a => a.ExamId == room.First.ExamId).OrderBy(a => a.Id).ToListAsync(ct);
        Assert.Equal([InvigilatorActionType.Pause, InvigilatorActionType.Resume], log.Select(a => a.ActionType));
        Assert.Equal("Fire alarm", log[0].Reason);
    }

    [Fact]
    public async Task IncidentTime_AddsMinutesEvenForOfflineCandidates()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, sectionId, isOnline: false);
        var before = await api.GetAttemptAsync(room.First.CandidateId, sectionId);

        var add = await ActAsync(room.Invigilator, room.First, sectionId, new { action = "AddIncidentTime", candidateIds = new[] { room.First.CandidateId }, minutes = 5, reason = "Power cut" });
        var zero = await room.Invigilator.PostAsJsonAsync($"/api/staff/exams/{room.First.ExamId}/sections/{sectionId}/actions", new { action = "AddIncidentTime", candidateIds = new[] { room.First.CandidateId }, minutes = 0 }, ct);

        var after = await api.GetAttemptAsync(room.First.CandidateId, sectionId);
        Assert.Equal(1, add.Applied);
        Assert.Equal(300, after.IncidentExtraSeconds);
        Assert.Equal(before.EndsAt!.Value.AddMinutes(5), after.EndsAt!.Value, TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    [Fact]
    public async Task EditEndTime_EndsNowOrMovesTheEndAndUseTestEndTimeUndoesIt()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, sectionId);
        await api.StartAttemptAsync(room.SecondId, sectionId);
        var first = new[] { room.First.CandidateId };

        await ActAsync(room.Invigilator, room.First, sectionId, new { action = "EndIn", candidateIds = first, minutes = 10 });
        var endIn = (await MonitorAsync(room.Invigilator, room.First, sectionId))!.Candidates.Single(c => c.CandidateId == room.First.CandidateId);
        await ActAsync(room.Invigilator, room.First, sectionId, new { action = "UseTestEndTime", candidateIds = first });
        var restored = (await MonitorAsync(room.Invigilator, room.First, sectionId))!.Candidates.Single(c => c.CandidateId == room.First.CandidateId);
        var endNow = await ActAsync(room.Invigilator, room.First, sectionId, new { action = "EndNow", candidateIds = new[] { room.SecondId } });
        var again = await ActAsync(room.Invigilator, room.First, sectionId, new { action = "EndNow", candidateIds = new[] { room.SecondId } });

        Assert.InRange(endIn.RemainingSeconds!.Value, 590, 600);
        Assert.True(endIn.EndTimeEdited);
        Assert.InRange(restored.RemainingSeconds!.Value, 3500, 3600);
        Assert.False(restored.EndTimeEdited);
        Assert.Equal(1, endNow.Applied);
        var ended = await api.GetAttemptAsync(room.SecondId, sectionId);
        Assert.Equal(AttemptStatus.Submitted, ended.Status);
        Assert.Equal(SubmitReason.InvigilatorEndNow, ended.SubmitReason);
        Assert.Equal(0, again.Applied);
    }

    [Fact]
    public async Task EndNow_KeepsExtraTimeUnlessTheInvigilatorDisregardsIt()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, sectionId);
        await ActAsync(room.Invigilator, room.First, sectionId, new { action = "AddIncidentTime", candidateIds = new[] { room.First.CandidateId }, minutes = 5 });

        await ActAsync(room.Invigilator, room.First, sectionId, new { action = "EndNow", candidateIds = new[] { room.First.CandidateId } });
        var keeping = await api.GetAttemptAsync(room.First.CandidateId, sectionId);
        await ActAsync(room.Invigilator, room.First, sectionId, new { action = "EndNow", candidateIds = new[] { room.First.CandidateId }, disregardExtraTime = true });
        var disregarded = await api.GetAttemptAsync(room.First.CandidateId, sectionId);

        Assert.Equal(AttemptStatus.InProgress, keeping.Status);
        Assert.Equal(AttemptStatus.Submitted, disregarded.Status);
    }

    [Fact]
    public async Task RewindAudio_MovesTheListeningBackByAtMostWhatHasPlayed()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var listening = room.First.ListeningSectionId;
        await api.StartAttemptAsync(room.First.CandidateId, listening, durationMinutes: 30, audioPlayedMinutes: 3);
        await api.StartAttemptAsync(room.SecondId, listening, durationMinutes: 30, audioPlayedMinutes: 3);

        var rewind = await ActAsync(room.Invigilator, room.First, listening, new { action = "RewindAudio", candidateIds = new[] { room.First.CandidateId }, minutes = 1, reason = "Headset fault" });
        var tooFar = await ActAsync(room.Invigilator, room.First, listening, new { action = "RewindAudio", candidateIds = new[] { room.SecondId }, minutes = 5 });

        Assert.Equal(1, rewind.Applied);
        Assert.Equal(60_000, (await api.GetAttemptAsync(room.First.CandidateId, listening)).AudioAdjustMs);
        Assert.Contains("played", Assert.Single(tooFar.Skipped).Reason);
    }

    [Fact]
    public async Task Actions_RefuseUnknownActionsAndCandidatesOfAnotherSession()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var stranger = await api.CreateCandidateAsync(Password);
        var url = $"/api/staff/exams/{room.First.ExamId}/sections/{room.First.ReadingWritingSectionId}/actions";

        var unknown = await room.Invigilator.PostAsJsonAsync(url, new { action = "Explode", candidateIds = new[] { room.First.CandidateId } }, ct);
        var foreign = await room.Invigilator.PostAsJsonAsync(url, new { action = "Pause", candidateIds = new[] { stranger.CandidateId } }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
    }

    [Fact]
    public async Task Messages_ReachTheWholeRoomOrOneCandidateThroughTheHeartbeat()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var sectionId = room.First.ReadingWritingSectionId;
        using var candidate = api.CreateClient();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(room.First.Username, Password), ct)).EnsureSuccessStatusCode();
        (await candidate.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(room.First.SessionPin), ct)).EnsureSuccessStatusCode();
        (await candidate.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        var toRoom = await room.Invigilator.PostAsJsonAsync($"/api/staff/exams/{room.First.ExamId}/messages", new { text = "Thirty minutes left." }, ct);
        var toOther = await room.Invigilator.PostAsJsonAsync($"/api/staff/exams/{room.First.ExamId}/messages", new { text = "Please sit down.", candidateIds = new[] { room.SecondId } }, ct);
        var empty = await room.Invigilator.PostAsJsonAsync($"/api/staff/exams/{room.First.ExamId}/messages", new { text = "  " }, ct);
        var beat = await candidate.PostAsync($"/api/candidate/sections/{sectionId}/heartbeat", null, ct);
        var monitor = await MonitorAsync(room.Invigilator, room.First, sectionId);

        Assert.Equal(HttpStatusCode.Created, toRoom.StatusCode);
        Assert.Equal(HttpStatusCode.Created, toOther.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        var state = await beat.Content.ReadFromJsonAsync<CandidateAttemptState>(ct);
        Assert.Equal(["Thirty minutes left."], state!.Messages.Select(m => m.Text));
        Assert.True(monitor!.Candidates.Single(c => c.CandidateId == room.First.CandidateId).IsOnline);
    }
}
