using System.Net;
using System.Net.Http.Json;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-8: the server owns the clock, every answer is saved as the candidate works, and a section ends when its
/// time runs out — whether or not the candidate's computer is still connected.
/// </summary>
public sealed class CandidateAttemptTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private const int ToleranceSeconds = 10;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record AttemptState(int SectionId, string Status, DateTime? EndsAt, int RemainingSeconds, DateTime ServerNow, Dictionary<int, string> Answers);

    private sealed record ResponseItem(int QuestionNumber, string Value);

    private sealed record SaveRequest(List<ResponseItem> Responses);

    private sealed record Section(int Id, int OrderNo, string Title, int DurationMinutes, string Status);

    private async Task<(HttpClient Client, ApiFixture.SeededCandidate Candidate)> SignedInAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        return (client, candidate);
    }

    private async Task<AttemptState> StartAsync(HttpClient client, int sectionId)
    {
        var response = await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttemptState>(ct))!;
    }

    private async Task<AttemptState> GetAsync(HttpClient client, int sectionId) =>
        (await client.GetFromJsonAsync<AttemptState>($"/api/candidate/sections/{sectionId}/attempt", ct))!;

    private Task<HttpResponseMessage> SaveAsync(HttpClient client, int sectionId, params ResponseItem[] items) =>
        client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new SaveRequest([.. items]), ct);

    [Fact]
    public async Task Start_RunsTheSectionClockOnTheServer()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;

        var state = await StartAsync(client, candidate.ReadingWritingSectionId);

        Assert.Equal("InProgress", state.Status);
        Assert.InRange(state.RemainingSeconds, 3600 - ToleranceSeconds, 3600);
        Assert.InRange((state.EndsAt!.Value - state.ServerNow).TotalSeconds, 3600 - ToleranceSeconds, 3600 + 1);
    }

    [Fact]
    public async Task Start_AddsTheCandidatesPlannedExtraTime()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        await api.SetPlannedExtraPercentAsync(candidate.CandidateId, 25);

        var state = await StartAsync(client, candidate.ReadingWritingSectionId);

        Assert.InRange(state.RemainingSeconds, 4500 - ToleranceSeconds, 4500);
    }

    [Fact]
    public async Task Start_AgainAfterAReload_ContinuesTheSameClock()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        await StartAsync(client, candidate.ReadingWritingSectionId);
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, candidate.ReadingWritingSectionId, TimeSpan.FromMinutes(10));

        var state = await StartAsync(client, candidate.ReadingWritingSectionId);

        Assert.InRange(state.RemainingSeconds, 3000 - ToleranceSeconds, 3000);
    }

    [Fact]
    public async Task Start_Returns403_ForASectionThatIsStillLocked()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;

        var response = await client.PostAsync($"/api/candidate/sections/{candidate.ListeningSectionId}/attempt", null, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sections_ShowInProgress_OnceStarted()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        await StartAsync(client, candidate.ReadingWritingSectionId);

        var sections = (await client.GetFromJsonAsync<List<Section>>("/api/candidate/sections", ct))!;

        Assert.Equal(["InProgress", "Locked"], sections.Select(s => s.Status));
    }

    [Fact]
    public async Task SavedAnswers_AreThereAfterAReload()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        var section = candidate.ReadingWritingSectionId;
        await StartAsync(client, section);

        var first = await SaveAsync(client, section, new ResponseItem(1, "B"), new ResponseItem(25, "for"), new ResponseItem(31, "Hi Jo, let's see the new film."));
        var change = await SaveAsync(client, section, new ResponseItem(1, "C"));
        var state = await GetAsync(client, section);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal("C", state.Answers[1]);
        Assert.Equal("for", state.Answers[25]);
        Assert.StartsWith("Hi Jo", state.Answers[31]);
    }

    [Fact]
    public async Task Save_Returns400_ForAQuestionThatIsNotInThePaper()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        await StartAsync(client, candidate.ReadingWritingSectionId);

        var response = await SaveAsync(client, candidate.ReadingWritingSectionId, new ResponseItem(99, "x"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_Returns409_BeforeTheSectionIsStarted()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;

        var response = await SaveAsync(client, candidate.ReadingWritingSectionId, new ResponseItem(1, "A"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task WhenTimeRunsOut_TheSectionIsSubmittedAndFurtherAnswersRefused()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        var section = candidate.ReadingWritingSectionId;
        await StartAsync(client, section);
        await SaveAsync(client, section, new ResponseItem(1, "B"));
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, section, TimeSpan.FromMinutes(61));

        var state = await GetAsync(client, section);
        var late = await SaveAsync(client, section, new ResponseItem(2, "A"));

        Assert.Equal("Submitted", state.Status);
        Assert.Equal(0, state.RemainingSeconds);
        Assert.Equal("B", state.Answers[1]);
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Equal(SubmitReason.TimeExpired, (await api.GetAttemptAsync(candidate.CandidateId, section)).SubmitReason);
    }

    [Fact]
    public async Task ExpiredSections_AreSubmittedByTheServer_EvenIfTheCandidateIsOffline()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await SignedInAsync();
        using var _ = client;
        await StartAsync(client, candidate.ReadingWritingSectionId);
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, candidate.ReadingWritingSectionId, TimeSpan.FromMinutes(61));

        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        }

        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        var sections = (await client.GetFromJsonAsync<List<Section>>("/api/candidate/sections", ct))!;
        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.Equal(["Submitted", "NotStarted"], sections.Select(s => s.Status));
    }
}
