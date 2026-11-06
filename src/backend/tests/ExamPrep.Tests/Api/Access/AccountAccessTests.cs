using System.Net;
using System.Net.Http.Json;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Exams;
using ExamPrep.Api.Realtime;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Access;

/// <summary>
/// Review of 2/11, task 7: what an account may reach follows its role now, not the assignments it had before a role
/// change (C3); the asset endpoint decides staff and candidates apart when a browser holds both cookies (C6); the
/// requests of an administrator who is switched off stop waiting (C7); the staff hub asks the database, not the
/// claims of a connection made hours ago.
/// </summary>
public sealed class AccountAccessTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Access#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private static ExamPrepDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();

    private async Task ChangeAccountAsync(int userId, object body)
    {
        using var admin = await api.AdminClientAsync();
        using var other = await api.SecondAdminClientAsync();
        var change = await admin.PutAsJsonAsync($"/api/admin/users/{userId}", body, ct);
        await ApiFixture.ApproveAsync(other, await ApiFixture.RequestIdOfAsync(change));
    }

    [Fact]
    public async Task AnInvigilatorWhoBecomesAnExaminer_LosesTheirSessions()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password);
        var account = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = Db(scope);
            db.ExamInvigilators.Add(new ExamInvigilator { ExamId = exam.ExamId, UserId = account.Id, AssignedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        await ChangeAccountAsync(account.Id, new { fullName = "Now examiner", isActive = true, role = "Examiner" });
        using var staff = await api.StaffClientAsync(account.Username, Password);

        using var check = api.Factory.Services.CreateScope();
        Assert.False(await Db(check).ExamInvigilators.AnyAsync(x => x.UserId == account.Id, ct));
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync($"/api/staff/exams/{exam.ExamId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync($"/api/staff/exams/{exam.ExamId}/login-slips", ct)).StatusCode);
    }

    [Fact]
    public async Task AnExaminerWhoBecomesAnInvigilator_LosesTheSession_ButKeepsTheirScripts()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password);
        var account = await api.CreateStaffAsync(UserRole.Examiner, Password);
        await api.AssignExaminerAsync(exam.ExamId, account.Id);
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = Db(scope);
            var candidate = await db.Candidates.SingleAsync(c => c.Id == exam.CandidateId, ct);
            candidate.ExaminerUserId = account.Id;
            candidate.MarkingCode = "ABC234";
            await db.SaveChangesAsync(ct);
        }

        await ChangeAccountAsync(account.Id, new { fullName = "Now invigilator", isActive = true, role = "Invigilator" });

        using var check = api.Factory.Services.CreateScope();
        var after = Db(check);
        Assert.False(await after.ExamExaminers.AnyAsync(x => x.UserId == account.Id, ct));
        // The deal is the allocator's to redo (Final scripts stay with their examiner); the role change leaves it alone.
        var kept = await after.Candidates.SingleAsync(c => c.Id == exam.CandidateId, ct);
        Assert.Equal(account.Id, kept.ExaminerUserId);
        Assert.Equal("ABC234", kept.MarkingCode);
    }

    [Fact]
    public async Task AManagerWhoLosesTheCandidatesArea_LosesTheirSessions()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password);
        var account = await api.CreateManagerAsync(Password, ManagerArea.Candidates, ManagerArea.Papers);
        await api.AssignCandidateManagerAsync(exam.ExamId, account.Id);

        await ChangeAccountAsync(account.Id, new { fullName = "Papers only", isActive = true, role = "Manager", areas = new[] { "Papers" } });

        using var check = api.Factory.Services.CreateScope();
        Assert.False(await Db(check).ExamCandidateManagers.AnyAsync(x => x.UserId == account.Id, ct));
    }

    [Fact]
    public async Task TheFilters_AlsoRequireTheCurrentRole_WhateverAssignmentsRemain()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password);
        // Assignments left over from an earlier role: an examiner listed as invigilator, an invigilator as examiner.
        var examiner = await api.CreateStaffAsync(UserRole.Examiner, Password);
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        using var scope = api.Factory.Services.CreateScope();
        var db = Db(scope);
        db.ExamInvigilators.Add(new ExamInvigilator { ExamId = exam.ExamId, UserId = examiner.Id, AssignedAt = DateTime.UtcNow });
        db.ExamExaminers.Add(new ExamExaminer { ExamId = exam.ExamId, UserId = invigilator.Id, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var asExaminer = StaffAuthenticationSetup.CreatePrincipal(examiner);
        var asInvigilator = StaffAuthenticationSetup.CreatePrincipal(invigilator);

        Assert.False(await db.Exams.VisibleTo(asExaminer).AnyAsync(e => e.Id == exam.ExamId, ct));
        Assert.False(await db.Exams.MonitorableBy(asExaminer).AnyAsync(e => e.Id == exam.ExamId, ct));
        Assert.False(await db.Exams.SlipsVisibleTo(asExaminer).AnyAsync(e => e.Id == exam.ExamId, ct));
        Assert.False(await db.Exams.MarkableBy(asInvigilator).AnyAsync(e => e.Id == exam.ExamId, ct));
    }

    private static string CookieOf(HttpResponseMessage response) =>
        string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(v => v.Split(';')[0]));

    private async Task<string> StaffCookieAsync(string username, string password)
    {
        using var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/staff/auth/login", new { username, password }, ct);
        login.EnsureSuccessStatusCode();
        return CookieOf(login);
    }

    private async Task<string> CandidateCookieAsync(ApiFixture.SeededCandidate candidate)
    {
        using var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/candidate/auth/login", new { username = candidate.Username, password = Password }, ct);
        login.EnsureSuccessStatusCode();
        using var pin = new HttpRequestMessage(HttpMethod.Post, "/api/candidate/auth/pin") { Content = JsonContent.Create(new { pin = candidate.SessionPin }) };
        pin.Headers.Add("Cookie", CookieOf(login));
        var pinned = await client.SendAsync(pin, ct);
        pinned.EnsureSuccessStatusCode();
        return CookieOf(pinned);
    }

    private async Task<HttpStatusCode> GetAssetAsync(int assetId, params string[] cookies)
    {
        using var client = api.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/assets/{assetId}");
        request.Headers.Add("Cookie", string.Join("; ", cookies));
        return (await client.SendAsync(request, ct)).StatusCode;
    }

    [Fact]
    public async Task AssetsWithBothCookies_AreDecidedPerIdentity()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var other = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var author = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        await api.StartAttemptAsync(candidate.CandidateId, candidate.ListeningSectionId);
        int audio, ownedByAuthor;
        using (var scope = api.Factory.Services.CreateScope())
        {
            var db = Db(scope);
            var paperId = await db.ExamSections.Where(s => s.Id == candidate.ListeningSectionId).Select(s => s.TestPaperId).SingleAsync(ct);
            audio = await db.PaperParts.Where(p => p.TestPaperId == paperId && p.AudioAssetId != null).Select(p => p.AudioAssetId!.Value).FirstAsync(ct);
            var upload = new Asset
            {
                Kind = AssetKind.Image,
                OriginalFileName = "x.png",
                StoredPath = await db.Assets.Select(a => a.StoredPath).FirstAsync(ct),
                ContentType = "image/png",
                SizeBytes = 1,
                Sha256 = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                UploadedByUserId = author.Id,
                CreatedAt = DateTime.UtcNow,
            };
            db.Assets.Add(upload);
            await db.SaveChangesAsync(ct);
            ownedByAuthor = upload.Id;
        }

        var candidateCookie = await CandidateCookieAsync(candidate);
        var otherCookie = await StaffCookieAsync(other.Username, Password);
        var authorCookie = await StaffCookieAsync(author.Username, Password);

        // A staff cookie of someone else in the same browser must not take away what the candidate may read, nor
        // give the candidate the staff member's files, nor the staff member the candidate's.
        Assert.Equal(HttpStatusCode.OK, await GetAssetAsync(audio, candidateCookie));
        Assert.Equal(HttpStatusCode.OK, await GetAssetAsync(audio, otherCookie, candidateCookie));
        Assert.Equal(HttpStatusCode.NotFound, await GetAssetAsync(audio, otherCookie));
        Assert.Equal(HttpStatusCode.OK, await GetAssetAsync(ownedByAuthor, authorCookie, candidateCookie));
        Assert.Equal(HttpStatusCode.NotFound, await GetAssetAsync(ownedByAuthor, otherCookie, candidateCookie));
    }

    [Fact]
    public async Task DeactivatingAnAdministrator_CancelsTheirPendingRequests()
    {
        api.SkipIfUnavailable();
        const string AdminPassword = "Leaving#Admin2026";
        var leaving = await api.CreateStaffAsync(UserRole.Admin, AdminPassword);
        var (paperId, _) = await api.CreatePublishedPapersAsync();
        using var leavingClient = await api.StaffClientAsync(leaving.Username, AdminPassword);
        using var admin = await api.AdminClientAsync();
        using var other = await api.SecondAdminClientAsync();
        var pending = await ApiFixture.RequestIdOfAsync(await leavingClient.DeleteAsync($"/api/admin/papers/{paperId}", ct));

        var change = await admin.PutAsJsonAsync($"/api/admin/users/{leaving.Id}", new { fullName = leaving.FullName, isActive = false }, ct);
        await ApiFixture.ApproveAsync(other, await ApiFixture.RequestIdOfAsync(change));
        var late = await other.PostAsync($"/api/staff/approvals/{pending}/approve", null, ct);

        using var scope = api.Factory.Services.CreateScope();
        var db = Db(scope);
        var request = await db.ApprovalRequests.SingleAsync(r => r.Id == pending, ct);
        Assert.Equal(ApprovalStatus.Cancelled, request.Status);
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.True(await db.TestPapers.AnyAsync(p => p.Id == paperId, ct));
    }

    private sealed class FakeContext(System.Security.Claims.ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId => "test-connection";

        public override string? UserIdentifier => null;

        public override System.Security.Claims.ClaimsPrincipal? User => user;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; } = new Microsoft.AspNetCore.Http.Features.FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }

    private sealed class FakeGroups : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// A WebSocket connection is authenticated once, when it opens; the claims it carries can be hours old. The hub
    /// therefore asks the database whether the account is still active and still has the stamp it signed in with.
    /// </summary>
    [Fact]
    public async Task TheStaffHub_RefusesWatching_OnceTheAccountIsSwitchedOffOrItsStampChanged()
    {
        api.SkipIfUnavailable();
        var exam = await api.CreateCandidateAsync(Password);
        var switchedOff = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        var rotated = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        var steady = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var scope = api.Factory.Services.CreateScope();
        var db = Db(scope);
        var principals = new Dictionary<int, System.Security.Claims.ClaimsPrincipal>();
        foreach (var account in new[] { switchedOff, rotated, steady })
        {
            var loaded = await db.Users.Include(u => u.Areas).AsNoTracking().SingleAsync(u => u.Id == account.Id, ct);
            principals[account.Id] = StaffAuthenticationSetup.CreatePrincipal(loaded);
        }

        await api.RotateSecurityStampAsync(rotated.Id);
        await db.Users.Where(u => u.Id == switchedOff.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), ct);

        async Task<bool> WatchAsync(int ownerId)
        {
            await api.SetExamCreatorAsync(exam.ExamId, ownerId);
            using var hubScope = api.Factory.Services.CreateScope();
            var hub = new StaffRoomHub(Db(hubScope)) { Context = new FakeContext(principals[ownerId]), Groups = new FakeGroups() };
            return await hub.WatchSection(exam.ExamId, exam.ReadingWritingSectionId);
        }

        Assert.False(await WatchAsync(switchedOff.Id));
        Assert.False(await WatchAsync(rotated.Id));
        Assert.True(await WatchAsync(steady.Id));
    }
}
