using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// A6 repair (review 02/11): an administrator corrects the answer key of a published paper, and every attempt already
/// marked on that paper is marked again for that question, so scores follow the corrected key.
/// </summary>
public sealed class PaperAnswerCorrectionTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record ResponseItem(int QuestionNumber, string Value);

    private sealed record SaveRequest(List<ResponseItem> Responses);

    /// <summary>A candidate who has finished Reading &amp; Writing: question 1 right (A), question 2 wrong (A, the key is B).</summary>
    private async Task<(ApiFixture.SeededCandidate Candidate, int PaperId, HttpClient Admin)> MarkedAttemptAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var admin = await api.AdminClientAsync();
        var paperId = await PaperOfSectionAsync(candidate.ReadingWritingSectionId);
        (await admin.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct)).EnsureSuccessStatusCode();

        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        var sectionId = candidate.ReadingWritingSectionId;
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new SaveRequest([new(1, "A"), new(2, "A"), new(25, "for")]), ct)).EnsureSuccessStatusCode();
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, sectionId, TimeSpan.FromMinutes(61));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return (candidate, paperId, admin);
    }

    private async Task<int> PaperOfSectionAsync(int sectionId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.ExamSections.Where(s => s.Id == sectionId).Select(s => s.TestPaperId).SingleAsync();
    }

    private async Task<(bool? IsCorrect, decimal? AutoScore)> ResponseOfAsync(int attemptId, int number)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var response = await db.Responses.AsNoTracking().SingleAsync(r => r.AttemptId == attemptId && r.Question!.Number == number);
        return (response.IsCorrect, response.AutoScore);
    }

    [Fact]
    public async Task AdminCorrectsAnAnswer_AndSubmittedAttemptsAreMarkedAgainForThatQuestion()
    {
        api.SkipIfUnavailable();
        var (candidate, paperId, admin) = await MarkedAttemptAsync();
        using var _ = admin;
        var before = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        var fix = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/2/answers", new { answers = new[] { "A" } }, ct);
        var after = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        var body = await fix.Content.ReadFromJsonAsync<JsonObject>(ct);

        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        Assert.Equal(1, (int)body!["attemptsRemarked"]!);
        Assert.Equal(["A"], body["answers"]!.AsArray().Select(a => (string)a!));
        Assert.Equal(2m, before.RawScore);
        Assert.Equal(before.RawScore + 1, after.RawScore);
        Assert.Equal((true, 1m), await ResponseOfAsync(after.Id, 2));
        var paper = await admin.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{paperId}", ct);
        var question = paper!["parts"]!.AsArray().SelectMany(p => p!["groups"]!.AsArray()).SelectMany(g => g!["questions"]!.AsArray()).Single(q => (int)q!["number"]! == 2);
        Assert.Equal(["A"], question["answers"]!.AsArray().Select(a => (string)a!));

        // Back again: the score returns, and the question that was right stays right.
        (await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/2/answers", new { answers = new[] { "B" } }, ct)).EnsureSuccessStatusCode();
        var restored = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        Assert.Equal(before.RawScore, restored.RawScore);
        Assert.Equal((false, 0m), await ResponseOfAsync(after.Id, 2));
        Assert.Equal((true, 1m), await ResponseOfAsync(after.Id, 1));
    }

    [Fact]
    public async Task TypedAnswers_CanGainAnAlternative()
    {
        api.SkipIfUnavailable();
        var (candidate, paperId, admin) = await MarkedAttemptAsync();
        using var _ = admin;
        var before = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        // The candidate typed "for"; the key of question 25 is "for" already, so first take it away, then add it back with another word.
        (await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/25/answers", new { answers = new[] { "from" } }, ct)).EnsureSuccessStatusCode();
        var wrong = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        (await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/25/answers", new { answers = new[] { "from", "FOR" } }, ct)).EnsureSuccessStatusCode();
        var right = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        Assert.Equal(before.RawScore - 1, wrong.RawScore);
        Assert.Equal(before.RawScore, right.RawScore);
    }

    [Fact]
    public async Task ABadKey_OtherStaff_DraftsAndWriting_AreRefused()
    {
        api.SkipIfUnavailable();
        var (candidate, paperId, admin) = await MarkedAttemptAsync();
        using var _ = admin;
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var papers = await api.StaffClientAsync(manager.Username, Password);
        var draft = await ImportDraftAsync(admin);

        var unknown = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1/answers", new { answers = new[] { "Z" } }, ct);
        var two = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1/answers", new { answers = new[] { "A", "B" } }, ct);
        var writing = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/31/answers", new { answers = new[] { "x" } }, ct);
        var missing = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/999/answers", new { answers = new[] { "A" } }, ct);
        var notAdmin = await papers.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1/answers", new { answers = new[] { "B" } }, ct);
        var onDraft = await admin.PutAsJsonAsync($"/api/admin/papers/{draft}/questions/1/answers", new { answers = new[] { "B" } }, ct);
        var attempt = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        Assert.All([unknown, two, writing], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, onDraft.StatusCode);
        Assert.Equal(1m, (await ResponseOfAsync(attempt.Id, 1)).AutoScore);
    }

    private async Task<int> ImportDraftAsync(HttpClient admin)
    {
        var file = new ByteArrayContent(ExamPrep.Tests.Infrastructure.Content.SamplePackages.Zip(ExamPrep.Tests.Infrastructure.Content.SamplePackages.ReadingWriting));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        var import = await admin.PostAsync("/api/admin/papers/import", new MultipartFormDataContent { { file, "package", "paper.zip" } }, ct);
        import.EnsureSuccessStatusCode();
        return (int)(await import.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
    }
}
