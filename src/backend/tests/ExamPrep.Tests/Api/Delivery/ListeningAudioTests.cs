using System.Net;
using System.Net.Http.Json;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-10: the Listening recording is played once, as in the exam room. The server remembers when the candidate
/// pressed Play, so a reload or a change of computer resumes where the recording is, never from the start.
/// </summary>
public sealed class ListeningAudioTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record AttemptState(int SectionId, string Status, int RemainingSeconds, int? AudioPositionMs);

    /// <summary>A candidate who has finished Reading &amp; Writing and is now in the Listening section.</summary>
    private async Task<(HttpClient Client, ApiFixture.SeededCandidate Candidate)> InListeningAsync(bool start = true)
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        await api.SubmitSectionAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        if (start)
        {
            (await client.PostAsync($"/api/candidate/sections/{candidate.ListeningSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        }

        return (client, candidate);
    }

    private Task<HttpResponseMessage> PlayAsync(HttpClient client, int sectionId) =>
        client.PostAsync($"/api/candidate/sections/{sectionId}/audio", null, ct);

    private async Task<AttemptState> StateAsync(HttpClient client, int sectionId) =>
        (await client.GetFromJsonAsync<AttemptState>($"/api/candidate/sections/{sectionId}/attempt", ct))!;

    [Fact]
    public async Task BeforePlay_ThereIsNoAudioPosition()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await InListeningAsync();
        using var _ = client;

        var state = await StateAsync(client, candidate.ListeningSectionId);

        Assert.Null(state.AudioPositionMs);
    }

    [Fact]
    public async Task Play_StartsTheRecordingClockOnTheServer()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await InListeningAsync();
        using var _ = client;

        var play = await PlayAsync(client, candidate.ListeningSectionId);
        var atStart = await play.Content.ReadFromJsonAsync<AttemptState>(ct);
        await api.MoveAudioStartBackAsync(candidate.CandidateId, candidate.ListeningSectionId, TimeSpan.FromSeconds(90));
        var later = await StateAsync(client, candidate.ListeningSectionId);

        Assert.Equal(HttpStatusCode.OK, play.StatusCode);
        Assert.InRange(atStart!.AudioPositionMs!.Value, 0, 3_000);
        Assert.InRange(later.AudioPositionMs!.Value, 90_000, 93_000);
    }

    [Fact]
    public async Task PlayAgain_AfterAReload_DoesNotStartTheRecordingOver()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await InListeningAsync();
        using var _ = client;
        await PlayAsync(client, candidate.ListeningSectionId);
        await api.MoveAudioStartBackAsync(candidate.CandidateId, candidate.ListeningSectionId, TimeSpan.FromSeconds(90));

        var again = await (await PlayAsync(client, candidate.ListeningSectionId)).Content.ReadFromJsonAsync<AttemptState>(ct);

        Assert.InRange(again!.AudioPositionMs!.Value, 90_000, 93_000);
    }

    [Fact]
    public async Task Play_Returns400_ForASectionWithoutAudio()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/candidate/sections/{candidate.ReadingWritingSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        var response = await PlayAsync(client, candidate.ReadingWritingSectionId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Play_Returns409_BeforeTheSectionIsStarted()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await InListeningAsync(start: false);
        using var _ = client;

        var response = await PlayAsync(client, candidate.ListeningSectionId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
