using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    /// <summary>Runs <see cref="During"/> once, the first time the matching save of the correction request is about to run.</summary>
    private sealed class SaveHook(Func<DbContext, bool> matches) : SaveChangesInterceptor
    {
        private int fired;

        public Func<DbContext, Task>? During;

        public bool Fired => fired == 1;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (During is not null && matches(eventData.Context!) && Interlocked.Exchange(ref fired, 1) == 0)
            {
                await During(eventData.Context!);
            }

            return result;
        }
    }

    private async Task<HttpClient> AdminThroughAsync(SaveHook hook)
    {
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(hook))));
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/staff/auth/login", new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword }, ct)).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>
    /// Final review A6: a second candidate's section ends, and the marker marks it, while the correction is marking the
    /// first one again. That attempt was not in the list of attempts to mark again, so it must have been marked with the
    /// corrected key: the key is committed before the re-mark starts.
    /// </summary>
    [Fact]
    public async Task AnAttemptMarkedWhileTheCorrectionIsMarkingAgain_EndsWithTheNewKey()
    {
        api.SkipIfUnavailable();
        var (candidate, paperId, admin) = await MarkedAttemptAsync();
        admin.Dispose();
        var (secondId, _) = await api.AddCandidateAsync(candidate.ExamId, Password);
        var secondAttempt = await api.StartAttemptAsync(secondId, candidate.ReadingWritingSectionId, durationMinutes: 1);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var question2 = await db.Questions.Where(q => q.Number == 2 && q.QuestionGroup!.PaperPart!.TestPaperId == paperId).Select(q => q.Id).SingleAsync(ct);
            db.Responses.Add(new ExamPrep.Domain.Entities.Response { AttemptId = secondAttempt, QuestionId = question2, Value = "A", UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        var hook = new SaveHook(context => context.ChangeTracker.Entries<ExamPrep.Domain.Entities.Response>().Any(e => e.State == EntityState.Modified));
        hook.During = async _ =>
        {
            // The second candidate's time runs out now and the marker marks the attempt, on its own connection.
            await api.SubmitStartedAttemptAsync(secondId, candidate.ReadingWritingSectionId);
            using var scope = api.Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(CancellationToken.None);
        };
        using var client = await AdminThroughAsync(hook);

        var fix = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        Assert.True(hook.Fired, "The correction marked nobody again, so the marker never ran during it.");
        Assert.NotEqual(GradingStatus.NotGraded, (await api.GetAttemptAsync(secondId, candidate.ReadingWritingSectionId)).GradingStatus);
        Assert.Equal((true, 1m), await ResponseOfAsync(secondAttempt, 2));
        var first = await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        Assert.Equal((true, 1m), await ResponseOfAsync(first.Id, 2));

        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var log = await db.AnswerCorrections.AsNoTracking().Where(c => c.TestPaperId == paperId && c.QuestionNumber == 2).SingleAsync(ct);
            Assert.Equal(1, log.AttemptsRemarked);
            Assert.Equal(0, log.AttemptsSkipped);
        }
    }

    /// <summary>Final review 9: two administrators correct the same question at once; the one who loses the race is told to reload.</summary>
    [Fact]
    public async Task TwoCorrectionsOfTheSameQuestionAtOnce_TheLoserGets409NotA500()
    {
        api.SkipIfUnavailable();
        var (_, paperId, admin) = await MarkedAttemptAsync();
        admin.Dispose();
        var hook = new SaveHook(context => context.ChangeTracker.Entries<ExamPrep.Domain.Entities.AnswerKey>().Any(e => e.State == EntityState.Deleted));
        // The other administrator's correction removed these keys a moment ago.
        hook.During = context => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE k FROM answer_keys k JOIN questions q ON q.id = k.question_id JOIN question_groups g ON g.id = q.question_group_id JOIN paper_parts p ON p.id = g.paper_part_id WHERE p.test_paper_id = {paperId} AND q.number = 2");
        using var client = await AdminThroughAsync(hook);

        var fix = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.Conflict, fix.StatusCode);
        Assert.Contains("Someone else corrected this question; reload the paper.", await fix.Content.ReadAsStringAsync(ct));
    }
}
