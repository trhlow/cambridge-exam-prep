using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Tests.Infrastructure.Content;

namespace ExamPrep.Tests.Api.Admin;

/// <summary>C5 (review 02/11): the lists a manager of exams picks from carry what the exam form uses and nothing more.</summary>
public sealed class ExamChoicesTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Choices#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExamPapers_AndMixes_ShowOnlyWhatTheExamFormUses()
    {
        api.SkipIfUnavailable();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var a = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"Choice A {suffix}");
        var b = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"Choice B {suffix}");
        using var admin = await api.AdminClientAsync();
        var mix = (await (await admin.PostAsJsonAsync("/api/admin/paper-mixes", new { sourcePaperIds = new[] { a, b }, versionCount = 4, title = $"Choice mix {suffix}" }, ct)).Content.ReadFromJsonAsync<JsonObject>(ct))!;
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var client = await api.StaffClientAsync(manager.Username, Password);

        var papers = (await client.GetFromJsonAsync<JsonArray>("/api/admin/exam-papers", ct))!;
        var mixes = (await client.GetFromJsonAsync<JsonArray>("/api/admin/exam-paper-mixes", ct))!;

        var paper = papers.Single(p => (int)p!["id"]! == a)!.AsObject();
        Assert.Equal(["id", "paperMixId", "section", "sectionTemplateId", "status", "title"], paper.Select(x => x.Key).Order());
        var version = papers.First(p => (int?)p!["paperMixId"] == (int)mix["id"]!)!.AsObject();
        Assert.Equal((int)mix["id"]!, (int)version["paperMixId"]!);
        var shown = mixes.Single(m => (int)m!["id"]! == (int)mix["id"]!)!.AsObject();
        Assert.Equal(["id", "section", "sectionTemplateId", "title", "versionCount"], shown.Select(x => x.Key).Order());
        Assert.Equal(4, (int)shown["versionCount"]!);
    }

    [Fact]
    public async Task AssignableStaff_AreTheActiveAccounts_PlusInactiveOnesAlreadyAssigned()
    {
        api.SkipIfUnavailable();
        var candidate = await api.CreateCandidateAsync(Password);
        var active = await api.CreateStaffAsync(UserRole.Invigilator, Password);
        var retired = await api.CreateStaffAsync(UserRole.Invigilator, Password, isActive: false);
        var retiredButAssigned = await api.CreateStaffAsync(UserRole.Examiner, Password, isActive: false);
        await api.AssignExaminerAsync(candidate.ExamId, retiredButAssigned.Id);
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var client = await api.StaffClientAsync(manager.Username, Password);

        var staff = (await client.GetFromJsonAsync<JsonArray>("/api/admin/assignable-staff", ct))!;
        var forExam = (await client.GetFromJsonAsync<JsonArray>($"/api/admin/assignable-staff?examId={candidate.ExamId}", ct))!;

        var ids = staff.Select(u => (int)u!["id"]!).ToList();
        Assert.Contains(active.Id, ids);
        Assert.DoesNotContain(retired.Id, ids);
        Assert.Contains(retiredButAssigned.Id, ids);
        Assert.Contains(retiredButAssigned.Id, forExam.Select(u => (int)u!["id"]!));
        Assert.DoesNotContain(retired.Id, forExam.Select(u => (int)u!["id"]!));
    }
}
