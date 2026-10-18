using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Tests.Infrastructure.Content;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// VE-25: a converted draft is fixed on the web — wording, option texts, answers, gaps, recordings — under the same
/// rules as the importer; published papers are left alone.
/// </summary>
public sealed class PaperEditorTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Editor#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, int PaperId)> DraftAsync(string sample, HttpClient? client = null)
    {
        client ??= await api.AdminClientAsync();
        var file = new ByteArrayContent(SamplePackages.Zip(sample));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var import = await client.PostAsync("/api/admin/papers/import", new MultipartFormDataContent { { file, "package", "paper.zip" } }, ct);
        import.EnsureSuccessStatusCode();
        return (client, (int)(await import.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!);
    }

    private static JsonObject Question(JsonObject paper, int number) =>
        paper["parts"]!.AsArray().SelectMany(p => p!["groups"]!.AsArray()).SelectMany(g => g!["questions"]!.AsArray()).Single(q => (int)q!["number"]! == number)!.AsObject();

    private static object Options(params (string Key, string Text)[] options) => options.Select(o => new { key = o.Key, text = o.Text }).ToArray();

    [Fact]
    public async Task AManagerOfPapers_FixesAChoiceQuestion_AndTheHtmlIsSanitised()
    {
        api.SkipIfUnavailable();
        var manager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var client = await api.StaffClientAsync(manager.Username, Password);
        var (_, paperId) = await DraftAsync(SamplePackages.ReadingWriting, client);

        var response = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1", new
        {
            promptHtml = "<p>What does the notice say?</p><script>alert(1)</script>",
            options = Options(("A", "Closed on Monday"), ("B", "Open late"), ("C", "Free entry")),
            answers = new[] { "C" },
            points = 1,
        }, ct);
        var paper = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        var question = Question(paper!, 1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("<p>What does the notice say?</p>", (string)question["promptHtml"]!);
        Assert.Equal("Free entry", (string)question["options"]![2]!["text"]!);
        Assert.Equal(["C"], question["answers"]!.AsArray().Select(a => (string)a!));
    }

    [Fact]
    public async Task TheImportersRules_StillHold()
    {
        api.SkipIfUnavailable();
        var (client, paperId) = await DraftAsync(SamplePackages.ReadingWriting);

        var unknownAnswer = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1", new { answers = new[] { "D" }, points = 1 }, ct);
        var twoAnswers = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/2", new { answers = new[] { "A", "B" }, points = 1 }, ct);
        var renamedOption = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/3", new { options = Options(("A", "x"), ("B", "y"), ("Z", "z")), answers = new[] { "A" }, points = 1 }, ct);
        var writingWithAnswer = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/31", new { answers = new[] { "anything" }, points = 10, minWords = 25 }, ct);
        var blankTypedAnswer = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/25", new { answers = new[] { " " }, points = 1 }, ct);
        var gapRemoved = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/parts/5/groups/1", new { instructions = "Fill the gaps.", bodyHtml = "<p>{{q:25}} {{q:26}} {{q:27}} {{q:28}} {{q:29}}</p>" }, ct);

        Assert.All([unknownAnswer, twoAnswers, renamedOption, writingWithAnswer, blankTypedAnswer, gapRemoved], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Contains("{{q:30}} is missing", await gapRemoved.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task TypedAnswers_PartPassages_AndTheTitle_CanBeFixed()
    {
        api.SkipIfUnavailable();
        var (client, paperId) = await DraftAsync(SamplePackages.ReadingWriting);

        (await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/25", new { answers = new[] { "for", "For" }, points = 1 }, ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/parts/1", new { title = "Part 1 — Notices", stimulusHtml = "<p>Read the notices.</p>" }, ct)).EnsureSuccessStatusCode();
        var titled = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}", new { title = "A2 R&W — checked" }, ct);
        var paper = await titled.Content.ReadFromJsonAsync<JsonObject>(ct);

        Assert.Equal("A2 R&W — checked", (string)paper!["title"]!);
        Assert.Equal("Part 1 — Notices", (string)paper["parts"]![0]!["title"]!);
        Assert.Equal(["for", "For"], Question(paper, 25)["answers"]!.AsArray().Select(a => (string)a!));
    }

    [Fact]
    public async Task AListeningRecording_CanBeReplaced()
    {
        api.SkipIfUnavailable();
        var (client, paperId) = await DraftAsync(SamplePackages.Listening);
        var mp3 = new byte[2048];
        new Random(25).NextBytes(mp3);
        var file = new ByteArrayContent(mp3);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        var response = await client.PostAsync($"/api/admin/papers/{paperId}/parts/2/audio", new MultipartFormDataContent { { file, "audio", "part2.mp3" } }, ct);
        var paper = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        var served = await client.GetByteArrayAsync((string)paper!["parts"]![1]!["audioUrl"]!, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mp3, served);
    }

    [Fact]
    public async Task PublishedPapers_AndOtherStaff_AreRefused()
    {
        api.SkipIfUnavailable();
        var (admin, paperId) = await DraftAsync(SamplePackages.ReadingWriting);
        var examsManager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var exams = await api.StaffClientAsync(examsManager.Username, Password);

        var forbidden = await exams.PutAsJsonAsync($"/api/admin/papers/{paperId}", new { title = "x" }, ct);
        (await admin.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct)).EnsureSuccessStatusCode();
        var published = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}", new { title = "x" }, ct);
        var missing = await admin.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/999", new { answers = new[] { "A" }, points = 1 }, ct);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, published.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
    }
}
