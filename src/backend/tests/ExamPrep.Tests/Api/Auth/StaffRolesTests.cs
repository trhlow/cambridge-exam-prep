using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>
/// VE-59, VE-61 (Victory, 16/10/2026): administrators create every staff account; managers work only in the areas
/// they were given and never delete; examiners review finished work, mark Writing and see the results of the
/// sessions they are assigned to; administrators see the score table only; invigilators no longer see results.
/// </summary>
public sealed class StaffRolesTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Roles#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private sealed record ResponseItem(int QuestionNumber, string Value);

    private sealed record StaffProfile(int Id, string Username, string FullName, string? Email, string Role, bool IsActive, List<string> Areas);

    private static string NewUsername() => $"u{Guid.NewGuid():N}"[..20];

    /// <summary>A candidate who wrote question 31 of Reading &amp; Writing, ran out of time and was marked automatically.</summary>
    private async Task<(ApiFixture.SeededCandidate Candidate, int AttemptId)> FinishedReadingWritingAsync()
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        using var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        var section = candidate.ReadingWritingSectionId;
        (await client.PostAsync($"/api/candidate/sections/{section}/attempt", null, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/candidate/sections/{section}/responses", new { responses = new[] { new ResponseItem(31, "Hi Jo, see you on Saturday.") } }, ct)).EnsureSuccessStatusCode();
        await api.MoveAttemptStartBackAsync(candidate.CandidateId, section, TimeSpan.FromMinutes(61));
        using var scope = api.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AttemptExpiry>().SubmitExpiredAsync(ct);
        await scope.ServiceProvider.GetRequiredService<AttemptMarker>().MarkPendingAsync(ct);
        return (candidate, (await api.GetAttemptAsync(candidate.CandidateId, section)).Id);
    }

    private Task<HttpResponseMessage> MarkWritingAsync(HttpClient client, int attemptId) =>
        client.PostAsJsonAsync($"/api/staff/attempts/{attemptId}/writing-marks", new { questionNumber = 31, score = 10 }, ct);

    [Fact]
    public async Task Admin_CreatesAManagerWithAreas_AndTheManagerSeesThemOnTheirProfile()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var username = NewUsername();

        var create = await admin.PostAsJsonAsync("/api/admin/users", new { username, fullName = "Ms Hoa", password = Password, role = "Manager", areas = new[] { "Papers", "Candidates" } }, ct);
        using var manager = await api.StaffClientAsync(username, Password);
        var me = await manager.GetFromJsonAsync<StaffProfile>("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal("Manager", me!.Role);
        Assert.Equal(["Candidates", "Papers"], me.Areas.Order());
    }

    [Fact]
    public async Task Create_RefusesAManagerWithoutAreas_AndAreasForOtherRoles()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();

        var noAreas = await admin.PostAsJsonAsync("/api/admin/users", new { username = NewUsername(), fullName = "A", password = Password, role = "Manager", areas = Array.Empty<string>() }, ct);
        var unknownArea = await admin.PostAsJsonAsync("/api/admin/users", new { username = NewUsername(), fullName = "B", password = Password, role = "Manager", areas = new[] { "Marks" } }, ct);
        var invigilatorWithArea = await admin.PostAsJsonAsync("/api/admin/users", new { username = NewUsername(), fullName = "C", password = Password, role = "Invigilator", areas = new[] { "Papers" } }, ct);
        var examiner = await admin.PostAsJsonAsync("/api/admin/users", new { username = NewUsername(), fullName = "D", password = Password, role = "Examiner" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, noAreas.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownArea.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invigilatorWithArea.StatusCode);
        Assert.Equal(HttpStatusCode.Created, examiner.StatusCode);
    }

    [Fact]
    public async Task Manager_ReachesOnlyTheAreasTheyWereGiven()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var papersManager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var examsManager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var papers = await api.StaffClientAsync(papersManager.Username, Password);
        using var exams = await api.StaffClientAsync(examsManager.Username, Password);

        Assert.Equal(HttpStatusCode.OK, (await papers.GetAsync("/api/admin/papers", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await papers.GetAsync("/api/admin/exam-types", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await papers.GetAsync($"/api/admin/exams/{candidate.ExamId}/candidates", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await papers.GetAsync("/api/admin/users", ct)).StatusCode);
        Assert.DoesNotContain((await papers.GetFromJsonAsync<List<JsonObject>>("/api/staff/exams", ct))!, e => (int)e["id"]! == candidate.ExamId);

        Assert.Equal(HttpStatusCode.OK, (await exams.GetAsync("/api/admin/exam-types", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await exams.GetAsync("/api/admin/papers", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await exams.GetAsync($"/api/admin/exams/{candidate.ExamId}/candidates", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await exams.PostAsJsonAsync($"/api/admin/exams/{candidate.ExamId}/candidates", new { candidates = Array.Empty<object>() }, ct)).StatusCode);
        Assert.Contains((await exams.GetFromJsonAsync<List<JsonObject>>("/api/staff/exams", ct))!, e => (int)e["id"]! == candidate.ExamId);
    }

    [Fact]
    public async Task CandidateList_StillNeedsASignedInStaffMember()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var anonymous = api.CreateClient();

        var response = await anonymous.GetAsync($"/api/admin/exams/{candidate.ExamId}/candidates", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Manager_OnlyRequestsDeletions_AndOnlyInTheirAreas()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var (paperId, _) = await api.CreatePublishedPapersAsync();
        var account = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var manager = await api.StaffClientAsync(account.Username, Password);

        Assert.Equal(HttpStatusCode.Accepted, (await manager.DeleteAsync($"/api/admin/papers/{paperId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"/api/admin/papers/{paperId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.DeleteAsync($"/api/admin/candidates/{candidate.CandidateId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.DeleteAsync($"/api/admin/exams/{candidate.ExamId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/admin/users/{account.Id}", new { fullName = "Me", isActive = true, areas = new[] { "Papers", "Exams" } }, ct)).StatusCode);
    }

    [Fact]
    public async Task Examiner_SeesFinishedWorkAndResults_OnlyForAssignedSessions_AndNeverMonitors()
    {
        api.SkipIfUnavailable();
        var (candidate, attemptId) = await FinishedReadingWritingAsync();
        var account = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var examiner = await api.StaffClientAsync(account.Username, Password);
        using var admin = await api.AdminClientAsync();

        var resultsBefore = await examiner.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct);
        var markingBefore = await examiner.GetAsync($"/api/staff/attempts/{attemptId}/marking", ct);
        var assign = await admin.PutAsJsonAsync($"/api/admin/exams/{candidate.ExamId}/examiners", new { userIds = new[] { account.Id } }, ct);
        var results = await examiner.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct);
        var marking = await examiner.GetAsync($"/api/staff/attempts/{attemptId}/marking", ct);
        var mark = await MarkWritingAsync(examiner, attemptId);
        var monitor = await examiner.GetAsync($"/api/staff/exams/{candidate.ExamId}/sections/{candidate.ReadingWritingSectionId}/monitor", ct);
        var exams = await examiner.GetFromJsonAsync<List<JsonObject>>("/api/staff/exams", ct);
        var detail = await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{candidate.ExamId}", ct);

        Assert.Equal(HttpStatusCode.NotFound, resultsBefore.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, markingBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        Assert.Equal(HttpStatusCode.OK, marking.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, monitor.StatusCode);
        Assert.Contains(exams!, e => (int)e["id"]! == candidate.ExamId);
        Assert.Contains(detail!["examiners"]!.AsArray(), e => (int)e!["id"]! == account.Id);
    }

    [Fact]
    public async Task Examiner_SeesAttemptsOnlyOnceTheSectionHasEnded()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var attemptId = await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        var account = await api.CreateStaffAsync(UserRole.Examiner, Password);
        await api.AssignExaminerAsync(candidate.ExamId, account.Id);
        using var examiner = await api.StaffClientAsync(account.Username, Password);

        var marking = await examiner.GetAsync($"/api/staff/attempts/{attemptId}/marking", ct);

        Assert.Equal(HttpStatusCode.Conflict, marking.StatusCode);
    }

    [Fact]
    public async Task Admin_SeesTheScoreTable_ButNotTheWorkOrTheWritingMarks()
    {
        api.SkipIfUnavailable();
        var (candidate, attemptId) = await FinishedReadingWritingAsync();
        using var admin = await api.AdminClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"/api/staff/attempts/{attemptId}/marking", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MarkWritingAsync(admin, attemptId)).StatusCode);
    }

    [Fact]
    public async Task InvigilatorsAndManagers_DoNotSeeResults()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var invigilatorAccount = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        var managerAccount = await api.CreateManagerAsync(Password, ManagerArea.Exams, ManagerArea.Candidates);
        using var admin = await api.AdminClientAsync();
        (await admin.PutAsJsonAsync($"/api/admin/exams/{candidate.ExamId}/invigilators", new { userIds = new[] { invigilatorAccount.Id } }, ct)).EnsureSuccessStatusCode();
        using var invigilator = await api.StaffClientAsync(invigilatorAccount.Username, Password);
        using var manager = await api.StaffClientAsync(managerAccount.Username, Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await invigilator.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"/api/staff/exams/{candidate.ExamId}/results", ct)).StatusCode);
    }

    [Fact]
    public async Task Examiners_MustBeActiveExaminerAccounts()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var admin = await api.AdminClientAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/exams/{candidate.ExamId}/examiners", new { userIds = new[] { invigilator.Id } }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
