using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Tests.Infrastructure.Content;

namespace ExamPrep.Tests.Api.Access;

/// <summary>
/// VE-72 (Victory 29/10): several people work in each area, and each sees only their own work. A papers manager sees
/// the papers and mixes they made (and their pictures and recordings); an exams manager the sessions they created; a
/// candidates manager the sessions they are assigned to. Administrators see everything. Anything else answers 404.
/// </summary>
public sealed class OwnershipTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Owner#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private async Task<HttpClient> ManagerAsync(params ManagerArea[] areas)
    {
        var user = await api.CreateManagerAsync(Password, areas);
        return await api.StaffClientAsync(user.Username, Password);
    }

    private async Task<int> ImportAsync(HttpClient client, string sample)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(SamplePackages.Zip(sample));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(file, "package", "paper.zip");
        var response = await client.PostAsync("/api/admin/papers/import", content, ct);
        response.EnsureSuccessStatusCode();
        var id = (int)(await response.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
        (await client.PostAsync($"/api/admin/papers/{id}/publish", null, ct)).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<int> CreateExamAsync(HttpClient client, int readingWritingId, int listeningId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/admin/exams", new
        {
            name,
            examTypeId = 1,
            scheduledDate = "2026-12-01",
            papers = new[] { new { sectionTemplateId = 1, testPaperId = readingWritingId }, new { sectionTemplateId = 2, testPaperId = listeningId } },
        }, ct);
        response.EnsureSuccessStatusCode();
        return (int)(await response.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
    }

    private static async Task<List<int>> IdsAsync(HttpClient client, string url) =>
        (await client.GetFromJsonAsync<JsonArray>(url))!.Select(x => (int)x!["id"]!).ToList();

    [Fact]
    public async Task PapersManagers_SeeAndChangeOnlyTheirOwnPapers()
    {
        api.SkipIfUnavailable();
        using var first = await ManagerAsync(ManagerArea.Papers);
        using var second = await ManagerAsync(ManagerArea.Papers);
        using var admin = await api.AdminClientAsync();
        var mine = await ImportAsync(first, SamplePackages.ReadingWriting);
        var theirs = await ImportAsync(second, SamplePackages.ReadingWriting);

        var listed = await IdsAsync(first, "/api/admin/papers");
        var detail = await first.GetAsync($"/api/admin/papers/{theirs}", ct);
        var edit = await first.PutAsJsonAsync($"/api/admin/papers/{theirs}", new { title = "Taken over" }, ct);
        var delete = await first.DeleteAsync($"/api/admin/papers/{theirs}", ct);

        Assert.Contains(mine, listed);
        Assert.DoesNotContain(theirs, listed);
        Assert.All([detail, edit, delete], r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Contains(theirs, await IdsAsync(admin, "/api/admin/papers"));
    }

    [Fact]
    public async Task PapersManagers_MixOnlyTheirOwnPapers_AndSeeOnlyTheirOwnMixes()
    {
        api.SkipIfUnavailable();
        using var first = await ManagerAsync(ManagerArea.Papers);
        using var second = await ManagerAsync(ManagerArea.Papers);
        var a = await ImportAsync(first, SamplePackages.ReadingWriting);
        var b = await ImportAsync(first, SamplePackages.ReadingWriting);
        var theirs = await ImportAsync(second, SamplePackages.ReadingWriting);

        var withTheirs = await first.PostAsJsonAsync("/api/admin/paper-mixes", new { sourcePaperIds = new[] { a, theirs }, versionCount = 4 }, ct);
        var own = await first.PostAsJsonAsync("/api/admin/paper-mixes", new { sourcePaperIds = new[] { a, b }, versionCount = 4 }, ct);
        var mixId = (int)(await own.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;

        Assert.Equal(HttpStatusCode.BadRequest, withTheirs.StatusCode);
        Assert.Contains("Choose papers that exist.", await withTheirs.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
        Assert.DoesNotContain(mixId, await IdsAsync(second, "/api/admin/paper-mixes"));
        Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync($"/api/admin/paper-mixes/{mixId}", ct)).StatusCode);
    }

    [Fact]
    public async Task ThePicturesAndRecordingsOfAPaper_AreForItsAuthorOnly()
    {
        api.SkipIfUnavailable();
        using var first = await ManagerAsync(ManagerArea.Papers);
        using var second = await ManagerAsync(ManagerArea.Papers);
        using var admin = await api.AdminClientAsync();
        var paperId = await ImportAsync(first, SamplePackages.Listening);
        var paper = (await first.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{paperId}", ct))!;
        var audio = (string)paper["parts"]![0]!["audioUrl"]!;

        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync(audio, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(audio, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync(audio, ct)).StatusCode);
    }

    [Fact]
    public async Task ExamsManagers_SeeOnlyTheSessionsTheyCreated_ButChooseAnyPublishedPaperByTitle()
    {
        api.SkipIfUnavailable();
        using var author = await ManagerAsync(ManagerArea.Papers);
        using var first = await ManagerAsync(ManagerArea.Exams);
        using var second = await ManagerAsync(ManagerArea.Exams);
        var readingWriting = await ImportAsync(author, SamplePackages.ReadingWriting);
        var listening = await ImportAsync(author, SamplePackages.Listening);

        var choices = await IdsAsync(first, "/api/admin/exam-papers");
        var mine = await CreateExamAsync(first, readingWriting, listening, "Mine");
        var theirs = await CreateExamAsync(second, readingWriting, listening, "Theirs");
        var listed = await IdsAsync(first, "/api/staff/exams");
        var detail = await first.GetAsync($"/api/staff/exams/{theirs}", ct);
        var status = await first.PostAsJsonAsync($"/api/admin/exams/{theirs}/status", new { status = "Active" }, ct);
        var invigilators = await first.PutAsJsonAsync($"/api/admin/exams/{theirs}/invigilators", new { userIds = Array.Empty<int>() }, ct);
        var paperContent = await first.GetAsync($"/api/admin/papers/{readingWriting}", ct);

        Assert.Contains(readingWriting, choices);
        Assert.Contains(mine, listed);
        Assert.DoesNotContain(theirs, listed);
        Assert.All([detail, status, invigilators], r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Equal(HttpStatusCode.Forbidden, paperContent.StatusCode);
    }

    [Fact]
    public async Task CandidatesManagers_WorkOnlyInTheSessionsTheyAreAssignedTo()
    {
        api.SkipIfUnavailable();
        var (readingWriting, listening) = await api.CreatePublishedPapersAsync();
        using var organiser = await ManagerAsync(ManagerArea.Exams);
        var candidatesManager = await api.CreateManagerAsync(Password, ManagerArea.Candidates);
        using var candidates = await api.StaffClientAsync(candidatesManager.Username, Password);
        var examId = await CreateExamAsync(organiser, readingWriting, listening, "Assigned later");
        var entry = new { candidates = new[] { new { candidateNumber = "0001", firstName = "An", lastName = "Nguyen" } } };

        var before = await candidates.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", entry, ct);
        var listedBefore = await IdsAsync(candidates, "/api/staff/exams");
        var assigned = await organiser.PutAsJsonAsync($"/api/admin/exams/{examId}/candidate-managers", new { userIds = new[] { candidatesManager.Id } }, ct);
        var after = await candidates.PostAsJsonAsync($"/api/admin/exams/{examId}/candidates", entry, ct);
        var detail = (await organiser.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{examId}", ct))!;

        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);
        Assert.DoesNotContain(examId, listedBefore);
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(HttpStatusCode.Created, after.StatusCode);
        Assert.Contains(examId, await IdsAsync(candidates, "/api/staff/exams"));
        Assert.Equal([candidatesManager.Id], detail["candidateManagers"]!.AsArray().Select(x => (int)x!["id"]!));
        var seenByCandidatesManager = (await candidates.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{examId}", ct))!;
        Assert.Equal((true, false, true), ((bool)detail["canOrganise"]!, (bool)detail["canEnterCandidates"]!, (bool)detail["canMonitor"]!));
        Assert.Equal((false, true, false), ((bool)seenByCandidatesManager["canOrganise"]!, (bool)seenByCandidatesManager["canEnterCandidates"]!, (bool)seenByCandidatesManager["canMonitor"]!));
    }

    [Fact]
    public async Task OnlyCandidatesManagers_CanBeAssignedToEnterCandidates()
    {
        api.SkipIfUnavailable();
        var (readingWriting, listening) = await api.CreatePublishedPapersAsync();
        using var organiser = await ManagerAsync(ManagerArea.Exams);
        var papersManager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var examId = await CreateExamAsync(organiser, readingWriting, listening, "Wrong person");

        var response = await organiser.PutAsJsonAsync($"/api/admin/exams/{examId}/candidate-managers", new { userIds = new[] { papersManager.Id } }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("is not an active manager of candidates", await response.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task ASessionSays_HowManyInvigilatorsCambridgeAsksFor()
    {
        api.SkipIfUnavailable();
        var seeded = await api.CreateCandidateAsync(Password);
        for (var i = 0; i < 25; i++)
        {
            await api.AddCandidateAsync(seeded.ExamId, Password, $"Name{i}", "Tran");
        }

        using var admin = await api.AdminClientAsync();
        var detail = (await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{seeded.ExamId}", ct))!;

        // 26 candidates: one invigilator for every 25 candidates in the room (Exam Day Booklet 2026).
        Assert.Equal(2, (int)detail["invigilatorsNeeded"]!);
    }
}
