using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Results;

/// <summary>
/// VE-73 (Victory 29/10): the Writing scripts of a session are dealt at random and evenly to its examiners; an examiner
/// sees only the scripts dealt to them, by code, never the candidate's name nor the other parts of the test; scores with
/// names are for administrators.
/// </summary>
public sealed class AnonymousMarkingTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Marker#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    /// <summary>A session of <paramref name="count"/> candidates who all finished Reading &amp; Writing (marked automatically).</summary>
    private async Task<(ApiFixture.SeededCandidate Exam, List<(int Id, string LastName)> Candidates)> FinishedSessionAsync(int count)
    {
        var exam = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var candidates = new List<(int Id, string LastName)> { (exam.CandidateId, "Nguyen") };
        for (var i = 1; i < count; i++)
        {
            var lastName = $"Zz{Guid.NewGuid():N}"[..10];
            candidates.Add(((await api.AddCandidateAsync(exam.ExamId, Password, "Mai", lastName)).CandidateId, lastName));
        }

        foreach (var (id, _) in candidates)
        {
            await api.StartAttemptAsync(id, exam.ReadingWritingSectionId);
            await api.SubmitStartedAttemptAsync(id, exam.ReadingWritingSectionId);
        }

        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return (exam, candidates);
    }

    /// <summary>
    /// Examiners put on the session together, as the portal does (one PUT, which deals the scripts evenly). Adding them one by one
    /// straight in the database let the live background sweep deal every script to the first one before the others arrived.
    /// </summary>
    private async Task<List<HttpClient>> ExaminersAsync(int examId, int count)
    {
        var staff = new List<ExamPrep.Domain.Entities.User>();
        for (var i = 0; i < count; i++)
        {
            staff.Add(await api.CreateStaffAsync(UserRole.Examiner, Password));
        }

        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{examId}/examiners", new { userIds = staff.Select(u => u.Id) }, ct)).EnsureSuccessStatusCode();
        var clients = new List<HttpClient>();
        foreach (var examiner in staff)
        {
            clients.Add(await api.StaffClientAsync(examiner.Username, Password));
        }

        return clients;
    }

    private async Task<JsonArray> ScriptsAsync(HttpClient examiner) => (await examiner.GetFromJsonAsync<JsonArray>("/api/staff/marking", ct))!;

    [Fact]
    public async Task TwentyScripts_AndFourExaminers_GiveFiveScriptsEach_ByCodeOnly()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(20);
        var examiners = await ExaminersAsync(exam.ExamId, 4);

        var lists = new List<JsonArray>();
        foreach (var examiner in examiners)
        {
            lists.Add(await ScriptsAsync(examiner));
        }

        var ofThisExam = lists.Select(l => l.Where(s => (int)s!["examId"]! == exam.ExamId).ToList()).ToList();
        Assert.All(ofThisExam, l => Assert.Equal(5, l.Count));
        Assert.Equal(20, ofThisExam.SelectMany(l => l).Select(s => (int)s!["attemptId"]!).Distinct().Count());
        Assert.All(ofThisExam.SelectMany(l => l), s => Assert.Matches("^[A-HJ-NP-Z2-9]{6}$", (string)s!["code"]!));
        var everything = string.Concat(lists.Select(l => l.ToJsonString()));
        Assert.All(candidates, c => Assert.DoesNotContain(c.LastName, everything));
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task AnExaminer_SeesAndMarksOnlyTheWritingOfTheirOwnScripts()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(2);
        var examiners = await ExaminersAsync(exam.ExamId, 2);
        var first = (await ScriptsAsync(examiners[0])).Single(s => (int)s!["examId"]! == exam.ExamId)!;
        var second = (await ScriptsAsync(examiners[1])).Single(s => (int)s!["examId"]! == exam.ExamId)!;
        var mine = (int)first["attemptId"]!;
        var theirs = (int)second["attemptId"]!;

        var marking = (await examiners[0].GetFromJsonAsync<JsonObject>($"/api/staff/attempts/{mine}/marking", ct))!;
        var otherMarking = await examiners[0].GetAsync($"/api/staff/attempts/{theirs}/marking", ct);
        var otherMark = await examiners[0].PostAsJsonAsync($"/api/staff/attempts/{theirs}/writing-marks", new { questionNumber = 31, score = 10 }, ct);
        var mark = await examiners[0].PostAsJsonAsync($"/api/staff/attempts/{mine}/writing-marks", new { questionNumber = 31, score = 10 }, ct);

        // A2 Reading & Writing: only the two Writing questions (31 and 32), with the code and no name.
        Assert.Equal([31, 32], marking["questions"]!.AsArray().Select(q => (int)q!["number"]!));
        Assert.Equal((string)first["code"]!, (string)marking["code"]!);
        Assert.All(candidates, c => Assert.DoesNotContain(c.LastName, marking.ToJsonString()));
        Assert.Equal(HttpStatusCode.NotFound, otherMarking.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherMark.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);
        Assert.Equal(10m, (decimal)(await mark.Content.ReadFromJsonAsync<JsonObject>(ct))!["rawScore"]!);
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task ScoresWithNames_AreForAdministrators_WhoSeeWhoMarksWhichCode()
    {
        api.SkipIfUnavailable();
        var (exam, _) = await FinishedSessionAsync(3);
        var examiners = await ExaminersAsync(exam.ExamId, 1);
        var scripts = await ScriptsAsync(examiners[0]);
        using var admin = await api.AdminClientAsync();

        var refused = await examiners[0].GetAsync($"/api/staff/exams/{exam.ExamId}/results", ct);
        var session = await examiners[0].GetAsync($"/api/staff/exams/{exam.ExamId}", ct);
        var results = (await admin.GetFromJsonAsync<JsonArray>($"/api/staff/exams/{exam.ExamId}/results", ct))!;

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, session.StatusCode);
        Assert.Equal(
            scripts.Where(s => (int)s!["examId"]! == exam.ExamId).Select(s => (string)s!["code"]!).Order(),
            results.Select(r => (string)r!["markingCode"]!).Order());
        Assert.All(results, r => Assert.NotNull((string?)r!["examiner"]));
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task AScript_IsDealtOnlyOnceItsSectionWasMarkedAutomatically()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        await api.StartAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        await api.SubmitStartedAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId);
        var examiners = await ExaminersAsync(exam.ExamId, 1);

        // Ended but not yet marked automatically: the Writing cannot be marked yet. The live sweep marks every 5 s, so the
        // list is only expected to be empty when the attempt was still ungraded before and after it was fetched.
        var ungradedBefore = (await api.GetAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId)).GradingStatus == GradingStatus.NotGraded;
        var before = (await ScriptsAsync(examiners[0])).Count(s => (int)s!["examId"]! == exam.ExamId);
        var ungradedAfter = (await api.GetAttemptAsync(exam.CandidateId, exam.ReadingWritingSectionId)).GradingStatus == GradingStatus.NotGraded;
        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        }

        var after = (await ScriptsAsync(examiners[0])).Count(s => (int)s!["examId"]! == exam.ExamId);

        if (ungradedBefore && ungradedAfter)
        {
            Assert.Equal(0, before);
        }

        Assert.Equal(1, after);
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task WhenAnExaminerLeaves_TheirUnmarkedScriptsAreDealtAgain()
    {
        api.SkipIfUnavailable();
        var (exam, _) = await FinishedSessionAsync(6);
        var stays = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var leaves = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { stays.Id, leaves.Id } }, ct)).EnsureSuccessStatusCode();
        using var staying = await api.StaffClientAsync(stays.Username, Password);
        Assert.Equal(3, (await ScriptsAsync(staying)).Count(s => (int)s!["examId"]! == exam.ExamId));

        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { stays.Id } }, ct)).EnsureSuccessStatusCode();

        Assert.Equal(6, (await ScriptsAsync(staying)).Count(s => (int)s!["examId"]! == exam.ExamId));
    }

    private async Task<int> AttemptIdAsync(int candidateId, int sectionId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.Attempts.Where(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId).Select(a => a.Id).SingleAsync(ct);
    }

    private async Task<int> CandidateOfAsync(int attemptId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.Attempts.Where(a => a.Id == attemptId).Select(a => a.CandidateId).SingleAsync(ct);
    }

    private Task<HttpResponseMessage> MarkAsync(HttpClient examiner, int attemptId, int question, decimal score) =>
        examiner.PostAsJsonAsync($"/api/staff/attempts/{attemptId}/writing-marks", new { questionNumber = question, score }, ct);

    private async Task<int?> ExaminerOfAsync(int candidateId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.Candidates.Where(c => c.Id == candidateId).Select(c => c.ExaminerUserId).SingleAsync(ct);
    }

    private async Task DeactivateAsync(int userId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), ct);
    }

    [Fact]
    public async Task TheMarkingView_CarriesEachWritingQuestionsPrompt_StimulusAndThePartsPassage_OfTheAttemptsPaper()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        var attemptId = await AttemptIdAsync(candidates[0].Id, exam.ReadingWritingSectionId);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var paperId = await db.Attempts.Where(a => a.Id == attemptId).Select(a => a.TestPaperId).SingleAsync(ct);
            var question = await db.Questions.SingleAsync(q => q.Number == 31 && q.QuestionGroup!.PaperPart!.TestPaperId == paperId, ct);
            question.PromptHtml = "<p>PROMPT-31</p>";
            question.StimulusHtml = "<p>STIMULUS-31</p>";
            await db.SaveChangesAsync(ct);
        }

        var examiners = await ExaminersAsync(exam.ExamId, 1);
        var marking = (await examiners[0].GetFromJsonAsync<JsonObject>($"/api/staff/attempts/{attemptId}/marking", ct))!;

        var q31 = marking["questions"]!.AsArray().Single(q => (int)q!["number"]! == 31)!;
        var q32 = marking["questions"]!.AsArray().Single(q => (int)q!["number"]! == 32)!;
        Assert.Equal("<p>PROMPT-31</p>", (string)q31["promptHtml"]!);
        Assert.Equal("<p>STIMULUS-31</p>", (string)q31["stimulusHtml"]!);
        Assert.Contains("Write an email to Jo", (string)q31["passageHtml"]!);
        Assert.Contains("Look at the three pictures", (string)q32["passageHtml"]!);
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task AMarkIsKeptInHistory_AndTheLatestCounts_AlsoForTheScore()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        var attemptId = await AttemptIdAsync(candidates[0].Id, exam.ReadingWritingSectionId);
        var examiners = await ExaminersAsync(exam.ExamId, 1);

        (await MarkAsync(examiners[0], attemptId, 31, 5)).EnsureSuccessStatusCode();
        (await MarkAsync(examiners[0], attemptId, 31, 9)).EnsureSuccessStatusCode();
        (await MarkAsync(examiners[0], attemptId, 31, 9)).EnsureSuccessStatusCode();
        var final = await MarkAsync(examiners[0], attemptId, 32, 0);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var scores = await db.ManualMarks.Where(m => m.Response!.AttemptId == attemptId && m.Response.Question!.Number == 31).OrderBy(m => m.Id).Select(m => m.Score).ToListAsync(ct);
        var attempt = await db.Attempts.AsNoTracking().SingleAsync(a => a.Id == attemptId, ct);
        Assert.Equal([5m, 9m, 9m], scores);
        Assert.Equal(9m, (decimal)(await final.Content.ReadFromJsonAsync<JsonObject>(ct))!["rawScore"]!);
        Assert.Equal(GradingStatus.Final, attempt.GradingStatus);
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task TwoMarksSavedAtOnce_BothSucceed_AndTheScoreAddsUp()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        var attemptId = await AttemptIdAsync(candidates[0].Id, exam.ReadingWritingSectionId);
        var examiners = await ExaminersAsync(exam.ExamId, 1);

        var both = await Task.WhenAll(MarkAsync(examiners[0], attemptId, 31, 7), MarkAsync(examiners[0], attemptId, 32, 4));

        Assert.All(both, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var attempt = await db.Attempts.AsNoTracking().SingleAsync(a => a.Id == attemptId, ct);
        Assert.Equal(GradingStatus.Final, attempt.GradingStatus);
        Assert.Equal(11m, attempt.RawScore - await db.Responses.Where(r => r.AttemptId == attemptId).SumAsync(r => r.AutoScore ?? 0, ct));
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task WhenAnExaminerLeaves_AFinalScriptStays_AndAPartMarkedOneIsDealtAgainKeepingItsMarks()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(2);
        var stays = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var leaves = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { stays.Id, leaves.Id } }, ct)).EnsureSuccessStatusCode();
        using var leaving = await api.StaffClientAsync(leaves.Username, Password);
        var leavingScripts = (await ScriptsAsync(leaving)).Where(s => (int)s!["examId"]! == exam.ExamId).Select(s => (int)s!["attemptId"]!).ToList();
        Assert.Single(leavingScripts);
        // The leaving examiner finishes their script (Final) ...
        (await MarkAsync(leaving, leavingScripts[0], 31, 10)).EnsureSuccessStatusCode();
        (await MarkAsync(leaving, leavingScripts[0], 32, 6)).EnsureSuccessStatusCode();
        var finalCandidate = await CandidateOfAsync(leavingScripts[0]);
        var otherCandidate = candidates.Single(c => c.Id != finalCandidate).Id;

        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { stays.Id } }, ct)).EnsureSuccessStatusCode();

        Assert.Equal(leaves.Id, await ExaminerOfAsync(finalCandidate));
        Assert.Equal(stays.Id, await ExaminerOfAsync(otherCandidate));

        // ... and a part-marked script of an examiner who leaves moves on with its marks.
        var second = await api.CreateStaffAsync(UserRole.Examiner, Password);
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { stays.Id, second.Id } }, ct)).EnsureSuccessStatusCode();
        // Adding an examiner deals unmarked scripts again, so either of the two may hold it now.
        var holder = await ExaminerOfAsync(otherCandidate) == stays.Id ? stays : second;
        var successor = holder == stays ? second : stays;
        using var staying = await api.StaffClientAsync(holder.Username, Password);
        var mine = (await ScriptsAsync(staying)).Where(s => (int)s!["examId"]! == exam.ExamId).Select(s => (int)s!["attemptId"]!).ToList();
        Assert.Single(mine);
        (await MarkAsync(staying, mine[0], 31, 8)).EnsureSuccessStatusCode();

        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { successor.Id } }, ct)).EnsureSuccessStatusCode();

        using var next = await api.StaffClientAsync(successor.Username, Password);
        var moved = (await next.GetFromJsonAsync<JsonObject>($"/api/staff/attempts/{mine[0]}/marking", ct))!;
        Assert.Equal(8m, (decimal)moved["questions"]!.AsArray().Single(q => (int)q!["number"]! == 31)!["score"]!);
        Assert.Equal(successor.Id, await ExaminerOfAsync(otherCandidate));
        Assert.Equal(leaves.Id, await ExaminerOfAsync(finalCandidate));
    }

    [Fact]
    public async Task ADeactivatedExaminer_NoLongerHoldsScripts_TheirUnmarkedOnesMoveOn()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(4);
        var first = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var second = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { first.Id, second.Id } }, ct)).EnsureSuccessStatusCode();
        await DeactivateAsync(first.Id);

        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<MarkingAllocator>().EnsureAllAsync(ct);
        }

        foreach (var candidate in candidates)
        {
            Assert.Equal(second.Id, await ExaminerOfAsync(candidate.Id));
        }
    }

    [Fact]
    public async Task ASessionWithoutAnEligibleExaminer_IsSkipped_AndScriptsStayWhereTheyAre()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(2);
        var only = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { only.Id } }, ct)).EnsureSuccessStatusCode();
        await DeactivateAsync(only.Id);

        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<MarkingAllocator>().EnsureAllAsync(ct);
        }

        foreach (var candidate in candidates)
        {
            Assert.Equal(only.Id, await ExaminerOfAsync(candidate.Id));
        }
    }

    /// <summary>
    /// Final review E: with no eligible examiner left the deal stops and the scripts keep the one they had, so the results
    /// say which scripts nobody can mark: their examiner is switched off, no longer an examiner or off the session.
    /// </summary>
    [Fact]
    public async Task Results_FlagTheScriptsWhoseExaminerCanNoLongerMarkThem()
    {
        api.SkipIfUnavailable();
        var (exam, _) = await FinishedSessionAsync(2);
        var (other, _) = await FinishedSessionAsync(1);
        var only = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var kept = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { only.Id } }, ct)).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{other.ExamId}/examiners", new { userIds = new[] { kept.Id } }, ct)).EnsureSuccessStatusCode();
        await DeactivateAsync(only.Id);
        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<MarkingAllocator>().EnsureAllAsync(ct);
        }

        var stuck = (await admin.GetFromJsonAsync<JsonArray>($"/api/staff/exams/{exam.ExamId}/results", ct))!;
        var fine = (await admin.GetFromJsonAsync<JsonArray>($"/api/staff/exams/{other.ExamId}/results", ct))!;

        Assert.All(stuck, r => Assert.True((bool)r!["examinerUnavailable"]!));
        Assert.All(stuck, r => Assert.Equal(only.FullName, (string?)r!["examiner"]));
        Assert.All(fine, r => Assert.False((bool)r!["examinerUnavailable"]!));

        // Off the session (taken off in the database, so no deal follows) or no longer an examiner: flagged as well.
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            await db.ExamExaminers.Where(x => x.ExamId == other.ExamId).ExecuteDeleteAsync(ct);
        }

        var offSession = (await admin.GetFromJsonAsync<JsonArray>($"/api/staff/exams/{other.ExamId}/results", ct))!;
        Assert.All(offSession, r => Assert.True((bool)r!["examinerUnavailable"]!));
    }

    [Fact]
    public async Task AnAccountThatIsNoLongerAnExaminer_IsNotDealtScripts()
    {
        api.SkipIfUnavailable();
        var (exam, _) = await FinishedSessionAsync(2);
        var wrongRole = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            db.ExamExaminers.Add(new ExamPrep.Domain.Entities.ExamExaminer { ExamId = exam.ExamId, UserId = wrongRole.Id, AssignedAt = DateTime.UtcNow });
            db.ExamExaminers.Add(new ExamPrep.Domain.Entities.ExamExaminer { ExamId = exam.ExamId, UserId = examiner.Id, AssignedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        using var client = await api.StaffClientAsync(examiner.Username, Password);
        Assert.Equal(2, (await ScriptsAsync(client)).Count(s => (int)s!["examId"]! == exam.ExamId));
    }

    [Fact]
    public async Task TheMarkingEndpoint_Answers404_ForASectionWithoutWriting()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        await api.StartAttemptAsync(candidates[0].Id, exam.ListeningSectionId);
        await api.SubmitStartedAttemptAsync(candidates[0].Id, exam.ListeningSectionId);
        using (var scope = api.Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        }

        var examiners = await ExaminersAsync(exam.ExamId, 1);
        var listening = await AttemptIdAsync(candidates[0].Id, exam.ListeningSectionId);

        var response = await examiners[0].GetAsync($"/api/staff/attempts/{listening}/marking", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        examiners.ForEach(e => e.Dispose());
    }

    [Fact]
    public async Task AnExaminer_ReadsOnlyTheWritingFilesOfTheirScripts()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        var attemptId = await AttemptIdAsync(candidates[0].Id, exam.ReadingWritingSectionId);
        var examiners = await ExaminersAsync(exam.ExamId, 1);
        int writing, reading;
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            var paperId = await db.Attempts.Where(a => a.Id == attemptId).Select(a => a.TestPaperId).SingleAsync(ct);
            var part7 = await db.PaperParts.SingleAsync(p => p.TestPaperId == paperId && p.PartNo == 7, ct);
            writing = int.Parse(System.Text.RegularExpressions.Regex.Match(part7.StimulusHtml!, @"/api/assets/(\d+)").Groups[1].Value);
            var part1 = await db.Questions.Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == paperId && q.QuestionGroup.PaperPart.PartNo == 1)
                .Select(q => (q.OptionsJson ?? "") + (q.PromptHtml ?? "") + (q.StimulusHtml ?? "") + (q.QuestionGroup!.BodyHtml ?? "") + (q.QuestionGroup.OptionPoolJson ?? ""))
                .ToListAsync(ct);
            reading = int.Parse(System.Text.RegularExpressions.Regex.Match(string.Concat(part1), @"(?:imageAssetId""\s*:\s*|/api/assets/)(\d+)").Groups[1].Value);
        }

        var writingFile = await examiners[0].GetAsync($"/api/assets/{writing}", ct);
        var readingFile = await examiners[0].GetAsync($"/api/assets/{reading}", ct);

        Assert.Equal(HttpStatusCode.OK, writingFile.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readingFile.StatusCode);
        examiners.ForEach(e => e.Dispose());
    }

    /// <summary>Runs <see cref="Before"/> once, just before the request's first locking read (FOR UPDATE / FOR SHARE), while it holds no lock yet.</summary>
    private sealed class FirstLockHook : DbCommandInterceptor
    {
        private int fired;

        public Func<Task>? Before;

        public override async ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Before is not null && (command.CommandText.Contains("FOR UPDATE") || command.CommandText.Contains("FOR SHARE")) && Interlocked.Exchange(ref fired, 1) == 0)
            {
                await Before();
            }

            return result;
        }
    }

    /// <summary>
    /// Final review 11: the script is dealt to another examiner (the examiners changed) after the request checked it was
    /// this examiner's but before the mark is saved. The save checks again under its lock and refuses.
    /// </summary>
    [Fact]
    public async Task AMark_IsRefused_WhenTheScriptWasDealtToSomeoneElseWhileItWasBeingSaved()
    {
        api.SkipIfUnavailable();
        var (exam, candidates) = await FinishedSessionAsync(1);
        var first = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var second = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using (var admin = await api.AdminClientAsync())
        {
            (await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}/examiners", new { userIds = new[] { first.Id } }, ct)).EnsureSuccessStatusCode();
        }

        var attemptId = await AttemptIdAsync(candidates[0].Id, exam.ReadingWritingSectionId);
        Assert.Equal(first.Id, await ExaminerOfAsync(candidates[0].Id));
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            db.ExamExaminers.Add(new ExamPrep.Domain.Entities.ExamExaminer { ExamId = exam.ExamId, UserId = second.Id, AssignedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        var hook = new FirstLockHook();
        hook.Before = async () =>
        {
            using var scope = api.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
            await db.Candidates.Where(c => c.Id == candidates[0].Id).ExecuteUpdateAsync(set => set.SetProperty(c => c.ExaminerUserId, second.Id), CancellationToken.None);
        };
        var factory = api.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.ConfigureDbContext<ExamPrepDbContext>(options => options.AddInterceptors(hook))));
        using var examiner = factory.CreateClient();
        (await examiner.PostAsJsonAsync("/api/staff/auth/login", new { username = first.Username, password = Password }, ct)).EnsureSuccessStatusCode();

        var mark = await MarkAsync(examiner, attemptId, 31, 3);

        Assert.Equal(HttpStatusCode.Conflict, mark.StatusCode);
        Assert.Contains("another examiner", await mark.Content.ReadAsStringAsync(ct));
        using var check = api.Factory.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<ExamPrepDbContext>().ManualMarks.AnyAsync(m => m.Response!.AttemptId == attemptId, ct));
    }
}
