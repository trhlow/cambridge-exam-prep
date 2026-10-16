using System.Net;
using System.Net.Http.Json;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-26: a teacher marks the Writing questions of a finished Reading &amp; Writing section; once every Writing
/// question has a mark, the section score is the automatic marks plus the Writing marks and the result is final.
/// </summary>
public sealed class WritingMarkingTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record ResponseItem(int QuestionNumber, string Value);

    private sealed record QuestionMark(int Number, int PartNo, string InteractionType, string? Response, List<string> AcceptedAnswers, bool? IsCorrect, decimal? Score, decimal Points, string? Comment);

    private sealed record AttemptMarking(int AttemptId, string SectionTitle, string GradingStatus, decimal? RawScore, decimal? MaxScore, List<QuestionMark> Questions);

    /// <summary>A candidate who answered question 1 correctly and wrote question 31 (32 left empty), then ran out of time.</summary>
    private async Task<(int AttemptId, ApiFixture.SeededCandidate Candidate)> FinishedReadingWritingAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        var section = candidate.ReadingWritingSectionId;
        (await client.PostAsync($"/api/candidate/sections/{section}/attempt", null, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/candidate/sections/{section}/responses", new { responses = new[] { new ResponseItem(1, "A"), new ResponseItem(31, "Hi Jo, let's meet on Saturday at the cinema.") } }, ct)).EnsureSuccessStatusCode();
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, section, TimeSpan.FromMinutes(61));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return ((await api.GetAttemptAsync(candidate.CandidateId, section)).Id, candidate);
    }

    private Task<HttpResponseMessage> MarkAsync(HttpClient client, int attemptId, int question, decimal score, string? comment = null) =>
        client.PostAsJsonAsync($"/api/staff/attempts/{attemptId}/writing-marks", new { questionNumber = question, score, comment }, ct);

    /// <summary>An examiner assigned to the session: the only staff who mark Writing.</summary>
    private async Task<HttpClient> ExaminerClientAsync(int examId)
    {
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        await api.AssignExaminerAsync(examId, examiner.Id);
        return await api.StaffClientAsync(examiner.Username, Password);
    }

    [Fact]
    public async Task MarkingEveryWritingQuestion_MakesTheResultFinal()
    {
        api.SkipIfUnavailable();
        var (attemptId, candidate) = await FinishedReadingWritingAsync();
        using var examiner = await ExaminerClientAsync(candidate.ExamId);

        var first = await MarkAsync(examiner, attemptId, 31, 12, "Clear, all three points.");
        var afterFirst = await first.Content.ReadFromJsonAsync<AttemptMarking>(ct);
        var second = await MarkAsync(examiner, attemptId, 32, 0, "Not attempted.");
        var final = await second.Content.ReadFromJsonAsync<AttemptMarking>(ct);
        var remark = await MarkAsync(examiner, attemptId, 31, 13);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("NeedsManualMarking", afterFirst!.GradingStatus);
        Assert.Equal(12m, afterFirst.Questions.Single(q => q.Number == 31).Score);
        Assert.Equal("Clear, all three points.", afterFirst.Questions.Single(q => q.Number == 31).Comment);
        Assert.Equal("Final", final!.GradingStatus);
        Assert.Equal(13m, final.RawScore); // 1 automatic mark + 12 + 0
        Assert.Equal(14m, (await remark.Content.ReadFromJsonAsync<AttemptMarking>(ct))!.RawScore);
        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        Assert.Equal(GradingStatus.Final, attempt.GradingStatus);
    }

    [Fact]
    public async Task WritingMarks_AreCheckedAndForAssignedExaminersOnly()
    {
        api.SkipIfUnavailable();
        var (attemptId, candidate) = await FinishedReadingWritingAsync();
        using var examiner = await ExaminerClientAsync(candidate.ExamId);
        using var administrator = await api.AdminClientAsync();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var invigilatorClient = await api.StaffClientAsync(invigilator.Username, Password);

        var tooHigh = await MarkAsync(examiner, attemptId, 31, 16);
        var notWriting = await MarkAsync(examiner, attemptId, 1, 1);
        var negative = await MarkAsync(examiner, attemptId, 31, -1);
        var byInvigilator = await MarkAsync(invigilatorClient, attemptId, 31, 10);
        var byAdministrator = await MarkAsync(administrator, attemptId, 31, 10);

        Assert.Equal(HttpStatusCode.BadRequest, tooHigh.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, notWriting.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byInvigilator.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byAdministrator.StatusCode);
    }

    [Fact]
    public async Task WritingMarks_WaitUntilTheSectionHasEnded()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var attemptId = await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        using var examiner = await ExaminerClientAsync(candidate.ExamId);

        var response = await MarkAsync(examiner, attemptId, 31, 10);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
