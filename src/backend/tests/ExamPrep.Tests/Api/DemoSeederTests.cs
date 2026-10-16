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
    public async Task Seed_WithResetOnStart_GivesTheDemoCandidateAFreshExam()
    {
        api.SkipIfUnavailable();
        var options = Options();
        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);
        int firstExamId;
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var exam = await db.Exams.Include(e => e.Sections).Include(e => e.Candidates).SingleAsync(e => e.Name == options.ExamName, ct);
            firstExamId = exam.Id;
            await api.SubmitSectionAsync(exam.Candidates.Single().Id, exam.Sections.Single(s => s.OrderNo == 1).Id);
        }

        await DemoSeeder.SeedAsync(api.Factory.Services, options with { ResetOnStart = true }, ct);

        using var check = api.Factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var fresh = await checkDb.Exams.Include(e => e.Candidates).ThenInclude(c => c.Attempts).SingleAsync(e => e.Name == options.ExamName, ct);
        Assert.NotEqual(firstExamId, fresh.Id);
        Assert.Empty(fresh.Candidates.Single().Attempts);
    }

    [Fact]
    public async Task Seed_UsesTheReadingWritingFolderWhenItExists()
    {
        api.SkipIfUnavailable();
        var folder = Directory.CreateTempSubdirectory("demo-rw-").FullName;
        var sample = Path.Combine(AppContext.BaseDirectory, "Samples", "a2-key-fs-reading-writing", "paper.json");
        var paper = JsonNode.Parse(await File.ReadAllTextAsync(sample, ct))!;
        paper["title"] = "Converted paper";
        await File.WriteAllTextAsync(Path.Combine(folder, "paper.json"), paper.ToJsonString(), ct);
        var options = Options() with { ReadingWritingFolder = folder };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        Assert.Equal("Converted paper", await ReadingWritingTitleAsync(options.ExamName));
    }

    [Fact]
    public async Task Seed_FallsBackToTheSampleWhenTheReadingWritingFolderIsMissing()
    {
        api.SkipIfUnavailable();
        var options = Options() with { ReadingWritingFolder = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}") };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        Assert.StartsWith("A2 Key for Schools – Victory sample test 1", await ReadingWritingTitleAsync(options.ExamName));
    }

    [Fact]
    public async Task Seed_UsesTheListeningFolderWhenItExists()
    {
        api.SkipIfUnavailable();
        var folder = Directory.CreateTempSubdirectory("demo-l-").FullName;
        var sample = Path.Combine(AppContext.BaseDirectory, "Samples", "a2-key-fs-listening", "paper.json");
        var paper = JsonNode.Parse(await File.ReadAllTextAsync(sample, ct))!;
        paper["title"] = "Converted listening";
        await File.WriteAllTextAsync(Path.Combine(folder, "paper.json"), paper.ToJsonString(), ct);
        var options = Options() with { ListeningFolder = folder };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        Assert.Equal("Converted listening", await SectionTitleAsync(options.ExamName, 2));
    }

    private Task<string> ReadingWritingTitleAsync(string examName) => SectionTitleAsync(examName, 1);

    private async Task<string> SectionTitleAsync(string examName, int orderNo)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.ExamSections
            .Where(s => s.Exam!.Name == examName && s.OrderNo == orderNo)
            .Select(s => s.TestPaper!.Title)
            .SingleAsync(ct);
    }

    [Fact]
    public async Task Seed_CreatesADemoInvigilatorWhoSeesTheDemoExam()
    {
        api.SkipIfUnavailable();
        var username = $"invdemo{Guid.NewGuid():N}"[..14];
        var options = Options() with { InvigilatorUsername = username, InvigilatorPassword = "demo-invig-1" };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);
        await DemoSeeder.SeedAsync(api.Factory.Services, options with { ResetOnStart = true }, ct);

        using var client = await api.StaffClientAsync(username, "demo-invig-1");
        var exams = await client.GetFromJsonAsync<List<JsonObject>>("/api/staff/exams", ct);
        Assert.Equal([options.ExamName], exams!.Select(e => (string)e["name"]!));
    }

    [Fact]
    public async Task Seed_CreatesADemoAdministratorWhoCanSignIn()
    {
        api.SkipIfUnavailable();
        var username = $"admdemo{Guid.NewGuid():N}"[..14];
        var options = Options() with { AdminUsername = username, AdminPassword = "19072005" };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);
        await DemoSeeder.SeedAsync(api.Factory.Services, options with { ResetOnStart = true }, ct);

        using var client = await api.StaffClientAsync(username, "19072005");
        var me = await client.GetFromJsonAsync<JsonObject>("/api/staff/auth/me", ct);
        Assert.Equal("Admin", (string)me!["role"]!);
    }

    [Fact]
    public async Task Seed_CreatesASecondAdmin_AManagerOfEveryArea_AndAnExaminerOfTheDemoExam()
    {
        api.SkipIfUnavailable();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var options = Options() with
        {
            SecondAdminUsername = $"adm2{suffix}", SecondAdminPassword = "19072005",
            ManagerUsername = $"mgr{suffix}", ManagerPassword = "19072005",
            ExaminerUsername = $"exm{suffix}", ExaminerPassword = "19072005",
        };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        using var secondAdmin = await api.StaffClientAsync(options.SecondAdminUsername, "19072005");
        using var manager = await api.StaffClientAsync(options.ManagerUsername, "19072005");
        using var examiner = await api.StaffClientAsync(options.ExaminerUsername, "19072005");
        Assert.Equal("Admin", (string)(await secondAdmin.GetFromJsonAsync<JsonObject>("/api/staff/auth/me", ct))!["role"]!);
        var managerAreas = (await manager.GetFromJsonAsync<JsonObject>("/api/staff/auth/me", ct))!["areas"]!.AsArray().Select(a => (string)a!);
        Assert.Equal(["Papers", "Exams", "Candidates", "Centre"], managerAreas);
        var examinerExams = await examiner.GetFromJsonAsync<List<JsonObject>>("/api/staff/exams", ct);
        Assert.Equal([options.ExamName], examinerExams!.Select(e => (string)e["name"]!));
    }

    [Fact]
    public async Task Seed_GivesTheDemoCandidateTheConfiguredNameAndDateOfBirth()
    {
        api.SkipIfUnavailable();
        var options = Options() with { CandidateFirstName = "Hoàng Long", CandidateLastName = "Trần", CandidateDateOfBirth = new DateOnly(2005, 7, 19) };

        await DemoSeeder.SeedAsync(api.Factory.Services, options, ct);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var candidate = await db.Candidates.SingleAsync(c => c.Username == options.CandidateUsername, ct);
        Assert.Equal(("Hoàng Long", "Trần", new DateOnly(2005, 7, 19)), (candidate.FirstName, candidate.LastName, candidate.DateOfBirth));
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
