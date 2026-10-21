using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-37: the B1 Preliminary for Schools Listening paper (4 parts, 25 questions, one track per part) imports, keeps
/// "one question per page" on Part 2 through mixing, and reaches the candidate with it.
/// </summary>
public sealed class B1ListeningTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "b1-listen-4q";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private static bool[] PerPage(JsonObject paper) => paper["parts"]!.AsArray().Select(p => (bool)p!["questionPerPage"]!).ToArray();

    [Fact]
    public async Task TheSample_ImportsWithFourTracksAndPart2OneQuestionPerPage()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var id = await api.ImportPublishedSampleAsync(SamplePackages.B1Listening, "B1 L sample");

        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{id}", ct))!;

        var parts = paper["parts"]!.AsArray();
        Assert.Equal(4, parts.Count);
        Assert.Equal(25, parts.Sum(p => p!["groups"]!.AsArray().Sum(g => g!["questions"]!.AsArray().Count)));
        Assert.All(parts, part => Assert.StartsWith("/api/assets/", (string)part!["audioUrl"]!));
        Assert.Equal([false, true, false, false], PerPage(paper));
    }

    [Fact]
    public async Task MixedVersions_KeepOneQuestionPerPage()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var a = await api.ImportPublishedSampleAsync(SamplePackages.B1Listening, "B1 L source A");
        var b = await api.ImportPublishedSampleAsync(SamplePackages.B1Listening, "B1 L source B");

        var mix = await (await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b, seed = 11 }, ct)).Content.ReadFromJsonAsync<JsonObject>(ct);

        foreach (var version in mix!["versions"]!.AsArray())
        {
            var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{(int)version!["paperId"]!}", ct))!;
            Assert.Equal([false, true, false, false], PerPage(paper));
        }
    }

    [Fact]
    public async Task TheCandidate_GetsPart2PagedQuestionByQuestion()
    {
        api.SkipIfUnavailable();
        var paperId = await api.ImportPublishedSampleAsync(SamplePackages.B1Listening, "B1 L delivery");
        int sectionId;
        string username;
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var adminId = db.Users.Where(u => u.Username == ApiFixture.AdminUsername).Select(u => u.Id).Single();
            var template = SeedData.SectionTemplates.Single(t => t.ExamTypeId == SeedData.B1PreliminaryForSchools && t.OrderNo == 3);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var now = DateTime.UtcNow;
            var candidate = new Candidate { CandidateNumber = "0001", FirstName = "Mai", LastName = "Tran", Username = $"tranm{suffix}", PasswordHash = string.Empty, PasswordProtected = "-", CreatedAt = now };
            candidate.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>().HashPassword(candidate, Password);
            // Listening alone, so the test needs no finished Reading and Writing attempts before it.
            var exam = new Exam
            {
                Name = $"B1 Listening {suffix}",
                ExamTypeId = SeedData.B1PreliminaryForSchools,
                ScheduledDate = DateOnly.FromDateTime(now),
                Status = ExamStatus.Active,
                SessionPin = "4821",
                InvigilatorPassword = "invig-pass",
                CreatedByUserId = adminId,
                CreatedAt = now,
                Sections = [new ExamSection { SectionTemplateId = template.Id, TestPaperId = paperId, OrderNo = 1, DurationMinutes = template.DurationMinutes }],
                Candidates = [candidate],
            };
            db.Exams.Add(exam);
            await db.SaveChangesAsync(ct);
            sectionId = exam.Sections.Single().Id;
            username = candidate.Username;
        }

        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new { username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = "4821" }, ct)).EnsureSuccessStatusCode();
        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/paper", ct))!;

        Assert.Equal([false, true, false, false], PerPage(paper));
        var context = paper["parts"]![1]!["groups"]![0]!["questions"]![0]!;
        Assert.Equal("You will hear two friends talking about a new sports centre.<br>What does the boy like about it?", (string)context["promptHtml"]!);
        Assert.Equal(2, (int)paper["parts"]![2]!["groups"]![0]!["maxWordsPerAnswer"]!);
    }
}
