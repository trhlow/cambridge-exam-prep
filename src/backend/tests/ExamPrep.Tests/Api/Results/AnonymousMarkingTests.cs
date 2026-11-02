using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
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
}
