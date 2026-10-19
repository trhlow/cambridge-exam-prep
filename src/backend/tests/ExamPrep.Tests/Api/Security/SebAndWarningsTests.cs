using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Security;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Security;

/// <summary>
/// VE-28: an exam that stores a Safe Exam Browser Config Key accepts candidates only through SEB with those
/// settings. VE-29: what the server observes (a request from outside SEB, a computer that stopped responding, a
/// computer clock far off) appears in the invigilator's Warnings column until resolved.
/// </summary>
public sealed class SebAndWarningsTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Seb#2026-pass";
    private const string ConfigKey = "3f7a0c5e9b2d4f6a8c1e3b5d7f9a2c4e6b8d0f1a3c5e7b9d2f4a6c8e0b1d3f5a";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>What SEB adds to every request: SHA-256 of the absolute URL followed by the Config Key.</summary>
    private sealed class SebHandler(string configKey) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(SebGuard.ConfigKeyHashHeader, SebGuard.ExpectedHash(request.RequestUri!.AbsoluteUri, configKey));
            return base.SendAsync(request, cancellationToken);
        }
    }

    private HttpClient Browser(string? configKey) => configKey is null
        ? api.Factory.CreateDefaultClient(new CookieContainerHandler())
        : api.Factory.CreateDefaultClient(new CookieContainerHandler(), new SebHandler(configKey));

    private async Task<ApiFixture.SeededCandidate> SebExamCandidateAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password);
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        await db.Exams.Where(e => e.Id == candidate.ExamId).ExecuteUpdateAsync(s => s.SetProperty(e => e.SebConfigKeyHash, ConfigKey), ct);
        return candidate;
    }

    private async Task SignInAsync(HttpClient client, ApiFixture.SeededCandidate candidate)
    {
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = candidate.SessionPin }, ct)).EnsureSuccessStatusCode();
    }

    private async Task<List<JsonObject>> WarningsAsync(HttpClient staff, ApiFixture.SeededCandidate candidate) =>
        (await staff.GetFromJsonAsync<List<JsonObject>>($"/api/staff/exams/{candidate.ExamId}/sections/{candidate.ReadingWritingSectionId}/warnings", ct))!;

    private async Task<int> WarningCountAsync(HttpClient staff, ApiFixture.SeededCandidate candidate)
    {
        var view = await staff.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{candidate.ExamId}/sections/{candidate.ReadingWritingSectionId}/monitor", ct);
        return (int)view!["candidates"]!.AsArray().Single(r => (int)r!["candidateId"]! == candidate.CandidateId)!["warnings"]!;
    }

    [Fact]
    public async Task AnExamRequiringSeb_RefusesOtherBrowsers_AndAcceptsSebWithItsSettings()
    {
        api.SkipIfUnavailable();
        var candidate = await SebExamCandidateAsync();
        using var chrome = Browser(null);
        using var otherSettings = Browser(new string('a', 64));
        using var seb = Browser(ConfigKey);

        var fromChrome = await chrome.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct);
        var fromOtherSeb = await otherSettings.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct);
        await SignInAsync(seb, candidate);
        var sections = await seb.GetAsync("/api/candidate/sections", ct);

        Assert.Equal(HttpStatusCode.Forbidden, fromChrome.StatusCode);
        Assert.Equal(SebGuard.RequiredTitle, (await fromChrome.Content.ReadFromJsonAsync<JsonObject>(ct))!["title"]!.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, fromOtherSeb.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sections.StatusCode);
    }

    [Fact]
    public async Task LeavingSebDuringTheTest_IsRefused_AndRaisesOneWarning()
    {
        api.SkipIfUnavailable();
        var candidate = await SebExamCandidateAsync();
        using var seb = Browser(ConfigKey);
        await SignInAsync(seb, candidate);
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        using var admin = await api.AdminClientAsync();

        // A hash for other settings goes first, so the server reads that one (as from a browser outside this SEB).
        var outside = await seb.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/candidate/sections") { Headers = { { SebGuard.ConfigKeyHashHeader, new string('0', 64) } } }, ct);
        var again = await seb.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/candidate/sections") { Headers = { { SebGuard.ConfigKeyHashHeader, new string('0', 64) } } }, ct);

        Assert.Equal(HttpStatusCode.Forbidden, outside.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, again.StatusCode);
        var warnings = await WarningsAsync(admin, candidate);
        Assert.Single(warnings);
        Assert.Equal("InvalidSebKey", (string)warnings[0]["type"]!);
        Assert.Equal(1, await WarningCountAsync(admin, candidate));
    }

    [Fact]
    public async Task AComputerClockFarOff_RaisesAWarning_WhichTheInvigilatorResolves()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        using var browser = Browser(null);
        await SignInAsync(browser, candidate);
        using var admin = await api.AdminClientAsync();
        var tenMinutesBehind = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds().ToString();

        for (var i = 0; i < 2; i++)
        {
            using var beat = new HttpRequestMessage(HttpMethod.Post, $"/api/candidate/sections/{candidate.ReadingWritingSectionId}/heartbeat");
            beat.Headers.Add("X-Client-Time", tenMinutesBehind);
            (await browser.SendAsync(beat, ct)).EnsureSuccessStatusCode();
        }

        var warnings = await WarningsAsync(admin, candidate);
        var resolve = await admin.PostAsync($"/api/staff/exams/{candidate.ExamId}/sections/{candidate.ReadingWritingSectionId}/warnings/{(long)warnings[0]["id"]!}/resolve", null, ct);
        var resolved = await resolve.Content.ReadFromJsonAsync<JsonObject>(ct);

        Assert.Single(warnings);
        Assert.Equal("ClockSkew", (string)warnings[0]["type"]!);
        Assert.Contains("10 min behind", (string)warnings[0]["detail"]!);
        Assert.NotNull(resolved!["resolvedAt"]);
        Assert.False(string.IsNullOrEmpty((string?)resolved["resolvedBy"]));
        Assert.Equal(0, await WarningCountAsync(admin, candidate));
    }

    [Fact]
    public async Task AComputerThatStopsResponding_RaisesAConnectionWarning()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var attemptId = await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var lastBeat = DateTime.UtcNow.AddSeconds(-14);
            await db.Attempts.Where(a => a.Id == attemptId).ExecuteUpdateAsync(s => s.SetProperty(a => a.LastHeartbeatAt, lastBeat), ct);
        }

        using var admin = await api.AdminClientAsync();
        List<JsonObject> warnings = [];
        for (var waited = 0; waited < 8_000 && warnings.Count == 0; waited += 500)
        {
            await Task.Delay(500, ct);
            warnings = await WarningsAsync(admin, candidate);
        }

        Assert.Single(warnings);
        Assert.Equal("ConnectionLost", (string)warnings[0]["type"]!);
    }

    [Fact]
    public async Task AnnouncingTheStart_WarnsAboutCandidatesWhoStartedBefore_Once()
    {
        api.SkipIfUnavailable();
        var early = await api.CreateCandidateAsync(Password);
        var section = early.ReadingWritingSectionId;
        await api.StartAttemptAsync(early.CandidateId, section);
        var (onTimeId, _) = await api.AddCandidateAsync(early.ExamId, Password, "Chi", "Le");
        using var admin = await api.AdminClientAsync();
        var announceUrl = $"/api/staff/exams/{early.ExamId}/sections/{section}/announce-start";

        var first = await (await admin.PostAsync(announceUrl, null, ct)).Content.ReadFromJsonAsync<JsonObject>(ct);
        await api.StartAttemptAsync(onTimeId, section);
        var second = await (await admin.PostAsync(announceUrl, null, ct)).Content.ReadFromJsonAsync<JsonObject>(ct);
        var warnings = await WarningsAsync(admin, early);

        Assert.NotNull(first!["startAnnouncedAt"]);
        Assert.Equal((string)first["startAnnouncedAt"]!, (string)second!["startAnnouncedAt"]!);
        var warning = Assert.Single(warnings);
        Assert.Equal("StartedEarly", (string)warning["type"]!);
        Assert.Equal(early.CandidateId, (int)warning["candidateId"]!);
    }

    [Fact]
    public async Task TheExamForm_TakesAConfigKey_AndShowsItOnlyToThoseWhoRunTheRoom()
    {
        api.SkipIfUnavailable();
        var (readingWriting, listening) = await api.CreatePublishedPapersAsync();
        using var admin = await api.AdminClientAsync();
        object Form(string? key) => new
        {
            name = "SEB session",
            examTypeId = 1,
            scheduledDate = "2026-11-02",
            papers = new[] { new { sectionTemplateId = 1, testPaperId = readingWriting }, new { sectionTemplateId = 2, testPaperId = listening } },
            sebConfigKey = key,
        };

        var invalid = await admin.PostAsJsonAsync("/api/admin/exams", Form("not-a-key"), ct);
        var created = await admin.PostAsJsonAsync("/api/admin/exams", Form(ConfigKey.ToUpperInvariant()), ct);
        var detail = await created.Content.ReadFromJsonAsync<JsonObject>(ct);
        var examId = (int)detail!["id"]!;
        var cleared = await admin.PutAsJsonAsync($"/api/admin/exams/{examId}", Form(""), ct);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(ConfigKey, (string)detail["sebConfigKey"]!);
        Assert.Null((await cleared.Content.ReadFromJsonAsync<JsonObject>(ct))!["sebConfigKey"]);
    }
}
