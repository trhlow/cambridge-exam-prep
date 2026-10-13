using System.Net;
using System.Net.Http.Json;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>
/// VE-18: the Options screen's contrast and text size are kept on the server, so they follow the candidate after a
/// reload or onto another computer (Inspera keeps them for the whole test).
/// </summary>
public sealed class CandidatePreferencesTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record Profile(string CandidateNumber, string FullName, string ExamName, bool PinVerified, string TextSize, string Contrast);

    [Fact]
    public async Task Preferences_AreSavedAndComeBackWithTheProfile()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();
        var login = await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct);
        var fresh = await login.Content.ReadFromJsonAsync<Profile>(ct);

        var saved = await client.PutAsJsonAsync("/api/candidate/auth/preferences", new { textSize = "Large", contrast = "YellowOnBlack" }, ct);
        using var otherComputer = api.CreateClient();
        var again = await otherComputer.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct);

        Assert.Equal(("Regular", "BlackOnWhite"), (fresh!.TextSize, fresh.Contrast));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var profile = await again.Content.ReadFromJsonAsync<Profile>(ct);
        Assert.Equal(("Large", "YellowOnBlack"), (profile!.TextSize, profile.Contrast));
    }

    [Fact]
    public async Task Preferences_RefuseUnknownValuesAndAnonymousCallers()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        using var anonymous = api.CreateClient();

        var bogus = await client.PutAsJsonAsync("/api/candidate/auth/preferences", new { textSize = "Huge", contrast = "BlackOnWhite" }, ct);
        var noSession = await anonymous.PutAsJsonAsync("/api/candidate/auth/preferences", new { textSize = "Large", contrast = "BlackOnWhite" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, bogus.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
    }
}
