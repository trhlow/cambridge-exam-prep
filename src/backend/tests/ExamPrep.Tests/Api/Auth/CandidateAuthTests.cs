using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>
/// VE-4: candidate sign-in as in the Cambridge exam room — printed username and password, then the
/// session PIN the invigilator reads out. Sections are only visible after the PIN.
/// </summary>
public sealed class CandidateAuthTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record CandidateProfile(string CandidateNumber, string FullName, string ExamName, bool PinVerified);

    private sealed record CandidateSection(int Id, int OrderNo, string Title, int DurationMinutes, string Status);

    private Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(username, password), ct);

    private Task<HttpResponseMessage> EnterPinAsync(HttpClient client, string pin) =>
        client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(pin), ct);

    private Task<HttpResponseMessage> GetSectionsAsync(HttpClient client) =>
        client.GetAsync("/api/candidate/sections", ct);

    private async Task<HttpClient> SignInWithPinAsync(ApiFixture.SeededCandidate candidate)
    {
        var client = api.CreateClient();
        (await LoginAsync(client, candidate.Username, Password)).EnsureSuccessStatusCode();
        (await EnterPinAsync(client, candidate.SessionPin)).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Login_ReturnsProfileWaitingForPin_WhenCredentialsAreValid()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();

        var response = await LoginAsync(client, candidate.Username, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<CandidateProfile>(ct);
        Assert.Equal("0001", profile!.CandidateNumber);
        Assert.Equal("An Nguyen", profile.FullName);
        Assert.False(profile.PinVerified);
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsWrong()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();

        var response = await LoginAsync(client, candidate.Username, "wrong-pass");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401_WhenUsernameIsUnknown()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await LoginAsync(client, "nobodyx9999", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("nguyea0001", "")]
    public async Task Login_Returns400_WhenFieldsAreMissing(string username, string password)
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await LoginAsync(client, username, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(ExamStatus.Draft)]
    [InlineData(ExamStatus.Scheduled)]
    [InlineData(ExamStatus.Closed)]
    public async Task Login_Returns403_WhenExamIsNotOpen(ExamStatus status)
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, status);
        using var client = api.CreateClient();

        var response = await LoginAsync(client, candidate.Username, Password);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sections_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await GetSectionsAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sections_Returns403_BeforePinIsEntered()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();
        await LoginAsync(client, candidate.Username, Password);

        var response = await GetSectionsAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pin_Returns400AndKeepsSectionsLocked_WhenPinIsWrong()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();
        await LoginAsync(client, candidate.Username, Password);

        var pin = await EnterPinAsync(client, "0000");
        var sections = await GetSectionsAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, pin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sections.StatusCode);
    }

    [Fact]
    public async Task Pin_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await EnterPinAsync(client, "4821");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sections_ListsA2SectionsInOrder_AfterCorrectPin()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = await SignInWithPinAsync(candidate);

        var sections = await client.GetFromJsonAsync<List<CandidateSection>>("/api/candidate/sections", ct);

        Assert.Collection(
            sections!,
            s =>
            {
                Assert.Equal("Reading & Writing", s.Title);
                Assert.Equal(60, s.DurationMinutes);
                Assert.Equal("NotStarted", s.Status);
            },
            s =>
            {
                Assert.Equal("Listening", s.Title);
                Assert.Equal(30, s.DurationMinutes);
            });
    }

    [Fact]
    public async Task Me_ReportsPinVerified_AfterCorrectPin()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = await SignInWithPinAsync(candidate);

        var profile = await client.GetFromJsonAsync<CandidateProfile>("/api/candidate/auth/me", ct);

        Assert.True(profile!.PinVerified);
    }

    [Fact]
    public async Task Login_OnAnotherComputer_SignsOutTheFirstComputer()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var firstComputer = await SignInWithPinAsync(candidate);
        using var secondComputer = api.CreateClient();

        await LoginAsync(secondComputer, candidate.Username, Password);
        var response = await GetSectionsAsync(firstComputer);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sections_Returns401_AfterLogout()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = await SignInWithPinAsync(candidate);

        var logout = await client.PostAsync("/api/candidate/auth/logout", null, ct);
        var response = await GetSectionsAsync(client);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sections_Returns401_ForStaffCookie()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(ApiFixture.AdminUsername, ApiFixture.AdminPassword), ct);

        var response = await GetSectionsAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaffEndpoints_Return401_ForCandidateCookie()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = await SignInWithPinAsync(candidate);

        var response = await client.GetAsync("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
