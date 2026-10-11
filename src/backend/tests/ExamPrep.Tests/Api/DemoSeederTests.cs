using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Demo;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api;

/// <summary>
/// Development only: the demo seeder imports the sample papers and opens an A2 exam with one candidate,
/// so the candidate screen can be tried without an admin portal.
/// </summary>
public sealed class DemoSeederTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record Section(int Id, int OrderNo, string Title, int DurationMinutes, string Status);

    private static DemoOptions Options() => new()
    {
        Enabled = true,
        SamplesRoot = Path.Combine(AppContext.BaseDirectory, "Samples"),
        ExamName = $"Demo {Guid.NewGuid():N}",
        CandidateUsername = $"demo{Guid.NewGuid():N}"[..14],
        CandidatePassword = "demo-pass-1",
        SessionPin = "4821",
    };

    [Fact]
    public async Task Seed_OpensAnA2ExamTheDemoCandidateCanTake()
    {
        api.SkipIfUnavailable();
        var options = Options();

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(options.CandidateUsername, options.CandidatePassword), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(options.SessionPin), ct)).EnsureSuccessStatusCode();
        var sections = (await client.GetFromJsonAsync<List<Section>>("/api/candidate/sections", ct))!;
        Assert.Equal(["Reading & Writing", "Listening"], sections.Select(s => s.Title));

        var paper = await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sections[0].Id}/paper", ct);
        Assert.Equal(7, paper!["parts"]!.AsArray().Count);
    }

    [Fact]
    public async Task Seed_DoesNothingWhenTheDemoExamExists()
    {
        api.SkipIfUnavailable();
        var options = Options();

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);
        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        Assert.Equal(1, await db.Exams.CountAsync(e => e.Name == options.ExamName, ct));
    }

    [Fact]
    public async Task Seed_DoesNothingWhenDisabled()
    {
        api.SkipIfUnavailable();
        var options = Options() with { Enabled = false };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        Assert.False(await db.Exams.AnyAsync(e => e.Name == options.ExamName, ct));
    }
}
