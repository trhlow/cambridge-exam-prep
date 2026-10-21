using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-34: the B1 Preliminary for Schools Reading paper (6 parts, 32 questions) imports, shows its two drag-and-drop
/// parts — people matched with long cards (Part 2) and sentences dragged into a text (Part 4) — and is marked.
/// </summary>
public sealed class B1ReadingTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "b1-read-7k";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>A B1 exam (Reading, Writing, Listening) whose Reading section is the self-written B1 sample.</summary>
    private async Task<(HttpClient Client, int CandidateId, int ReadingSectionId)> CandidateAsync()
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var adminId = db.Users.Where(u => u.Username == ApiFixture.AdminUsername).Select(u => u.Id).Single();
        var import = await scope.ServiceProvider.GetRequiredService<PaperPackageImporter>()
            .ImportAsync(new MemoryStream(SamplePackages.Zip(SamplePackages.Load(SamplePackages.B1Reading))), adminId, ct);
        Assert.Empty(import.Errors);
        var now = DateTime.UtcNow;
        var writing = new TestPaper { SectionTemplateId = SeedData.SectionTemplates.Single(t => t.ExamTypeId == SeedData.B1PreliminaryForSchools && t.OrderNo == 2).Id, Title = "B1 W", CreatedByUserId = adminId, CreatedAt = now };
        var listening = new TestPaper { SectionTemplateId = SeedData.SectionTemplates.Single(t => t.ExamTypeId == SeedData.B1PreliminaryForSchools && t.OrderNo == 3).Id, Title = "B1 L", CreatedByUserId = adminId, CreatedAt = now };
        db.TestPapers.AddRange(writing, listening);
        await db.SaveChangesAsync(ct);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var candidate = new Candidate { CandidateNumber = "0001", FirstName = "Lan", LastName = "Pham", Username = $"phaml{suffix}", PasswordHash = string.Empty, PasswordProtected = "-", CreatedAt = now };
        candidate.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>().HashPassword(candidate, Password);
        var templates = SeedData.SectionTemplates.Where(t => t.ExamTypeId == SeedData.B1PreliminaryForSchools).ToDictionary(t => t.OrderNo);
        var exam = new Exam
        {
            Name = $"B1 PfS {suffix}",
            ExamTypeId = SeedData.B1PreliminaryForSchools,
            ScheduledDate = DateOnly.FromDateTime(now),
            Status = ExamStatus.Active,
            SessionPin = "4821",
            InvigilatorPassword = "invig-pass",
            CreatedByUserId = adminId,
            CreatedAt = now,
            Sections =
            [
                new ExamSection { SectionTemplateId = templates[1].Id, TestPaperId = import.Paper!.Id, OrderNo = 1, DurationMinutes = templates[1].DurationMinutes },
                new ExamSection { SectionTemplateId = templates[2].Id, TestPaperId = writing.Id, OrderNo = 2, DurationMinutes = templates[2].DurationMinutes },
                new ExamSection { SectionTemplateId = templates[3].Id, TestPaperId = listening.Id, OrderNo = 3, DurationMinutes = templates[3].DurationMinutes },
            ],
            Candidates = [candidate],
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(ct);

        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = "4821" }, ct)).EnsureSuccessStatusCode();
        return (client, candidate.Id, exam.Sections.Single(s => s.OrderNo == 1).Id);
    }

    [Fact]
    public async Task TheCandidate_SeesThreeSections_AndTheDragAndDropPartsWithTheirHeadingsAndCards()
    {
        api.SkipIfUnavailable();
        var (client, _, sectionId) = await CandidateAsync();

        var sections = (await client.GetFromJsonAsync<JsonArray>("/api/candidate/sections", ct))!;
        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/paper", ct))!;
        var parts = paper["parts"]!.AsArray();
        var people = parts[1]!["groups"]![0]!;
        var sentences = parts[3]!["groups"]![0]!;

        Assert.Equal(["Reading", "Writing", "Listening"], sections.Select(s => (string)s!["title"]!));
        Assert.Equal(6, parts.Count);
        Assert.Equal(32, parts.SelectMany(p => p!["groups"]!.AsArray()).Sum(g => g!["questions"]!.AsArray().Count));
        Assert.Equal("Young People", (string)people["itemsHeading"]!);
        Assert.Equal("Summer Clubs", (string)people["poolHeading"]!);
        Assert.Equal(8, people["optionPool"]!.AsArray().Count);
        Assert.Equal("Riverside Rowing", (string)people["optionPool"]![0]!["title"]!);
        Assert.NotNull((string?)people["questions"]![0]!["stimulusHtml"]);
        Assert.Contains("{{q:16}}", (string)sentences["bodyHtml"]!);
        Assert.Equal("DragDropMatch", (string)sentences["interactionType"]!);
        Assert.Null((string?)sentences["optionPool"]![0]!["title"]);
    }

    [Fact]
    public async Task CardsAndSentences_AreMarkedByTheirKey()
    {
        api.SkipIfUnavailable();
        var (client, candidateId, sectionId) = await CandidateAsync();
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        // Part 2: four people right, one wrong; Part 4: all five sentences right; Part 6: one typed word.
        var answers = new[] { (6, "A"), (7, "B"), (8, "E"), (9, "D"), (10, "H"), (16, "C"), (17, "F"), (18, "A"), (19, "B"), (20, "E"), (31, "for") }
            .Select(a => new { questionNumber = a.Item1, value = a.Item2 }).ToArray();
        (await client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new { responses = answers }, ct)).EnsureSuccessStatusCode();

        await api.MoveAttemptStartBackAsync(candidateId, sectionId, TimeSpan.FromMinutes(46));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        var attempt = await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Attempts.AsNoTracking()
            .SingleAsync(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId, ct);

        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.Equal(GradingStatus.AutoGraded, attempt.GradingStatus);
        Assert.Equal(10, attempt.RawScore);
        Assert.Equal(32, attempt.MaxScore);
    }
}
