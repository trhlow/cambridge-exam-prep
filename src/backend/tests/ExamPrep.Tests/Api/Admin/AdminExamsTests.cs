using System.Net;
using System.Net.Http.Json;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>
/// VE-21: an administrator schedules an exam session (Inspera "parent test") with one published paper per section,
/// a session PIN and an invigilator password; VE-23: invigilators only see the sessions they are assigned to.
/// </summary>
public sealed class AdminExamsTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string StaffPassword = "kq7m-2xpt-9";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    public sealed record SectionPaper(int SectionTemplateId, int TestPaperId);

    public sealed record SaveExamRequest(string Name, int ExamTypeId, DateOnly ScheduledDate, string? Room, List<SectionPaper> Papers, string? SessionPin = null, string? InvigilatorPassword = null);

    public sealed record ExamSectionView(int Id, int SectionTemplateId, int OrderNo, string Title, int DurationMinutes, int TestPaperId, string PaperTitle);

    public sealed record InvigilatorView(int Id, string Username, string FullName);

    public sealed record ExamDetail(int Id, string Name, int ExamTypeId, string ExamType, DateOnly ScheduledDate, string? Room, string Status, string SessionPin, string InvigilatorPassword, int Candidates, bool HasStarted, List<ExamSectionView> Sections, List<InvigilatorView> Invigilators);

    public sealed record ExamListItem(int Id, string Name, string ExamType, DateOnly ScheduledDate, string? Room, string Status, int Candidates);

    public sealed record SectionTemplateView(int Id, int OrderNo, string Title, int DurationMinutes);

    public sealed record ExamTypeView(int Id, string Code, string Name, List<SectionTemplateView> Sections);

    public sealed record PaperListItem(int Id, string Title, string ExamType, int SectionTemplateId, string Section, string Status, int Parts, int Questions, DateTime CreatedAt, int UsedByExams);

    private static SaveExamRequest A2Exam((int ReadingWritingId, int ListeningId) papers, string? name = null) =>
        new(name ?? $"A2 {Guid.NewGuid():N}"[..20], 1, new DateOnly(2026, 11, 20), "Room 1", [new(1, papers.ReadingWritingId), new(2, papers.ListeningId)]);

    private async Task<ExamDetail> CreateExamAsync(HttpClient admin, SaveExamRequest request)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/exams", request, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExamDetail>(ct))!;
    }

    [Fact]
    public async Task ExamTypes_ListTheA2SectionsInOrder()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();

        var types = await admin.GetFromJsonAsync<List<ExamTypeView>>("/api/admin/exam-types", ct);

        var a2 = types!.Single(t => t.Code == "A2_KEY_FS");
        Assert.Equal(["Reading & Writing", "Listening"], a2.Sections.Select(s => s.Title));
        Assert.Equal([60, 30], a2.Sections.Select(s => s.DurationMinutes));
    }

    [Fact]
    public async Task Create_SchedulesTheSessionWithAPinAndAnInvigilatorPassword()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var papers = await api.CreatePublishedPapersAsync();

        var exam = await CreateExamAsync(admin, A2Exam(papers, "A2 Key fS 20/11"));

        Assert.Equal("A2 Key fS 20/11", exam.Name);
        Assert.Equal("Draft", exam.Status);
        Assert.Matches("^[0-9]{4}$", exam.SessionPin);
        Assert.True(exam.InvigilatorPassword.Length >= 8);
        Assert.Equal(["Reading & Writing", "Listening"], exam.Sections.Select(s => s.Title));
        Assert.Equal([papers.ReadingWritingId, papers.ListeningId], exam.Sections.Select(s => s.TestPaperId));
        Assert.Equal([60, 30], exam.Sections.Select(s => s.DurationMinutes));
    }

    [Fact]
    public async Task Create_AcceptsTheAdministratorsOwnPinAndPassword()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var papers = await api.CreatePublishedPapersAsync();

        var exam = await CreateExamAsync(admin, A2Exam(papers) with { SessionPin = "735190", InvigilatorPassword = "room-one-pass" });

        Assert.Equal("735190", exam.SessionPin);
        Assert.Equal("room-one-pass", exam.InvigilatorPassword);
    }

    [Fact]
    public async Task Create_RefusesADraftPaperAPaperForAnotherSectionOrAMissingSection()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var papers = await api.CreatePublishedPapersAsync();
        var draft = await api.CreateCandidateAsync("x-pass-1234"); // its papers are drafts
        var draftExam = await admin.GetFromJsonAsync<ExamDetail>($"/api/staff/exams/{draft.ExamId}", ct);
        var draftPaperId = draftExam!.Sections.Single(s => s.SectionTemplateId == 1).TestPaperId;

        var withDraft = await admin.PostAsJsonAsync("/api/admin/exams", A2Exam((draftPaperId, papers.ListeningId)), ct);
        var swapped = await admin.PostAsJsonAsync("/api/admin/exams", A2Exam((papers.ListeningId, papers.ReadingWritingId)), ct);
        var missing = await admin.PostAsJsonAsync("/api/admin/exams", A2Exam(papers) with { Papers = [new(1, papers.ReadingWritingId)] }, ct);
        var badPin = await admin.PostAsJsonAsync("/api/admin/exams", A2Exam(papers) with { SessionPin = "12a" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, withDraft.StatusCode);
        Assert.Contains("not published", await withDraft.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, swapped.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains("Listening", await missing.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, badPin.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_AreClosedToInvigilators()
    {
        api.SkipIfUnavailable();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, StaffPassword);
        using var client = await api.StaffClientAsync(invigilator.Username, StaffPassword);
        var papers = await api.CreatePublishedPapersAsync();

        var create = await client.PostAsJsonAsync("/api/admin/exams", A2Exam(papers), ct);
        var list = await client.GetAsync("/api/admin/papers", ct);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Invigilators_SeeOnlyTheSessionsTheyAreAssignedTo()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, StaffPassword);
        var assigned = await CreateExamAsync(admin, A2Exam(await api.CreatePublishedPapersAsync()));
        var other = await CreateExamAsync(admin, A2Exam(await api.CreatePublishedPapersAsync()));

        var assign = await admin.PutAsJsonAsync($"/api/admin/exams/{assigned.Id}/invigilators", new { userIds = new[] { invigilator.Id } }, ct);
        using var client = await api.StaffClientAsync(invigilator.Username, StaffPassword);
        var visible = await client.GetFromJsonAsync<List<ExamListItem>>("/api/staff/exams", ct);
        var detail = await client.GetFromJsonAsync<ExamDetail>($"/api/staff/exams/{assigned.Id}", ct);
        var hidden = await client.GetAsync($"/api/staff/exams/{other.Id}", ct);

        assign.EnsureSuccessStatusCode();
        Assert.Equal([assigned.Id], visible!.Select(e => e.Id));
        Assert.Equal(assigned.InvigilatorPassword, detail!.InvigilatorPassword);
        Assert.Equal([invigilator.Id], detail.Invigilators.Select(i => i.Id));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task AssignInvigilators_RefusesAdministratorsAndUnknownAccounts()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var otherAdmin = await api.CreateStaffAsync(UserRole.Admin, StaffPassword);
        var exam = await CreateExamAsync(admin, A2Exam(await api.CreatePublishedPapersAsync()));

        var response = await admin.PutAsJsonAsync($"/api/admin/exams/{exam.Id}/invigilators", new { userIds = new[] { otherAdmin.Id, 999_999 } }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesDetailsButNotThePapersOnceCandidatesHaveStarted()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var seeded = await api.CreateCandidateAsync("x-pass-1234");
        var current = (await admin.GetFromJsonAsync<ExamDetail>($"/api/staff/exams/{seeded.ExamId}", ct))!;
        await api.StartAttemptAsync(seeded.CandidateId, seeded.ReadingWritingSectionId);
        var request = new SaveExamRequest("Renamed", 1, current.ScheduledDate, "Room 2", current.Sections.Select(s => new SectionPaper(s.SectionTemplateId, s.TestPaperId)).ToList());

        var rename = await admin.PutAsJsonAsync($"/api/admin/exams/{seeded.ExamId}", request, ct);
        var newPapers = await api.CreatePublishedPapersAsync();
        var swap = await admin.PutAsJsonAsync($"/api/admin/exams/{seeded.ExamId}", A2Exam(newPapers, "Renamed"), ct);
        var delete = await admin.DeleteAsync($"/api/admin/exams/{seeded.ExamId}", ct);

        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        var renamed = await rename.Content.ReadFromJsonAsync<ExamDetail>(ct);
        Assert.Equal("Renamed", renamed!.Name);
        Assert.Equal("Room 2", renamed.Room);
        Assert.True(renamed.HasStarted);
        Assert.Equal(HttpStatusCode.Conflict, swap.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task SetStatus_OpensTheSessionAndDeleteRemovesAnUnusedOne()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var exam = await CreateExamAsync(admin, A2Exam(await api.CreatePublishedPapersAsync()));

        var open = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.Id}/status", new { status = "Active" }, ct);
        var bogus = await admin.PostAsJsonAsync($"/api/admin/exams/{exam.Id}/status", new { status = "Exploded" }, ct);
        var delete = await admin.DeleteAsync($"/api/admin/exams/{exam.Id}", ct);
        var gone = await admin.GetAsync($"/api/staff/exams/{exam.Id}", ct);

        Assert.Equal("Active", (await open.Content.ReadFromJsonAsync<ExamDetail>(ct))!.Status);
        Assert.Equal(HttpStatusCode.BadRequest, bogus.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Papers_ArePublishedBeforeUseAndCannotBeDeletedWhileAnExamUsesThem()
    {
        api.SkipIfUnavailable();
        using var admin = await api.AdminClientAsync();
        var seeded = await api.CreateCandidateAsync("x-pass-1234");
        var exam = (await admin.GetFromJsonAsync<ExamDetail>($"/api/staff/exams/{seeded.ExamId}", ct))!;
        var usedPaperId = exam.Sections[0].TestPaperId;
        var unused = await api.CreatePublishedPapersAsync();

        var publish = await admin.PostAsync($"/api/admin/papers/{usedPaperId}/publish", null, ct);
        var papers = await admin.GetFromJsonAsync<List<PaperListItem>>("/api/admin/papers", ct);
        var deleteUsed = await admin.DeleteAsync($"/api/admin/papers/{usedPaperId}", ct);
        var deleteUnused = await admin.DeleteAsync($"/api/admin/papers/{unused.ListeningId}", ct);

        Assert.Equal("Published", (await publish.Content.ReadFromJsonAsync<PaperListItem>(ct))!.Status);
        var used = papers!.Single(p => p.Id == usedPaperId);
        Assert.Equal(1, used.UsedByExams);
        Assert.Equal("Reading & Writing", used.Section);
        Assert.Equal(HttpStatusCode.Conflict, deleteUsed.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteUnused.StatusCode);
    }
}
