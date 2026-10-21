using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Api.Admin;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-66 (Victory 20/10): a section with a mix gives each computer the version of its seat — neighbours never share a
/// paper — and an attempt keeps its paper for good, also after a change of seat; marking follows the attempt's paper.
/// </summary>
public sealed class SeatedPapersTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "seat-4x9q";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record Room(ApiFixture.SeededCandidate Exam, int MixId, Dictionary<int, int> VersionPapers, List<(int Id, string Username)> Candidates);

    /// <summary>An exam whose Reading &amp; Writing section delivers a mix, two computers per row, four seated candidates.</summary>
    private async Task<Room> RoomAsync(int seatsPerRow = 2)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var a = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"A {suffix}");
        var b = await api.ImportPublishedSampleAsync(SamplePackages.ReadingWriting, $"B {suffix}");
        using var admin = await api.AdminClientAsync();
        var mix = (await (await admin.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct)).Content.ReadFromJsonAsync<JsonObject>(ct))!;
        var versions = mix["versions"]!.AsArray().ToDictionary(v => (int)v!["version"]!, v => (int)v!["paperId"]!);

        var exam = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var detail = (await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{exam.ExamId}", ct))!;
        var listeningPaper = detail["sections"]!.AsArray().Single(s => (int)s!["sectionTemplateId"]! == 2)!["testPaperId"]!.GetValue<int>();
        var save = await admin.PutAsJsonAsync($"/api/admin/exams/{exam.ExamId}", new SaveExamRequest(
            (string)detail["name"]!, 1, DateOnly.FromDateTime(DateTime.UtcNow), null,
            [new SectionPaper(1, 0, (int)mix["id"]!), new SectionPaper(2, listeningPaper)],
            SeatsPerRow: seatsPerRow), ct);
        save.EnsureSuccessStatusCode();
        // Changing a section's paper replaces the section: read the new ids.
        var sections = (await save.Content.ReadFromJsonAsync<JsonObject>(ct))!["sections"]!.AsArray();
        exam = exam with
        {
            ReadingWritingSectionId = (int)sections.Single(s => (int)s!["sectionTemplateId"]! == 1)!["id"]!,
            ListeningSectionId = (int)sections.Single(s => (int)s!["sectionTemplateId"]! == 2)!["id"]!,
        };

        var candidates = new List<(int, string)> { (exam.CandidateId, exam.Username) };
        for (var i = 0; i < 3; i++)
        {
            candidates.Add(await api.AddCandidateAsync(exam.ExamId, Password));
        }

        for (var seat = 1; seat <= candidates.Count; seat++)
        {
            await api.SetSeatAsync(candidates[seat - 1].Item1, seat.ToString());
        }

        return new Room(exam, (int)mix["id"]!, versions, candidates);
    }

    private async Task<HttpClient> SignInAsync(string username, string pin)
    {
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new { username, password = Password }, ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/candidate/auth/pin", new { pin }, ct)).EnsureSuccessStatusCode();
        return client;
    }

    private async Task<int> AttemptPaperAsync(int candidateId, int sectionId)
    {
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return await db.Attempts.Where(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId).Select(a => a.TestPaperId).SingleAsync(ct);
    }

    private static string PartTitles(JsonObject paper) => string.Join("|", paper["parts"]!.AsArray().Select(p => (string?)p!["title"]));

    [Fact]
    public async Task FourComputersInTwoRows_GetTheFourVersions_AndTheSectionShowsTheMix()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var papers = new List<string>();

        foreach (var (id, username) in room.Candidates)
        {
            using var client = await SignInAsync(username, room.Exam.SessionPin);
            var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{room.Exam.ReadingWritingSectionId}/paper", ct))!;
            (await client.PostAsync($"/api/candidate/sections/{room.Exam.ReadingWritingSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
            papers.Add(PartTitles(paper));
            Assert.Equal(room.VersionPapers[room.Candidates.IndexOf((id, username)) + 1], await AttemptPaperAsync(id, room.Exam.ReadingWritingSectionId));
        }

        Assert.Equal(4, papers.Distinct().Count());
        using var admin = await api.AdminClientAsync();
        var detail = (await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{room.Exam.ExamId}", ct))!;
        var section = detail["sections"]!.AsArray().Single(s => (int)s!["sectionTemplateId"]! == 1)!;
        Assert.Equal(room.MixId, (int)section["paperMixId"]!);
        Assert.NotNull((string?)section["paperMixTitle"]);
        Assert.Equal(2, (int)detail["seatsPerRow"]!);
    }

    [Fact]
    public async Task ACandidateWhoChangesSeat_KeepsThePaperTheyStarted()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var (id, username) = room.Candidates[0];
        using var client = await SignInAsync(username, room.Exam.SessionPin);
        var sectionId = room.Exam.ReadingWritingSectionId;
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();
        var before = PartTitles((await client.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/paper", ct))!);

        // The invigilator moves the candidate to computer 4 (another version) during the test.
        await api.SetSeatAsync(id, "4");
        using var otherComputer = await SignInAsync(username, room.Exam.SessionPin);
        var after = PartTitles((await otherComputer.GetFromJsonAsync<JsonObject>($"/api/candidate/sections/{sectionId}/paper", ct))!);
        var resumed = await otherComputer.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct);

        Assert.Equal(before, after);
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        Assert.Equal(room.VersionPapers[1], await AttemptPaperAsync(id, sectionId));
    }

    [Fact]
    public async Task AnswersAreSavedAndMarked_AgainstTheCandidatesOwnVersion()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        var (id, username) = room.Candidates[2];
        using var client = await SignInAsync(username, room.Exam.SessionPin);
        var sectionId = room.Exam.ReadingWritingSectionId;
        (await client.PostAsync($"/api/candidate/sections/{sectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        var saved = await client.PutAsJsonAsync($"/api/candidate/sections/{sectionId}/responses", new { responses = new[] { new { questionNumber = 1, value = "A" } } }, ct);

        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var response = await db.Responses.Include(r => r.Question!).ThenInclude(q => q.QuestionGroup!).ThenInclude(g => g.PaperPart)
            .SingleAsync(r => r.Attempt!.CandidateId == id && r.Attempt.ExamSectionId == sectionId, ct);
        Assert.Equal(room.VersionPapers[3], response.Question!.QuestionGroup!.PaperPart!.TestPaperId);
    }

    [Fact]
    public async Task WithoutSeatNumbers_TheCandidateListOrderGivesTheSeats()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync(seatsPerRow: 4);
        foreach (var (id, _) in room.Candidates)
        {
            await api.SetSeatAsync(id, null);
        }

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var order = await db.Candidates.Where(c => c.ExamId == room.Exam.ExamId).OrderBy(c => c.CandidateNumber).ThenBy(c => c.Id).Select(c => new { c.Id }).ToListAsync(ct);
        var (firstId, firstUsername) = room.Candidates.Single(c => c.Id == order[1].Id);
        using var client = await SignInAsync(firstUsername, room.Exam.SessionPin);
        (await client.PostAsync($"/api/candidate/sections/{room.Exam.ReadingWritingSectionId}/attempt", null, ct)).EnsureSuccessStatusCode();

        // Second in the list = seat 2 = version 2 with four computers to a row.
        Assert.Equal(room.VersionPapers[2], await AttemptPaperAsync(firstId, room.Exam.ReadingWritingSectionId));
    }

    [Fact]
    public async Task AMixForAnotherSection_OrABadRowLength_IsRefused()
    {
        api.SkipIfUnavailable();
        var room = await RoomAsync();
        using var admin = await api.AdminClientAsync();
        var detail = (await admin.GetFromJsonAsync<JsonObject>($"/api/staff/exams/{room.Exam.ExamId}", ct))!;

        var wrongSection = await admin.PutAsJsonAsync($"/api/admin/exams/{room.Exam.ExamId}", new SaveExamRequest(
            (string)detail["name"]!, 1, DateOnly.FromDateTime(DateTime.UtcNow), null,
            [new SectionPaper(1, 0, room.MixId), new SectionPaper(2, 0, room.MixId)]), ct);
        var badRow = await admin.PutAsJsonAsync($"/api/admin/exams/{room.Exam.ExamId}", new SaveExamRequest(
            (string)detail["name"]!, 1, DateOnly.FromDateTime(DateTime.UtcNow), null,
            [new SectionPaper(1, 0, room.MixId), new SectionPaper(2, (int)detail["sections"]![1]!["testPaperId"]!)], SeatsPerRow: 51), ct);

        Assert.Equal(HttpStatusCode.BadRequest, wrongSection.StatusCode);
        Assert.Contains("is not a mix of Listening papers", await wrongSection.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, badRow.StatusCode);
        Assert.Contains("Computers per row", await badRow.Content.ReadAsStringAsync(ct));
    }
}
