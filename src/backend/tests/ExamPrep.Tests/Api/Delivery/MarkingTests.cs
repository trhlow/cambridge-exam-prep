using System.Net;
using System.Net.Http.Json;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-11: once a section has ended, Reading Parts 1–5 and Listening are marked from the answer key; Writing waits
/// for a teacher. Staff see the scores; candidates do not, as in Cambridge exams.
/// </summary>
public sealed class MarkingTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record ResponseItem(int QuestionNumber, string Value);

    private sealed record SaveRequest(List<ResponseItem> Responses);

    private sealed record SectionResult(int SectionId, string Title, string Status, string GradingStatus, decimal? RawScore, decimal? MaxScore, int? AttemptId);

    private sealed record CandidateResult(int CandidateId, string CandidateNumber, string FullName, List<SectionResult> Sections);

    private sealed record QuestionMark(int Number, int PartNo, string InteractionType, string? Response, List<string> AcceptedAnswers, bool? IsCorrect, decimal? Score, decimal Points);

    private sealed record AttemptMarking(int AttemptId, string SectionTitle, string GradingStatus, decimal? RawScore, decimal? MaxScore, List<QuestionMark> Questions);

    private async Task<(HttpClient Client, ApiFixture.SeededCandidate Candidate)> CandidateAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        return (client, candidate);
    }

    /// <summary>Starts the section, saves the answers, lets the time run out and runs the background marking.</summary>
    private async Task TakeSectionAsync(HttpClient client, ApiFixture.SeededCandidate candidate, int sectionId, params ResponseItem[] answers)
    {
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new SaveRequest([.. answers]), ct)).EnsureSuccessStatusCode();
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, sectionId, TimeSpan.FromMinutes(61));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
    }

    private async Task<HttpClient> StaffClientAsync()
    {
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(ApiFixture.AdminUsername, ApiFixture.AdminPassword), ct)).EnsureSuccessStatusCode();
        return client;
    }

    private static readonly ResponseItem[] ReadingWritingAnswers =
    [
        new(1, "A"),             // correct
        new(2, "A"),             // wrong: the key is B
        new(19, "A"),            // correct word in the gap ("make")
        new(25, "FOR"),          // correct, case ignored
        new(27, "’ll"),          // correct alternative with a curly apostrophe
        new(31, "Hi Jo, let's see the new film on Saturday. We can take the bus."),
    ];

    [Fact]
    public async Task ReadingAndWriting_IsAutoMarked_AndWaitsForATeacherForWriting()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;

        await TakeSectionAsync(client, candidate, candidate.ReadingWritingSectionId, ReadingWritingAnswers);

        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        Assert.Equal(4m, attempt.RawScore);
        Assert.Equal(60m, attempt.MaxScore);
        Assert.Equal(GradingStatus.NeedsManualMarking, attempt.GradingStatus);
    }

    [Fact]
    public async Task Listening_IsFullyAutoMarked_IncludingDragAndDrop()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        await api.SubmitSectionAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        await TakeSectionAsync(client, candidate, candidate.ListeningSectionId, new(1, "B"), new(7, "14th"), new(21, "A"), new(22, "B"));

        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ListeningSectionId);
        Assert.Equal(3m, attempt.RawScore);
        Assert.Equal(25m, attempt.MaxScore);
        Assert.Equal(GradingStatus.AutoGraded, attempt.GradingStatus);
    }

    [Fact]
    public async Task SectionsStillRunning_AreNotMarked()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        (await client.PostAsync($"/api/candidate/sections/{candidate.ReadingWritingSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        }

        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        Assert.Null(attempt.RawScore);
        Assert.Equal(GradingStatus.NotGraded, attempt.GradingStatus);
    }

    [Fact]
    public async Task Results_ShowEachCandidatesScorePerSection_ToStaff()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        await TakeSectionAsync(client, candidate, candidate.ReadingWritingSectionId, ReadingWritingAnswers);
        using var staff = await StaffClientAsync();

        var results = await staff.GetFromJsonAsync<List<CandidateResult>>($"/api/staff/exams/{candidate.ExamId}/results", ct);

        var row = Assert.Single(results!);
        Assert.Equal("An Nguyen", row.FullName);
        var readingWriting = row.Sections.Single(s => s.Title == "Reading & Writing");
        Assert.Equal("Submitted", readingWriting.Status);
        Assert.Equal("NeedsManualMarking", readingWriting.GradingStatus);
        Assert.Equal(4m, readingWriting.RawScore);
        Assert.Equal("NotStarted", row.Sections.Single(s => s.Title == "Listening").Status);
    }

    [Fact]
    public async Task Marking_ShowsEveryQuestionWithTheCandidatesAnswerAndTheKey()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        await TakeSectionAsync(client, candidate, candidate.ReadingWritingSectionId, ReadingWritingAnswers);
        var attemptId = (await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId)).Id;
        using var staff = await StaffClientAsync();

        var marking = (await staff.GetFromJsonAsync<AttemptMarking>($"/api/staff/attempts/{attemptId}/marking", ct))!;

        Assert.Equal(32, marking.Questions.Count);
        var q2 = marking.Questions.Single(q => q.Number == 2);
        Assert.Equal("A", q2.Response);
        Assert.Equal(["B"], q2.AcceptedAnswers);
        Assert.False(q2.IsCorrect);
        Assert.Equal(0m, q2.Score);
        var q3 = marking.Questions.Single(q => q.Number == 3);
        Assert.Null(q3.Response);
        Assert.False(q3.IsCorrect);
        var essay = marking.Questions.Single(q => q.Number == 31);
        Assert.Null(essay.IsCorrect);
        Assert.StartsWith("Hi Jo", essay.Response);
    }

    [Fact]
    public async Task Results_AreNotVisibleToCandidates()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;

        var response = await client.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Marking_WithTheAnswerKey_IsNotAvailableToInvigilators_AndResultsOnlyForTheirSessions()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        (await client.PostAsync($"/api/candidate/sections/{candidate.ReadingWritingSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        var attemptId = (await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId)).Id;
        const string password = "Invigilator#2026";
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, password);
        using var staff = api.CreateClient();
        (await staff.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(invigilator.Username, password), ct)).EnsureSuccessStatusCode();

        var marking = await staff.GetAsync($"/api/staff/attempts/{attemptId}/marking", ct);
        var resultsBeforeAssignment = await staff.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{candidate.ExamId}/invigilators", new { userIds = new[] { invigilator.Id } }, ct)).EnsureSuccessStatusCode();
        var results = await staff.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct);

        Assert.Equal(HttpStatusCode.Forbidden, marking.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, resultsBeforeAssignment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
    }

    [Fact]
    public async Task Marking_Returns404_ForAnUnknownAttempt()
    {
        api.SkipIfUnavailable();
        using var staff = await StaffClientAsync();

        var response = await staff.GetAsync("/api/staff/attempts/999999/marking", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
