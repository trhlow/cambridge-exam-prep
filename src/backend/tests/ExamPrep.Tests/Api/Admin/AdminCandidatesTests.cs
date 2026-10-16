using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>
/// VE-22: candidates are entered for a session (one by one or pasted from a spreadsheet); each gets a Cambridge-style
/// username and a password printed on a login slip that can be printed again.
/// </summary>
public sealed class AdminCandidatesTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string StaffPassword = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    public sealed record NewCandidate(string CandidateNumber, string FirstName, string LastName, DateOnly? DateOfBirth = null, int PlannedExtraPercent = 0, string? SeatNo = null);

    public sealed record CandidateView(int Id, string CandidateNumber, string FirstName, string LastName, string Username, DateOnly? DateOfBirth, int PlannedExtraPercent, string? SeatNo, bool HasStarted);

    public sealed record LoginSlip(int CandidateId, string CandidateNumber, string FullName, string Username, string Password, string ExamName, DateOnly ScheduledDate, string? Room);

    private sealed record LoginRequest(string Username, string Password);

    /// <summary>An empty, open A2 session created through the admin API.</summary>
    private async Task<int> CreateOpenExamAsync(HttpClient admin)
    {
        var papers = await api.CreatePublishedPapersAsync();
        var create = await admin.PostAsJsonAsync("/api/admin/exams", new
        {
            name = $"A2 {Guid.NewGuid():N}"[..20],
            examTypeId = 1,
            scheduledDate = "2026-11-20",
            room = "Room 1",
            papers = new[] { new { sectionTemplateId = 1, testPaperId = papers.ReadingWritingId }, new { sectionTemplateId = 2, testPaperId = papers.ListeningId } },
        }, ct);
        create.EnsureSuccessStatusCode();
        var examId = (await create.Content.ReadFromJsonAsync<IdOnly>(ct))!.Id;
        (await admin.PostAsJsonAsync($"/api/admin/exams/{examId}/status", new { status = "Active" }, ct)).EnsureSuccessStatusCode();
        return examId;
    }

    private sealed record IdOnly(int Id);

    private static string Number() => Random.Shared.Next(1000, 9999).ToString();

    [Fact]
    public async Task Add_GivesEachCandidateACambridgeUsernameAndAPasswordThatSignsThemIn()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var examId = await CreateOpenExamAsync(admin);
        var number = Number();

        var add = await admin.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", new { candidates = new[] { new NewCandidate(number, "An", "Nguyễn", PlannedExtraPercent: 25), new NewCandidate(Number(), "Thị Hoa", "Lê") } }, ct);
        var slips = await admin.GetFromJsonAsync<List<LoginSlip>>($"/api/staff/exams/{examId}/login-slips", ct);

        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var created = await add.Content.ReadFromJsonAsync<List<CandidateView>>(ct);
        Assert.StartsWith($"nguyea{number}", created![0].Username);
        Assert.Equal(25, created[0].PlannedExtraPercent);
        var slip = slips!.Single(s => s.CandidateNumber == number);
        Assert.Equal("An Nguyễn", slip.FullName);
        Assert.True(slip.Password.Length >= 8);
        using var candidate = api.CreateClient();
        var login = await candidate.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(slip.Username, slip.Password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Add_RefusesTheWholeListWhenARowIsInvalid()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var examId = await CreateOpenExamAsync(admin);
        var number = Number();

        var duplicate = await admin.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", new { candidates = new[] { new NewCandidate(number, "An", "Tran"), new NewCandidate(number, "Binh", "Pham"), new NewCandidate(Number(), "", "Vo") } }, ct);
        var list = await admin.GetFromJsonAsync<List<CandidateView>>($"/api/admin/exams/{examId}/candidates", ct);

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var body = await duplicate.Content.ReadAsStringAsync(ct);
        Assert.Contains($"Row 2: candidate number {number} is already in the list.", body);
        Assert.Contains("Row 3", body);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Add_KeepsUsernamesUniqueAcrossSessions()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var first = await CreateOpenExamAsync(admin);
        var second = await CreateOpenExamAsync(admin);
        var number = Number();

        var a = await admin.PostAsJsonAsync($"/api/admin/exams/{first}/candidates", new { candidates = new[] { new NewCandidate(number, "An", "Hoang") } }, ct);
        var b = await admin.PostAsJsonAsync($"/api/admin/exams/{second}/candidates", new { candidates = new[] { new NewCandidate(number, "Anh", "Hoang") } }, ct);

        var usernameA = (await a.Content.ReadFromJsonAsync<List<CandidateView>>(ct))![0].Username;
        var usernameB = (await b.Content.ReadFromJsonAsync<List<CandidateView>>(ct))![0].Username;
        Assert.Equal($"hoanga{number}", usernameA);
        Assert.NotEqual(usernameA, usernameB);
        Assert.StartsWith($"hoanga{number}", usernameB);
    }

    [Fact]
    public async Task UpdateAndDelete_AreAllowedUntilTheCandidateStarts()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var seeded = await api.CreateCandidateAsync("x-pass-1234");
        var examId = await CreateOpenExamAsync(admin);
        var added = await admin.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", new { candidates = new[] { new NewCandidate(Number(), "Cuong", "Do") } }, ct);
        var fresh = (await added.Content.ReadFromJsonAsync<List<CandidateView>>(ct))![0];
        await api.StartAttemptAsync(seeded.CandidateId, seeded.ReadingWritingSectionId);

        var update = await admin.PutAsJsonAsync($"/api/admin/candidates/{fresh.Id}", new { firstName = "Cuong", lastName = "Do", plannedExtraPercent = 50, seatNo = "B4" }, ct);
        var deleteStarted = await admin.DeleteAsync($"/api/admin/candidates/{seeded.CandidateId}", ct);
        var deleteFresh = await admin.DeleteAsync($"/api/admin/candidates/{fresh.Id}", ct);
        using var otherAdmin = await api.SecondAdminClientAsync();
        await ApiFixture.ApproveAsync(otherAdmin, await ApiFixture.RequestIdOfAsync(deleteFresh));
        var remaining = await admin.GetFromJsonAsync<List<CandidateView>>($"/api/admin/exams/{examId}/candidates", ct);

        var updated = await update.Content.ReadFromJsonAsync<CandidateView>(ct);
        Assert.Equal(50, updated!.PlannedExtraPercent);
        Assert.Equal("B4", updated.SeatNo);
        Assert.Equal(fresh.Username, updated.Username);
        Assert.Equal(HttpStatusCode.Conflict, deleteStarted.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, deleteFresh.StatusCode);
        Assert.DoesNotContain(remaining!, c => c.Id == fresh.Id);
    }

    [Fact]
    public async Task LoginSlips_AreForAdministratorsAndTheSessionsInvigilatorsOnly()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var examId = await CreateOpenExamAsync(admin);
        var assigned = await api.CreateStaffAsync(UserRole.Invigilator, StaffPassword);
        var outsider = await api.CreateStaffAsync(UserRole.Invigilator, StaffPassword);
        (await admin.PutAsJsonAsync($"/api/admin/exams/{examId}/invigilators", new { userIds = new[] { assigned.Id } }, ct)).EnsureSuccessStatusCode();

        using var assignedClient = await api.StaffClientAsync(assigned.Username, StaffPassword);
        using var outsiderClient = await api.StaffClientAsync(outsider.Username, StaffPassword);

        Assert.Equal(HttpStatusCode.OK, (await assignedClient.GetAsync($"/api/staff/exams/{examId}/login-slips", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.GetAsync($"/api/staff/exams/{examId}/login-slips", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await assignedClient.GetAsync($"/api/admin/exams/{examId}/candidates", ct)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_GivesANewPasswordForTheSlipAndSignsOutTheOldSession()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var examId = await CreateOpenExamAsync(admin);
        var added = await admin.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", new { candidates = new[] { new NewCandidate(Number(), "Giang", "Vu") } }, ct);
        var candidate = (await added.Content.ReadFromJsonAsync<List<CandidateView>>(ct))![0];
        var before = (await admin.GetFromJsonAsync<List<LoginSlip>>($"/api/staff/exams/{examId}/login-slips", ct))!.Single(s => s.CandidateId == candidate.Id);
        using var oldSession = api.CreateClient();
        (await oldSession.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(before.Username, before.Password!), ct)).EnsureSuccessStatusCode();

        var reset = await admin.PostAsync($"/api/admin/candidates/{candidate.Id}/reset-password", null, ct);
        var slip = await reset.Content.ReadFromJsonAsync<LoginSlip>(ct);
        using var fresh = api.CreateClient();
        var oldPassword = await fresh.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(before.Username, before.Password!), ct);
        var newPassword = await fresh.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(slip!.Username, slip.Password!), ct);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.NotEqual(before.Password, slip.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldSession.GetAsync("/api/candidate/auth/me", ct)).StatusCode);
    }
}
