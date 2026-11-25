using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Realtime;
using ExamPrep.Api.Security;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Realtime;

/// <summary>
/// VE-13: the invigilator's page changes within 2 seconds when a candidate comes online, answers, is paused or
/// leaves, and the candidate's screen hears about an invigilator's action at once instead of at the next heartbeat.
/// </summary>
public sealed class RealtimeTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Live#2026-pass";
    private static readonly TimeSpan Soon = TimeSpan.FromSeconds(2);
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private static string CookieOf(HttpResponseMessage response) =>
        string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(v => v.Split(';')[0]));

    private HttpClient ClientWith(string cookie)
    {
        var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    private async Task<string> StaffCookieAsync(string username, string password)
    {
        using var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/staff/auth/login", new { username, password }, ct);
        login.EnsureSuccessStatusCode();
        return CookieOf(login);
    }

    private async Task<string> CandidateCookieAsync(ApiFixture.SeededCandidate candidate)
    {
        using var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct);
        login.EnsureSuccessStatusCode();
        using var pin = new HttpRequestMessage(HttpMethod.Post, "/api/candidate/auth/pin") { Content = JsonContent.Create(new { pin = candidate.SessionPin }) };
        pin.Headers.Add("Cookie", CookieOf(login));
        var pinned = await client.SendAsync(pin, ct);
        pinned.EnsureSuccessStatusCode();
        return CookieOf(pinned);
    }

    /// <summary>What Safe Exam Browser adds to every request: SHA-256 of the absolute URL followed by the Config Key.</summary>
    private sealed class SebHandler(string configKey, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(SebGuard.ConfigKeyHashHeader, SebGuard.ExpectedHash(request.RequestUri!.AbsoluteUri, configKey));
            return base.SendAsync(request, cancellationToken);
        }
    }

    private async Task<HubConnection> ConnectAsync(string path, string cookie, string? sebConfigKey = null)
    {
        var server = api.Factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, path), options =>
            {
                options.HttpMessageHandlerFactory = _ => sebConfigKey is null ? server.CreateHandler() : new SebHandler(sebConfigKey, server.CreateHandler());
                options.Transports = HttpTransportType.LongPolling;
                options.Headers["Cookie"] = cookie;
            })
            .Build();
        await connection.StartAsync(ct);
        return connection;
    }

    private static TaskCompletionSource<int> Listen(HubConnection connection, string method)
    {
        var heard = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<int>(method, id => heard.TrySetResult(id));
        return heard;
    }

    private async Task<HubConnection> WatchAsync(ApiFixture.SeededCandidate candidate, int sectionId)
    {
        var staff = await ConnectAsync(StaffRoomHub.Path, await StaffCookieAsync(ApiFixture.AdminUsername, ApiFixture.AdminPassword));
        Assert.True(await staff.InvokeAsync<bool>("WatchSection", candidate.ExamId, sectionId, ct));
        return staff;
    }

    private async Task<JsonObject> MonitorRowAsync(ApiFixture.SeededCandidate candidate, int sectionId)
    {
        using var admin = await api.AdminClientAsync();
        var view = await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{candidate.ExamId}/sections/{sectionId}/monitor", ct);
        return view!["candidates"]!.AsArray().Single(r => (int)r!["candidateId"]! == candidate.CandidateId)!.AsObject();
    }

    [Fact]
    public async Task Invigilator_IsToldWithinTwoSeconds_WhenACandidateComesOnline()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var section = candidate.ReadingWritingSectionId;
        await api.StartAttemptAsync(candidate.CandidateId, section, isOnline: false);
        await using var staff = await WatchAsync(candidate, section);
        var heard = Listen(staff, RoomNotifier.SectionChangedMethod);
        using var browser = ClientWith(await CandidateCookieAsync(candidate));

        (await browser.PostAsync($"/api/candidate/sections/{section}/heartbeat", null, ct)).EnsureSuccessStatusCode();

        Assert.Equal(section, await heard.Task.WaitAsync(Soon, ct));
    }

    [Fact]
    public async Task Heartbeat_OfACandidateAlreadyOnline_KeepsThemOnlineWithoutTellingTheInvigilator()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var section = candidate.ReadingWritingSectionId;
        await api.StartAttemptAsync(candidate.CandidateId, section);
        var before = (await api.GetAttemptAsync(candidate.CandidateId, section)).LastHeartbeatAt;
        await using var staff = await WatchAsync(candidate, section);
        var heard = Listen(staff, RoomNotifier.SectionChangedMethod);
        using var browser = ClientWith(await CandidateCookieAsync(candidate));
        await Task.Delay(20, ct);

        (await browser.PostAsync($"/api/candidate/sections/{section}/heartbeat", null, ct)).EnsureSuccessStatusCode();

        var after = await api.GetAttemptAsync(candidate.CandidateId, section);
        Assert.True(after.LastHeartbeatAt > before);
        Assert.Equal(ConnectionStatus.Online, after.ConnectionStatus);
        await Assert.ThrowsAsync<TimeoutException>(() => heard.Task.WaitAsync(TimeSpan.FromSeconds(1.5), ct));
    }

    [Fact]
    public async Task Invigilator_IsToldWithinTwoSeconds_WhenACandidateSavesAnAnswer()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var section = candidate.ReadingWritingSectionId;
        using var browser = ClientWith(await CandidateCookieAsync(candidate));
        (await browser.PostAsync($"/api/candidate/sections/{section}/attempt", null, ct)).EnsureSuccessStatusCode();
        await using var staff = await WatchAsync(candidate, section);
        var heard = Listen(staff, RoomNotifier.SectionChangedMethod);

        (await browser.PutAsJsonAsync($"/api/candidate/sections/{section}/responses", new { responses = new[] { new { questionNumber = 1, value = "A" } } }, ct)).EnsureSuccessStatusCode();

        Assert.Equal(section, await heard.Task.WaitAsync(Soon, ct));
    }

    [Fact]
    public async Task Candidate_IsToldAtOnce_WhenTheInvigilatorPausesThem()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var section = candidate.ReadingWritingSectionId;
        await api.StartAttemptAsync(candidate.CandidateId, section);
        await using var screen = await ConnectAsync(CandidateRoomHub.Path, await CandidateCookieAsync(candidate));
        var refresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        screen.On(RoomNotifier.RefreshMethod, () => refresh.TrySetResult());
        using var admin = await api.AdminClientAsync();

        var pause = await admin.PostAsJsonAsync($"/api/staff/exams/{candidate.ExamId}/sections/{section}/actions", new { action = "Pause", candidateIds = new[] { candidate.CandidateId } }, ct);

        pause.EnsureSuccessStatusCode();
        await refresh.Task.WaitAsync(Soon, ct);
    }

    [Fact]
    public async Task ClosingTheCandidatesScreen_ShowsThemOfflineAtOnce()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var section = candidate.ReadingWritingSectionId;
        await api.StartAttemptAsync(candidate.CandidateId, section);
        var screen = await ConnectAsync(CandidateRoomHub.Path, await CandidateCookieAsync(candidate));
        var onlineBefore = (bool)(await MonitorRowAsync(candidate, section))["isOnline"]!;
        await using var staff = await WatchAsync(candidate, section);
        var heard = Listen(staff, RoomNotifier.SectionChangedMethod);

        await screen.DisposeAsync();

        Assert.Equal(section, await heard.Task.WaitAsync(Soon, ct));
        Assert.True(onlineBefore);
        Assert.False((bool)(await MonitorRowAsync(candidate, section))["isOnline"]!);
    }

    [Fact]
    public async Task WatchingIsRefused_OutsideTheStaffMembersSessions()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        await using var staff = await ConnectAsync(StaffRoomHub.Path, await StaffCookieAsync(invigilator.Username, Password));

        Assert.False(await staff.InvokeAsync<bool>("WatchSection", candidate.ExamId, candidate.ReadingWritingSectionId, ct));
    }

    [Fact]
    public async Task TheHubs_NeedTheRightSignIn()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var staffCookie = await StaffCookieAsync(ApiFixture.AdminUsername, ApiFixture.AdminPassword);
        var candidateCookie = await CandidateCookieAsync(candidate);

        await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(StaffRoomHub.Path, string.Empty));
        await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(CandidateRoomHub.Path, staffCookie));
        await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(StaffRoomHub.Path, candidateCookie));
    }

    [Fact]
    public async Task AnExamRequiringSeb_TakesTheCandidatesLiveConnectionOnlyFromSeb()
    {
        api.SkipIfUnavailable();
        const string ConfigKey = "3f7a0c5e9b2d4f6a8c1e3b5d7f9a2c4e6b8d0f1a3c5e7b9d2f4a6c8e0b1d3f5a";
        var candidate = await api.CreateCandidateAsync(Password);
        var section = candidate.ReadingWritingSectionId;
        await api.StartAttemptAsync(candidate.CandidateId, section);
        var cookie = await CandidateCookieAsync(candidate);
        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Exams.Where(e => e.Id == candidate.ExamId)
                .ExecuteUpdateAsync(set => set.SetProperty(e => e.SebConfigKeyHash, ConfigKey), ct);
            SebActionFilter.Forget(scope.ServiceProvider.GetRequiredService<IMemoryCache>(), candidate.ExamId);
        }

        // Outside SEB the server closes the connection, at the handshake or right after it.
        var closed = false;
        try
        {
            await using var outside = await ConnectAsync(CandidateRoomHub.Path, cookie);
            var gone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            outside.Closed += _ => { gone.TrySetResult(); return Task.CompletedTask; };
            await gone.Task.WaitAsync(Soon, ct);
            closed = true;
        }
        catch (Exception e) when (e is not TimeoutException)
        {
            closed = true;
        }

        await using var seb = await ConnectAsync(CandidateRoomHub.Path, cookie, ConfigKey);
        var refresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        seb.On(RoomNotifier.RefreshMethod, () => refresh.TrySetResult());
        using var admin = await api.AdminClientAsync();
        (await admin.PostAsJsonAsync($"/api/staff/exams/{candidate.ExamId}/sections/{section}/actions", new { action = "Pause", candidateIds = new[] { candidate.CandidateId } }, ct)).EnsureSuccessStatusCode();

        Assert.True(closed);
        await refresh.Task.WaitAsync(Soon, ct);
        Assert.Equal(HubConnectionState.Connected, seb.State);
    }
}
