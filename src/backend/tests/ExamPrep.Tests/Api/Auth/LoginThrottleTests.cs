using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>
/// VE-30: repeated wrong passwords or PINs lock that account (not the computer: a whole exam room may share one
/// address) for a few minutes, so a 4-digit PIN or a printed password cannot be guessed by trying them all.
/// </summary>
public sealed class LoginThrottleTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt-9";
    private const int AllowedFailures = 10;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private Task<HttpResponseMessage> StaffLoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(username, password), ct);

    private Task<HttpResponseMessage> CandidateLoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(username, password), ct);

    [Fact]
    public async Task StaffLogin_IsRefusedAfterTooManyWrongPasswords_EvenWithTheRightOne()
    {
        api.SkipIfUnavailable();
        var target = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        var other = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = api.CreateClient();

        for (var i = 0; i < AllowedFailures; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await StaffLoginAsync(client, target.Username, "wrong")).StatusCode);
        }

        var locked = await StaffLoginAsync(client, target.Username, Password);
        var otherAccount = await StaffLoginAsync(client, other.Username, Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.Contains("Too many", await locked.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.OK, otherAccount.StatusCode);
    }

    [Fact]
    public async Task StaffLogin_ASuccessfulSignInClearsEarlierMistakes()
    {
        api.SkipIfUnavailable();
        var user = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var client = api.CreateClient();

        for (var i = 0; i < AllowedFailures - 1; i++)
        {
            await StaffLoginAsync(client, user.Username, "wrong");
        }

        Assert.Equal(HttpStatusCode.OK, (await StaffLoginAsync(client, user.Username, Password)).StatusCode);
        for (var i = 0; i < AllowedFailures - 1; i++)
        {
            await StaffLoginAsync(client, user.Username, "wrong");
        }

        Assert.Equal(HttpStatusCode.OK, (await StaffLoginAsync(client, user.Username, Password)).StatusCode);
    }

    [Fact]
    public async Task CandidateLogin_IsRefusedAfterTooManyWrongPasswords()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();

        for (var i = 0; i < AllowedFailures; i++)
        {
            await CandidateLoginAsync(client, candidate.Username, "wrong");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await CandidateLoginAsync(client, candidate.Username, Password)).StatusCode);
    }

    [Fact]
    public async Task Pin_IsRefusedAfterTooManyWrongGuesses_SoThe4DigitPinCannotBeTriedOut()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();
        (await CandidateLoginAsync(client, candidate.Username, Password)).EnsureSuccessStatusCode();

        for (var i = 0; i < AllowedFailures; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest($"{i:D4}9"), ct)).StatusCode);
        }

        var right = await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, right.StatusCode);
    }
}
