using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Auth;

/// <summary>
/// VE-60 (Victory, 16/10/2026): so that no single administrator can abuse their power, deletions and staff account
/// changes wait for an administrator other than the requester. A manager's request needs one administrator; an
/// administrator's request needs the other one. Every request, decision and outcome stays as the approval log.
/// </summary>
public sealed class ApprovalTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Approve#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record Approval(int Id, string Kind, int? TargetId, string Summary, string Status, int RequestedById, string RequestedBy, int? DecidedById, string? DecidedBy, string? DecisionNote, string? Outcome);

    private Task<HttpResponseMessage> ApproveAsync(HttpClient client, int id) => client.PostAsync($"/api/staff/approvals/{id}/approve", null, ct);

    [Fact]
    public async Task AdminsDeletion_WaitsForTheOtherAdmin_AndTheRequesterCannotApproveIt()
    {
        api.SkipIfUnavailable();
        var (paperId, _) = await api.CreatePublishedPapersAsync();
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();

        var request = await admin.DeleteAsync($"/api/admin/papers/{paperId}", ct);
        var id = await ApiFixture.RequestIdOfAsync(request);
        var stillThere = await admin.GetAsync($"/api/admin/papers/{paperId}", ct);
        var bySelf = await ApproveAsync(admin, id);
        var byOther = await ApproveAsync(otherAdmin, id);
        var gone = await admin.GetAsync($"/api/admin/papers/{paperId}", ct);

        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, bySelf.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byOther.StatusCode);
        var decided = (await byOther.Content.ReadFromJsonAsync<Approval>(ct))!;
        Assert.Equal(("Approved", "DeletePaper", paperId), (decided.Status, decided.Kind, decided.TargetId));
        Assert.NotEqual(decided.RequestedById, decided.DecidedById);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task ManagersDeletion_IsApprovedByAnAdmin_NeverByAManager()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var requester = await api.CreateManagerAsync(Password, ManagerArea.Candidates);
        var colleague = await api.CreateManagerAsync(Password, ManagerArea.Candidates, ManagerArea.Exams);
        using var manager = await api.StaffClientAsync(requester.Username, Password);
        using var otherManager = await api.StaffClientAsync(colleague.Username, Password);
        using var admin = await api.AdminClientAsync();

        var request = await manager.DeleteAsync($"/api/admin/candidates/{candidate.CandidateId}", ct);
        var id = await ApiFixture.RequestIdOfAsync(request);
        var byManager = await ApproveAsync(otherManager, id);
        var byAdmin = await ApproveAsync(admin, id);
        var candidates = await admin.GetFromJsonAsync<List<JsonObject>>($"/api/admin/exams/{candidate.ExamId}/candidates", ct);

        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byManager.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
        Assert.Empty(candidates!);
    }

    [Fact]
    public async Task Reject_NeedsAReason_AndKeepsTheTarget()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();
        var id = await ApiFixture.RequestIdOfAsync(await admin.DeleteAsync($"/api/admin/exams/{candidate.ExamId}", ct));

        var noReason = await otherAdmin.PostAsJsonAsync($"/api/staff/approvals/{id}/reject", new { note = "" }, ct);
        var rejected = await otherAdmin.PostAsJsonAsync($"/api/staff/approvals/{id}/reject", new { note = "Still needed for the mock test." }, ct);
        var approveLater = await ApproveAsync(otherAdmin, id);
        var exam = await admin.GetAsync($"/api/staff/exams/{candidate.ExamId}", ct);

        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        var decision = (await rejected.Content.ReadFromJsonAsync<Approval>(ct))!;
        Assert.Equal(("Rejected", "Still needed for the mock test."), (decision.Status, decision.DecisionNote));
        Assert.Equal(HttpStatusCode.Conflict, approveLater.StatusCode);
        Assert.Equal(HttpStatusCode.OK, exam.StatusCode);
    }

    [Fact]
    public async Task OnlyTheRequesterCancels()
    {
        api.SkipIfUnavailable();
        var (paperId, _) = await api.CreatePublishedPapersAsync();
        var account = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var manager = await api.StaffClientAsync(account.Username, Password);
        using var admin = await api.AdminClientAsync();
        var id = await ApiFixture.RequestIdOfAsync(await manager.DeleteAsync($"/api/admin/papers/{paperId}", ct));

        var byAdmin = await admin.PostAsync($"/api/staff/approvals/{id}/cancel", null, ct);
        var bySelf = await manager.PostAsync($"/api/staff/approvals/{id}/cancel", null, ct);

        Assert.Equal(HttpStatusCode.Forbidden, byAdmin.StatusCode);
        Assert.Equal("Cancelled", (await bySelf.Content.ReadFromJsonAsync<Approval>(ct))!.Status);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/papers/{paperId}", ct)).StatusCode);
    }

    [Fact]
    public async Task ARequestThatCanNoLongerBeCarriedOut_EndsAsFailedWithTheReason()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();
        var id = await ApiFixture.RequestIdOfAsync(await admin.DeleteAsync($"/api/admin/candidates/{candidate.CandidateId}", ct));
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);

        var approved = await ApproveAsync(otherAdmin, id);

        var outcome = (await approved.Content.ReadFromJsonAsync<Approval>(ct))!;
        Assert.Equal("Failed", outcome.Status);
        Assert.Contains("started", outcome.Outcome);
        Assert.Single((await admin.GetFromJsonAsync<List<JsonObject>>($"/api/admin/exams/{candidate.ExamId}/candidates", ct))!);
    }

    [Fact]
    public async Task ImpossibleOrDuplicateDeletions_AreRefusedAtOnce()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ReadingWritingSectionId);
        var (paperId, _) = await api.CreatePublishedPapersAsync();
        using var admin = await api.AdminClientAsync();

        var started = await admin.DeleteAsync($"/api/admin/candidates/{candidate.CandidateId}", ct);
        var first = await admin.DeleteAsync($"/api/admin/papers/{paperId}", ct);
        var duplicate = await admin.DeleteAsync($"/api/admin/papers/{paperId}", ct);
        var missing = await admin.DeleteAsync("/api/admin/papers/999999", ct);

        Assert.Equal(HttpStatusCode.Conflict, started.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task AccountChanges_WaitForApproval_ThenSignTheAccountOut()
    {
        api.SkipIfUnavailable();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var signedIn = await api.StaffClientAsync(invigilator.Username, Password);
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();

        var change = await admin.PutAsJsonAsync($"/api/admin/users/{invigilator.Id}", new { fullName = "Renamed now", isActive = false }, ct);
        var body = (await change.Content.ReadFromJsonAsync<JsonObject>(ct))!;
        var meBeforeApproval = await signedIn.GetAsync("/api/staff/auth/me", ct);
        await ApiFixture.ApproveAsync(otherAdmin, (int)body["request"]!["id"]!);
        var meAfterApproval = await signedIn.GetAsync("/api/staff/auth/me", ct);

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal("Renamed now", (string)body["user"]!["fullName"]!);
        Assert.True((bool)body["user"]!["isActive"]!);
        Assert.Equal("Pending", (string)body["request"]!["status"]!);
        Assert.Equal(HttpStatusCode.OK, meBeforeApproval.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterApproval.StatusCode);
    }

    [Fact]
    public async Task RoleAndAreaChanges_TakeEffectOnceApproved()
    {
        api.SkipIfUnavailable();
        var account = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();

        var change = await admin.PutAsJsonAsync($"/api/admin/users/{account.Id}", new { fullName = "Ms Hoa", isActive = true, role = "Manager", areas = new[] { "Papers", "Exams" } }, ct);
        await ApiFixture.ApproveAsync(otherAdmin, await ApiFixture.RequestIdOfAsync(change));
        using var manager = await api.StaffClientAsync(account.Username, Password);

        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/admin/exam-types", ct)).StatusCode);
    }

    [Fact]
    public async Task PasswordResets_ApplyOnlyOnceApproved()
    {
        api.SkipIfUnavailable();
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();

        var change = await admin.PutAsJsonAsync($"/api/admin/users/{examiner.Id}", new { fullName = "Mr Nam", isActive = true, newPassword = "new-pass-2026" }, ct);
        var newPasswordBefore = await LoginAsync(examiner.Username, "new-pass-2026");
        var requestId = await ApiFixture.RequestIdOfAsync(change);
        await ApiFixture.ApproveAsync(otherAdmin, requestId);
        var newPasswordAfter = await LoginAsync(examiner.Username, "new-pass-2026");

        Assert.Equal(HttpStatusCode.Unauthorized, newPasswordBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPasswordAfter.StatusCode);

        // The decided request no longer keeps the password hash it carried.
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var payload = await db.ApprovalRequests.Where(r => r.Id == requestId).Select(r => r.PayloadJson).SingleAsync(ct);
        Assert.Contains("\"passwordHash\":\"(set)\"", payload);
    }

    [Fact]
    public async Task AThirdAdministrator_NeedsTheOtherAdministratorsApproval()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        using var otherAdmin = await api.SecondAdminClientAsync();
        var username = $"adm{Guid.NewGuid():N}"[..20];

        var create = await admin.PostAsJsonAsync("/api/admin/users", new { username, fullName = "Third Admin", password = Password, role = "Admin" }, ct);
        var loginBefore = await LoginAsync(username, Password);
        await ApiFixture.ApproveAsync(otherAdmin, await ApiFixture.RequestIdOfAsync(create));
        var loginAfter = await LoginAsync(username, Password);

        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginAfter.StatusCode);
    }

    [Fact]
    public async Task TheLog_ShowsAdministratorsEveryRequest_AndOthersOnlyTheirOwn()
    {
        api.SkipIfUnavailable();
        var (paperA, paperB) = await api.CreatePublishedPapersAsync();
        var account = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var manager = await api.StaffClientAsync(account.Username, Password);
        using var admin = await api.AdminClientAsync();
        var managersRequest = await ApiFixture.RequestIdOfAsync(await manager.DeleteAsync($"/api/admin/papers/{paperA}", ct));
        var adminsRequest = await ApiFixture.RequestIdOfAsync(await admin.DeleteAsync($"/api/admin/papers/{paperB}", ct));

        var adminsView = await admin.GetFromJsonAsync<List<Approval>>("/api/staff/approvals", ct);
        var managersView = await manager.GetFromJsonAsync<List<Approval>>("/api/staff/approvals", ct);

        Assert.Contains(adminsView!, r => r.Id == managersRequest);
        Assert.Contains(adminsView!, r => r.Id == adminsRequest);
        Assert.Equal([managersRequest], managersView!.Select(r => r.Id));
    }

    private async Task<HttpResponseMessage> LoginAsync(string username, string password)
    {
        using var client = api.CreateClient();
        return await client.PostAsJsonAsync("/api/staff/auth/login", new { username, password }, ct);
    }
}

/// <summary>A new installation has one administrator, who creates the second one directly (nobody else could approve).</summary>
public sealed class FirstAdministratorTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task TheOnlyAdministrator_CreatesTheSecondOneDirectly()
    {
        api.SkipIfUnavailable();
        var ct = TestContext.Current.CancellationToken;
        using var admin = await api.AdminClientAsync();

        var create = await admin.PostAsJsonAsync("/api/admin/users", new { username = "second-admin", fullName = "Second Admin", password = "Second#Admin2026", role = "Admin" }, ct);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }
}
