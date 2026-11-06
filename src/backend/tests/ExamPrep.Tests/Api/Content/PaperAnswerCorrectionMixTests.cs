using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Papers;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// Review of Task 6: a corrected answer key must not leave copies of a question disagreeing. A correction on a source
/// paper reaches the mix versions that took the question's part from it, each with an audit row; a mix version refuses
/// it and names the source. The re-mark keeps Writing marks and survives a lost race.
/// </summary>
public sealed class PaperAnswerCorrectionMixTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>Changes the attempt's concurrency stamp behind the request's back, a set number of times.</summary>
    private sealed class ConflictMaker : SaveChangesInterceptor
    {
        public int Remaining;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context!;
            var attempt = context.ChangeTracker.Entries<Attempt>().FirstOrDefault(e => e.State == EntityState.Modified);
            if (attempt is not null && Remaining > 0)
            {
                Remaining--;
                var id = attempt.Entity.Id;
                await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE attempts SET concurrency_stamp = UUID() WHERE id = {id}", cancellationToken);
            }

            return result;
        }
    }

    private sealed record Setup(int ExamId, int SectionId, int A, int B, int MixId, List<(int PaperId, int Version, bool FromA)> Versions, string TitleA);

    /// <summary>Two published sources (question 2 is B in both), a mix of four versions and which of them took question 2's part from A.</summary>
    private async Task<Setup> MixedAsync(HttpClient admin)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var titleA = $"Source A {suffix}";
        var a = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, titleA);
        var b = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"Source B {suffix}");
        var response = await admin.PostAsJsonAsync("/api/admin/paper-mixes", new { sourcePaperIds = new[] { a, b }, versionCount = 4, seed = 11 }, ct);
        response.EnsureSuccessStatusCode();
        var mixId = (int)(await response.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
        var candidate = await api.CreateCandidateAsync(Password);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var versions = new List<(int, int, bool)>();
        foreach (var paper in await db.TestPapers.AsNoTracking().Include(p => p.Parts).Where(p => p.PaperMixId == mixId).OrderBy(p => p.MixVersion).ToListAsync(ct))
        {
            var ordered = paper.Parts.OrderBy(p => p.PartNo).ToList();
            var partOfTwo = await db.Questions.Where(q => q.Number == 2 && q.QuestionGroup!.PaperPart!.TestPaperId == paper.Id).Select(q => q.QuestionGroup!.PaperPart!.PartNo).SingleAsync(ct);
            var ordinal = ordered.FindIndex(p => p.PartNo == partOfTwo) + 1;
            versions.Add((paper.Id, paper.MixVersion!.Value, PaperMixPlan.SourceOf(paper.MixPattern!, ordinal) == 0));
        }

        return new Setup(candidate.ExamId, candidate.ReadingWritingSectionId, a, b, mixId, versions, titleA);
    }

    /// <summary>A candidate who submitted Reading &amp; Writing on the paper: question 2 answered A (the key is B), then marked.</summary>
    private async Task<int> MarkedAttemptAsync(Setup setup, int paperId)
    {
        var (candidateId, _) = await api.AddCandidateAsync(setup.ExamId, Password);
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var now = DateTime.UtcNow;
        var question = await db.Questions.Where(q => q.Number == 2 && q.QuestionGroup!.PaperPart!.TestPaperId == paperId).Select(q => q.Id).SingleAsync(ct);
        db.Attempts.Add(new Attempt
        {
            CandidateId = candidateId,
            ExamSectionId = setup.SectionId,
            TestPaperId = paperId,
            Status = AttemptStatus.Submitted,
            StartedAt = now.AddMinutes(-70),
            EndsAt = now.AddMinutes(-10),
            SubmittedAt = now.AddMinutes(-10),
            SubmitReason = SubmitReason.TimeExpired,
            Responses = [new Response { QuestionId = question, Value = "A", UpdatedAt = now }],
        });
        await db.SaveChangesAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return await db.Attempts.Where(a => a.CandidateId == candidateId).Select(a => a.Id).SingleAsync(ct);
    }

    private async Task<Attempt> AttemptAsync(int id)
    {
        using var scope = api.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().Attempts.AsNoTracking().SingleAsync(a => a.Id == id, ct);
    }

    private async Task<List<string>> KeyAsync(int paperId, int number)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.AnswerKeys.Where(k => k.Question!.Number == number && k.Question.QuestionGroup!.PaperPart!.TestPaperId == paperId).Select(k => k.Value).ToListAsync(ct);
    }

    [Fact]
    public async Task ACorrectionOnASource_ReachesTheVersionsThatTookTheQuestionFromIt_EachWithAnAuditRow()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var setup = await MixedAsync(admin);
        Assert.Contains(setup.Versions, v => v.FromA);
        Assert.Contains(setup.Versions, v => !v.FromA);
        var attempts = new Dictionary<int, int> { [setup.A] = await MarkedAttemptAsync(setup, setup.A), [setup.B] = await MarkedAttemptAsync(setup, setup.B) };
        foreach (var v in setup.Versions)
        {
            attempts[v.PaperId] = await MarkedAttemptAsync(setup, v.PaperId);
        }

        var fix = await admin.PutAsJsonAsync($"/api/admin/papers/{setup.A}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        var fromA = setup.Versions.Where(v => v.FromA).Select(v => v.PaperId).ToList();
        foreach (var paperId in fromA.Append(setup.A))
        {
            Assert.Equal(["A"], await KeyAsync(paperId, 2));
            Assert.Equal(1m, (await AttemptAsync(attempts[paperId])).RawScore);
        }

        foreach (var paperId in setup.Versions.Where(v => !v.FromA).Select(v => v.PaperId).Append(setup.B))
        {
            Assert.Equal(["B"], await KeyAsync(paperId, 2));
            Assert.Equal(0m, (await AttemptAsync(attempts[paperId])).RawScore);
        }

        var body = await fix.Content.ReadFromJsonAsync<JsonObject>(ct);
        Assert.Equal(fromA.Count + 1, (int)body!["attemptsRemarked"]!);
        using var scope = api.Factory.Services.CreateScope();
        var rows = await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().AnswerCorrections.AsNoTracking()
            .Where(c => c.QuestionNumber == 2 && (c.TestPaperId == setup.A || fromA.Contains(c.TestPaperId))).ToListAsync(ct);
        Assert.Equal(fromA.Count + 1, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal("B", row.OldAnswers);
            Assert.Equal("A", row.NewAnswers);
            Assert.NotNull(row.CorrectedByUserId);
            Assert.Equal(1, row.AttemptsRemarked);
            Assert.Equal(0, row.AttemptsSkipped);
        });
    }

    [Fact]
    public async Task ACorrectionOnAMixVersion_IsRefused_AndNamesTheSource()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var setup = await MixedAsync(admin);
        var version = setup.Versions.First(v => v.FromA);

        var response = await admin.PutAsJsonAsync($"/api/admin/papers/{version.PaperId}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var title = (string)(await response.Content.ReadFromJsonAsync<JsonObject>(ct))!["title"]!;
        Assert.Equal($"This paper is a mixed version; correct the answer on its source paper {setup.TitleA}.", title);
        Assert.Equal(["B"], await KeyAsync(version.PaperId, 2));
    }

    [Fact]
    public async Task ACorrectionOnAnOrdinaryPaper_WritesOneAuditRow()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var setup = await MixedAsync(admin);
        var paper = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"Alone {Guid.NewGuid().ToString("N")[..6]}");
        await MarkedAttemptAsync(setup, paper);

        (await admin.PutAsJsonAsync($"/api/admin/papers/{paper}/questions/2/answers", new { answers = new[] { "A" } }, ct)).EnsureSuccessStatusCode();

        using var scope = api.Factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().AnswerCorrections.AsNoTracking().SingleAsync(c => c.TestPaperId == paper, ct);
        Assert.Equal((2, "B", "A", 1, 0), (row.QuestionNumber, row.OldAnswers, row.NewAnswers, row.AttemptsRemarked, row.AttemptsSkipped));
    }

    [Fact]
    public async Task AFinalAttemptWithWritingMarks_KeepsItsStatus_AndTheLatestMarkCounts()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var setup = await MixedAsync(admin);
        var attemptId = await MarkedAttemptAsync(setup, setup.A);
        using (var scope = api.Factory.Services.CreateScope())
        {
            // Two marks on Writing question 31: the later one (4) is the mark that counts; the attempt is Final.
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var marker = await db.Users.Select(u => u.Id).FirstAsync(ct);
            var essay = await db.Questions.Where(q => q.Number == 31 && q.QuestionGroup!.PaperPart!.TestPaperId == setup.A).Select(q => q.Id).SingleAsync(ct);
            var response = new Response { AttemptId = attemptId, QuestionId = essay, Value = "My letter", UpdatedAt = DateTime.UtcNow };
            db.Responses.Add(response);
            await db.SaveChangesAsync(ct);
            db.ManualMarks.AddRange(
                new ManualMark { ResponseId = response.Id, MarkerUserId = marker, Score = 2, MaxScore = 5, MarkedAt = DateTime.UtcNow.AddMinutes(-5) },
                new ManualMark { ResponseId = response.Id, MarkerUserId = marker, Score = 4, MaxScore = 5, MarkedAt = DateTime.UtcNow });
            var attempt = await db.Attempts.SingleAsync(a => a.Id == attemptId, ct);
            attempt.RawScore = 4;
            attempt.GradingStatus = GradingStatus.Final;
            await db.SaveChangesAsync(ct);
        }

        (await admin.PutAsJsonAsync($"/api/admin/papers/{setup.A}/questions/2/answers", new { answers = new[] { "A" } }, ct)).EnsureSuccessStatusCode();

        var after = await AttemptAsync(attemptId);
        Assert.Equal(5m, after.RawScore);
        Assert.Equal(GradingStatus.Final, after.GradingStatus);
    }

    [Fact]
    public async Task AnAttemptStillNeedingManualMarking_IsRemarkedAndStaysSo()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var setup = await MixedAsync(admin);
        var attemptId = await MarkedAttemptAsync(setup, setup.A);
        Assert.Equal(GradingStatus.NeedsManualMarking, (await AttemptAsync(attemptId)).GradingStatus);

        (await admin.PutAsJsonAsync($"/api/admin/papers/{setup.A}/questions/2/answers", new { answers = new[] { "A" } }, ct)).EnsureSuccessStatusCode();

        var after = await AttemptAsync(attemptId);
        Assert.Equal(1m, after.RawScore);
        Assert.Equal(GradingStatus.NeedsManualMarking, after.GradingStatus);
    }

    private async Task<(HttpClient Admin, ConflictMaker Conflicts)> ClientWithConflictsAsync()
    {
        var conflicts = new ConflictMaker();
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(conflicts))));
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/staff/auth/login", new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword }, ct);
        login.EnsureSuccessStatusCode();
        return (client, conflicts);
    }

    [Fact]
    public async Task ALostRaceDuringTheRemark_IsReadAgainAndTheAttemptStillMarked()
    {
        api.SkipIfUnavailable();
        using var plain = await api.AdminClientAsync();
        var setup = await MixedAsync(plain);
        var attemptId = await MarkedAttemptAsync(setup, setup.A);
        var (admin, conflicts) = await ClientWithConflictsAsync();
        using var _ = admin;
        conflicts.Remaining = 2;

        var fix = await admin.PutAsJsonAsync($"/api/admin/papers/{setup.A}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        Assert.Empty((await fix.Content.ReadFromJsonAsync<JsonObject>(ct))!["skipped"]!.AsArray());
        Assert.Equal(1m, (await AttemptAsync(attemptId)).RawScore);
    }

    [Fact]
    public async Task AnAttemptThatKeepsChanging_IsReportedSkipped_AndCountedInTheAuditRow()
    {
        api.SkipIfUnavailable();
        using var plain = await api.AdminClientAsync();
        var setup = await MixedAsync(plain);
        var attemptId = await MarkedAttemptAsync(setup, setup.A);
        var (admin, conflicts) = await ClientWithConflictsAsync();
        using var _ = admin;
        conflicts.Remaining = 50;

        var fix = await admin.PutAsJsonAsync($"/api/admin/papers/{setup.A}/questions/2/answers", new { answers = new[] { "A" } }, ct);

        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        Assert.Equal([attemptId], (await fix.Content.ReadFromJsonAsync<JsonObject>(ct))!["skipped"]!.AsArray().Select(x => (int)x!).ToList());
        Assert.Equal(0m, (await AttemptAsync(attemptId)).RawScore);
        using var scope = api.Factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().AnswerCorrections.AsNoTracking().SingleAsync(c => c.TestPaperId == setup.A, ct);
        Assert.Equal(1, row.AttemptsSkipped);
    }
}
