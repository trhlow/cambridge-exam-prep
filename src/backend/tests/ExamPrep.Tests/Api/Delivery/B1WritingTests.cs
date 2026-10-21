using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-35: B1 Writing Part 2 — the candidate answers one of questions 2 and 3, says which with "Answering this
/// question?", and only that one is marked; the section is worth Part 1 + one Part 2 question.
/// </summary>
public sealed class B1WritingTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "b1-write-5w";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>A B1 session holding only the Writing section (the self-written sample), signed in with the PIN.</summary>
    private async Task<(HttpClient Client, int CandidateId, int ExamId, int SectionId)> CandidateAsync()
    {
        var paperId = await api.ImportPublishedSampleAsync(SamplePackages.B1Writing, "B1 W sample");
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var adminId = db.Users.Where(u => u.Username == ApiFixture.AdminUsername).Select(u => u.Id).Single();
        var template = SeedData.SectionTemplates.Single(t => t.ExamTypeId == SeedData.B1PreliminaryForSchools && t.OrderNo == 2);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;
        var candidate = new Candidate { CandidateNumber = "0001", FirstName = "Hoa", LastName = "Le", Username = $"leh{suffix}", PasswordHash = string.Empty, PasswordProtected = "-", CreatedAt = now };
        candidate.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>().HashPassword(candidate, Password);
        var exam = new Exam
        {
            Name = $"B1 Writing {suffix}",
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

        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new { pin = "4821" }, ct)).EnsureSuccessStatusCode();
        var sectionId = exam.Sections.Single().Id;
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        return (client, candidate.Id, exam.Id, sectionId);
    }

    private Task<HttpResponseMessage> SelectAsync(HttpClient client, int sectionId, int question, string selection) =>
        client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/selections", new { questionNumber = question, selection }, ct);

    private Task<HttpResponseMessage> WriteAsync(HttpClient client, int sectionId, params (int Number, string Text)[] answers) =>
        client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new { responses = answers.Select(a => new { questionNumber = a.Number, value = a.Text }) }, ct);

    /// <summary>Ends the section by time and marks it automatically, as the expiry worker does.</summary>
    private async Task<int> FinishAsync(int candidateId, int sectionId)
    {
        await api.MoveAttemptStartBackAsync(candidateId, sectionId, TimeSpan.FromMinutes(46));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return (await api.GetAttemptAsync(candidateId, sectionId)).Id;
    }

    private async Task<HttpClient> ExaminerAsync(int examId)
    {
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        await api.AssignExaminerAsync(examId, examiner.Id);
        return await api.StaffClientAsync(examiner.Username, Password);
    }

    private Task<HttpResponseMessage> MarkAsync(HttpClient client, int attemptId, int question, decimal score) =>
        client.PostAsJsonAsync($"/api/staff/attempts/{attemptId}/writing-marks", new { questionNumber = question, score }, ct);

    [Fact]
    public async Task TheCandidate_ChoosesOneQuestion_AndCannotChooseASecond()
    {
        api.SkipIfUnavailable();
        var (client, _, _, sectionId) = await CandidateAsync();

        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/paper", ct))!;
        var yes = await SelectAsync(client, sectionId, 2, "Yes");
        var second = await SelectAsync(client, sectionId, 3, "Yes");
        var no = await SelectAsync(client, sectionId, 3, "No");
        var part1 = await SelectAsync(client, sectionId, 1, "Yes");
        var nonsense = await SelectAsync(client, sectionId, 2, "Maybe");
        var state = (await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/attempt", ct))!;

        Assert.Equal(1, (int)paper["parts"]![1]!["groups"]![0]!["chooseCount"]!);
        Assert.Null(paper["parts"]![0]!["groups"]![0]!["chooseCount"]);
        Assert.Equal(HttpStatusCode.NoContent, yes.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, no.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, part1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nonsense.StatusCode);
        Assert.Equal("Yes", (string)state["selections"]!["2"]!);
        Assert.Equal("No", (string)state["selections"]!["3"]!);
    }

    [Fact]
    public async Task OnlyTheChosenQuestion_IsMarked_AndTheSectionIsWorthPart1PlusOneQuestion()
    {
        api.SkipIfUnavailable();
        var (client, candidateId, examId, sectionId) = await CandidateAsync();
        (await WriteAsync(client, sectionId, (1, "Hi Alex, the morning is better because…"), (2, "My favourite place is the beach."), (3, "When Sam opened the old box, he saw…"))).EnsureSuccessStatusCode();
        (await SelectAsync(client, sectionId, 3, "Yes")).EnsureSuccessStatusCode();
        var attemptId = await FinishAsync(candidateId, sectionId);
        using var examiner = await ExaminerAsync(examId);

        var notChosen = await MarkAsync(examiner, attemptId, 2, 10);
        (await MarkAsync(examiner, attemptId, 1, 15)).EnsureSuccessStatusCode();
        var final = await (await MarkAsync(examiner, attemptId, 3, 12)).Content.ReadFromJsonAsync<JsonObject>(ct);

        Assert.Equal(HttpStatusCode.BadRequest, notChosen.StatusCode);
        Assert.Equal("Final", (string)final!["gradingStatus"]!);
        Assert.Equal(27m, (decimal)final["rawScore"]!);
        Assert.Equal(40m, (decimal)final["maxScore"]!);
        var questions = final["questions"]!.AsArray();
        Assert.False((bool)questions.Single(q => (int)q!["number"]! == 2)!["counts"]!);
        Assert.Equal("Yes", (string)questions.Single(q => (int)q!["number"]! == 3)!["selection"]!);
        Assert.Null(questions.Single(q => (int)q!["number"]! == 1)!["selection"]);
    }

    [Fact]
    public async Task ACandidateWhoForgotToChoose_IsMarkedOnTheQuestionTheyWrote()
    {
        api.SkipIfUnavailable();
        var (client, candidateId, examId, sectionId) = await CandidateAsync();
        (await WriteAsync(client, sectionId, (1, "Hi Alex!"), (3, "When Sam opened the old box…"))).EnsureSuccessStatusCode();
        var attemptId = await FinishAsync(candidateId, sectionId);
        using var examiner = await ExaminerAsync(examId);

        var marking = (await examiner.GetFromJsonAsync<JsonObject>($"/api/staff/attempts/{attemptId}/marking", ct))!;

        var questions = marking["questions"]!.AsArray();
        Assert.False((bool)questions.Single(q => (int)q!["number"]! == 2)!["counts"]!);
        Assert.True((bool)questions.Single(q => (int)q!["number"]! == 3)!["counts"]!);
        Assert.Equal("Undecided", (string)questions.Single(q => (int)q!["number"]! == 3)!["selection"]!);
    }
}
