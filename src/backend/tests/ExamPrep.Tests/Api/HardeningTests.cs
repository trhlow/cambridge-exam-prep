using System.Net.Http.Json;
using ExamPrep.Api.Admin;
using ExamPrep.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api;

/// <summary>Defences around the whole API: response headers, and keys that survive a restart.</summary>
public sealed class HardeningTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/staff/auth/login", new { username = "nobody", password = "x" }, ct);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public void CandidatePasswords_CanStillBeReadForTheSlipsAfterARestart()
    {
        api.SkipIfUnavailable();
        var candidate = new Candidate { CandidateNumber = "1", FirstName = "An", LastName = "Le", Username = "x", PasswordHash = "", PasswordProtected = "" };
        using (var scope = api.Factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CandidateCredentials>().SetPassword(candidate, "slip-pass-1");
        }

        // A second host with the same settings stands in for the API after a restart (or a new container).
        using var restarted = api.CreateFactory();
        using var restartedScope = restarted.Services.CreateScope();

        Assert.Equal("slip-pass-1", restartedScope.ServiceProvider.GetRequiredService<CandidateCredentials>().Reveal(candidate));
        Assert.NotEmpty(Directory.GetFiles(api.KeysPath, "key-*.xml"));
    }
}
