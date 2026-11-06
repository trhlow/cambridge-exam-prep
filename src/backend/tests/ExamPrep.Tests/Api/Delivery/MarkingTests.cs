using System.Net;
using System.Net.Http.Json;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>An examiner assigned to the session: the only staff who see the marking of an attempt.</summary>
    private async Task<HttpClient> ExaminerClientAsync(int examId)
    {
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        await api.AssignExaminerAsync(examId, examiner.Id);
        return await api.StaffClientAsync(examiner.Username, Password);
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
    public async Task Marking_ShowsOnlyTheWriting_WithTheCandidatesAnswer_AndNoOtherPart()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateAsync();
        using var _ = client;
        await TakeSectionAsync(client, candidate, candidate.ReadingWritingSectionId, ReadingWritingAnswers);
        var attemptId = (await api.GetAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId)).Id;
        using var staff = await ExaminerClientAsync(candidate.ExamId);

        var marking = (await staff.GetFromJsonAsync<AttemptMarking>($"/api/staff/attempts/{attemptId}/marking", ct))!;

        // VE-73: the examiner marks Writing only (A2 Reading & Writing questions 31 and 32), so Reading is left out.
        Assert.Equal([31, 32], marking.Questions.Select(q => q.Number));
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
    public async Task Invigilators_SeeNeitherTheMarkingNorTheResults_EvenForTheirOwnSessions()
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
        Assert.Equal(HttpStatusCode.Forbidden, resultsBeforeAssignment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Marking_Returns404_ForAnUnknownAttempt()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var staff = await ExaminerClientAsync(candidate.ExamId);

        var response = await staff.GetAsync("/api/staff/attempts/999999/marking", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Counts the commands that read the questions table, to show how often the marker loads a paper.</summary>
    private sealed class QuestionQueryCounter : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override System.Data.Common.DbDataReader ReaderExecuted(System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData, System.Data.Common.DbDataReader result)
        {
            Count += command.CommandText.Contains("FROM `questions`", StringComparison.Ordinal) ? 1 : 0;
            return result;
        }

        public override ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData, System.Data.Common.DbDataReader result, CancellationToken cancellationToken = default)
        {
            Count += command.CommandText.Contains("FROM `questions`", StringComparison.Ordinal) ? 1 : 0;
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// When a section ends for the whole room, hundreds of attempts of one paper wait for marking at once; the marker
    /// used to load the paper's questions and answer keys again for every attempt (about 400 queries at 200 candidates).
    /// </summary>
    [Fact]
    public async Task MarkPending_LoadsTheQuestionsOncePerPaper_NotOncePerAttempt()
    {
        api.SkipIfUnavailable();
        var first = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var ids = new List<int> { first.CandidateId };
        for (var i = 0; i < 3; i++)
        {
            ids.Add((await api.AddCandidateAsync(first.ExamId, Password, "Mai", $"Zz{Guid.NewGuid():N}"[..10])).CandidateId);
        }

        foreach (var id in ids)
        {
            await api.StartAttemptAsync(id, first.ReadingWritingSectionId);
            await api.SubmitStartedAttemptAsync(id, first.ReadingWritingSectionId);
        }

        using var scope = api.Factory.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Database.GetConnectionString()!;
        var counter = new QuestionQueryCounter();
        await using var db = new ExamPrepDbContext(new DbContextOptionsBuilder<ExamPrepDbContext>().UseMySQL(connection).AddInterceptors(counter).Options);
        var papers = await db.Attempts
            .Where(a => a.Status == AttemptStatus.Submitted && a.GradingStatus == GradingStatus.NotGraded)
            .Select(a => a.TestPaperId).Distinct().CountAsync(ct);

        var marked = await new AttemptMarker(db).MarkPendingAsync(ct);

        Assert.True(marked >= 1);
        Assert.True(counter.Count <= papers, $"{counter.Count} question queries for {papers} papers and {marked} attempts");
    }

    /// <summary>
    /// The background sweep and a caller can mark at the same time. When one attempt of a batch was changed meanwhile,
    /// its conflict used to throw the whole batch away (and report 0), so the others stayed ungraded until the next
    /// tick. This was the cause of the occasional failure of AnonymousMarkingTests, which marks beside the live sweep.
    /// </summary>
    [Fact]
    public async Task MarkPending_WhenOneAttemptWasChangedMeanwhile_StillMarksTheOthers()
    {
        api.SkipIfUnavailable();
        var first = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var ids = new List<int> { first.CandidateId };
        for (var i = 0; i < 2; i++)
        {
            ids.Add((await api.AddCandidateAsync(first.ExamId, Password, "Mai", $"Zz{Guid.NewGuid():N}"[..10])).CandidateId);
        }

        foreach (var id in ids)
        {
            await api.StartAttemptAsync(id, first.ReadingWritingSectionId);
            await api.SubmitStartedAttemptAsync(id, first.ReadingWritingSectionId);
        }

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var bumped = false;
        db.SavingChanges += (_, _) =>
        {
            // Someone else saves one of the attempts between this marker's read and its save.
            if (bumped || db.ChangeTracker.Entries<ExamPrep.Domain.Entities.Attempt>().FirstOrDefault(e => e.State == EntityState.Modified) is not { } entry)
            {
                return;
            }

            bumped = true;
            db.Database.ExecuteSqlInterpolated($"UPDATE attempts SET concurrency_stamp = UUID() WHERE id = {entry.Entity.Id}");
        };

        await new AttemptMarker(db).MarkPendingAsync(ct);

        using var check = api.Factory.Services.CreateScope();
        var graded = await check.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Attempts
            .Where(a => ids.Contains(a.CandidateId) && a.ExamSectionId == first.ReadingWritingSectionId)
            .Select(a => a.GradingStatus).ToListAsync(ct);
        Assert.All(graded, g => Assert.NotEqual(GradingStatus.NotGraded, g));
    }
}
