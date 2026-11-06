using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFklEQVR4nGP8z8DAwMDAxMDAwMDAAAANHQEDasKb6QAAAABJRU5ErkJggg==");

    private static MultipartFormDataContent Picture(byte[] content, string name = "picture.png")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { file, "image", name } };
    }

    private async Task<int> BlankAsync(HttpClient client, int sectionTemplateId, string title)
    {
        var created = await client.PostAsJsonAsync("/api/admin/papers/blank", new { sectionTemplateId, title }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (int)(await created.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
    }

    [Fact]
    public async Task ABlankPaper_HasTheExamLayout_AndCannotBePublishedUntilWritten()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var readingWriting = await BlankAsync(client, 1, "A2 R&W — written on the web");
        var listening = await BlankAsync(client, 2, "A2 Listening — written on the web");

        var rw = await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{readingWriting}", ct);
        var l = await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{listening}", ct);
        var publish = await client.PostAsync($"/api/admin/papers/{listening}/publish", null, ct);
        var errors = (await publish.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();

        int Questions(JsonObject paper) => paper["parts"]!.AsArray().SelectMany(p => p!["groups"]!.AsArray()).Sum(g => g!["questions"]!.AsArray().Count);
        Assert.Equal((7, 32), (rw!["parts"]!.AsArray().Count, Questions(rw)));
        Assert.Equal((5, 25), (l!["parts"]!.AsArray().Count, Questions(l)));
        Assert.Equal("Draft", (string)l["status"]!);
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
        Assert.Contains("Part 1: the recording is still the placeholder.", errors);
        Assert.Contains("Question 1: the wording, options or answer are still to write.", errors);
        // A6: a blank paper starts with no answer at all (choice, card or typed), and publishing names each one.
        Assert.Empty(Question(l, 1)["answers"]!.AsArray());
        Assert.Empty(Question(l, 7)["answers"]!.AsArray());
        Assert.Empty(Question(l, 21)["answers"]!.AsArray());
        Assert.Contains("Question 1: the answer is not set.", errors);
    }

    [Fact]
    public async Task PublishingABlankPaper_NeedsEveryAnswerChosen()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var paperId = await BlankAsync(client, 5, "B1 Listening — answers");
        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{paperId}", ct))!;
        var unset = paper["parts"]!.AsArray().SelectMany(p => p!["groups"]!.AsArray()).SelectMany(g => g!["questions"]!.AsArray())
            .Where(q => q!["answers"]!.AsArray().Count == 0).Select(q => (int)q!["number"]!).ToList();

        Assert.Equal(Enumerable.Range(1, 25), unset);
        // Setting the answer through the editor is all it takes to clear that question's refusal.
        var set = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1", new
        {
            promptHtml = "<p>Where?</p>",
            options = Options(("A", "the museum"), ("B", "the park"), ("C", "the zoo")),
            answers = new[] { "B" },
            points = 1,
        }, ct);
        var publish = await client.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct);
        var errors = (await publish.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();

        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.DoesNotContain("Question 1: the answer is not set.", errors);
        Assert.Contains("Question 2: the answer is not set.", errors);
    }

    [Fact]
    public async Task TheGroupEditor_WritesTheColumnHeadingsAndCardTitles_SoABlankB1ReadingCanBePublished()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var paperId = await BlankAsync(client, 3, "B1 Reading — headings");

        var before = await client.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct);
        var cards = "ABCDEFGH".Select(k => new { key = k.ToString(), text = $"Text {k}", title = $"Title {k}" }).ToArray();
        var edited = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/parts/2/groups/1", new
        {
            instructions = "Match each person with the right text.",
            bodyHtml = (string?)null,
            optionPool = cards,
            itemsHeading = "Young people",
            poolHeading = "Texts",
        }, ct);
        var paper = await edited.Content.ReadFromJsonAsync<JsonObject>(ct);
        var group = paper!["parts"]![1]!["groups"]![0]!;
        var after = await client.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct);
        var errors = (await after.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();

        Assert.Contains("Part 2: the instructions, text, headings or cards are still to write.", (await before.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!));
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(("Young people", "Texts"), ((string)group["itemsHeading"]!, (string)group["poolHeading"]!));
        Assert.Equal("Title C", (string)group["optionPool"]![2]!["title"]!);
        Assert.DoesNotContain("Part 2: the instructions, text, headings or cards are still to write.", errors);
    }

    [Theory]
    [InlineData(3, 6, 32, "Question 6: the wording, options or answer are still to write.")]
    [InlineData(4, 2, 3, "Question 2: the wording, options or answer are still to write.")]
    [InlineData(5, 4, 25, "Part 1: the recording is still the placeholder.")]
    public async Task B1Sections_HaveBlankLayoutsToo(int sectionTemplateId, int parts, int questions, string unwritten)
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var layouts = await client.GetFromJsonAsync<JsonArray>("/api/admin/papers/blank-layouts", ct);
        var paperId = await BlankAsync(client, sectionTemplateId, $"B1 — written on the web {sectionTemplateId}");

        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{paperId}", ct))!;
        var publish = await client.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct);
        var errors = (await publish.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();

        Assert.Equal([1, 2, 3, 4, 5], layouts!.Select(l => (int)l!["sectionTemplateId"]!));
        Assert.Equal("B1 Preliminary for Schools", (string)paper["examType"]!);
        Assert.Equal(parts, paper["parts"]!.AsArray().Count);
        Assert.Equal(questions, paper["parts"]!.AsArray().SelectMany(p => p!["groups"]!.AsArray()).Sum(g => g!["questions"]!.AsArray().Count));
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
        Assert.Contains(unwritten, errors);
        if (sectionTemplateId == 3)
        {
            // Reading Part 2: the column headings of the drag-and-drop are still to write too.
            Assert.Contains("Part 2: the instructions, text, headings or cards are still to write.", errors);
        }
    }

    [Fact]
    public async Task WritingAQuestionAndItsPictures_TakesItOffThePublishList()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var paperId = await BlankAsync(client, 2, "A2 Listening — pictures");
        foreach (var key in new[] { "A", "B", "C" })
        {
            (await client.PostAsync($"/api/admin/papers/{paperId}/questions/1/options/{key}/image", Picture(Png, $"{key}.png"), ct)).EnsureSuccessStatusCode();
        }

        var written = await client.PutAsJsonAsync($"/api/admin/papers/{paperId}/questions/1", new
        {
            promptHtml = "<p>Where will the class go on Friday?</p>",
            options = Options(("A", "the museum"), ("B", "the park"), ("C", "the zoo")),
            answers = new[] { "B" },
            points = 1,
        }, ct);
        var paper = await written.Content.ReadFromJsonAsync<JsonObject>(ct);
        var publish = await client.PostAsync($"/api/admin/papers/{paperId}/publish", null, ct);
        var errors = (await publish.Content.ReadFromJsonAsync<JsonObject>(ct))!["errors"]!.AsArray().Select(e => (string)e!).ToList();
        var picture = (string)Question(paper!, 1)["options"]![1]!["imageUrl"]!;

        Assert.Equal(HttpStatusCode.OK, written.StatusCode);
        Assert.DoesNotContain(errors, e => e.StartsWith("Question 1:", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.StartsWith("Question 2:", StringComparison.Ordinal));
        Assert.Equal(Png, await client.GetByteArrayAsync(picture, ct));
    }

    [Fact]
    public async Task PicturesForTheText_AreUploaded_AndOnlyRealPicturesAreAccepted()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var paperId = await BlankAsync(client, 1, "A2 R&W — notices");

        var uploaded = await client.PostAsync($"/api/admin/papers/{paperId}/images", Picture(Png, "notice.png"), ct);
        var url = (string)(await uploaded.Content.ReadFromJsonAsync<JsonObject>(ct))!["url"]!;
        var svg = await client.PostAsync($"/api/admin/papers/{paperId}/images", Picture("<svg onload=alert(1)>"u8.ToArray(), "notice.svg"), ct);
        var fakePng = await client.PostAsync($"/api/admin/papers/{paperId}/images", Picture("<html></html>"u8.ToArray(), "notice.png"), ct);
        var noLayout = await client.PostAsJsonAsync("/api/admin/papers/blank", new { sectionTemplateId = 999, title = "No such section" }, ct);

        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        Assert.StartsWith("/api/assets/", url);
        Assert.Equal(Png, await client.GetByteArrayAsync(url, ct));
        Assert.Equal(HttpStatusCode.BadRequest, svg.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, fakePng.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noLayout.StatusCode);
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

    [Fact]
    public async Task HtmlMayLinkOnlyPicturesTheirPaperUsesOrThePersonUploaded()
    {
        api.SkipIfUnavailable();
        var author = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var other = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var mine = await api.StaffClientAsync(author.Username, Password);
        using var theirs = await api.StaffClientAsync(other.Username, Password);
        var myPaper = await BlankAsync(mine, 1, "C1 mine");
        var theirPaper = await BlankAsync(theirs, 1, "C1 theirs");
        var uploaded = await mine.PostAsync($"/api/admin/papers/{myPaper}/images", Picture(Png, "notice.png"), ct);
        var url = (string)(await uploaded.Content.ReadFromJsonAsync<JsonObject>(ct))!["url"]!;
        var withPicture = new { promptHtml = (string?)null, stimulusHtml = $"<p><img src=\"{url}\"></p>", answers = new[] { "A" }, points = 1 };

        var ownUse = await mine.PutAsJsonAsync($"/api/admin/papers/{myPaper}/questions/1", withPicture, ct);
        // The paper uses the picture now, so any other place in it may use it too.
        var again = await mine.PutAsJsonAsync($"/api/admin/papers/{myPaper}/parts/2", new { title = "Notices", stimulusHtml = $"<p><img src=\"{url}\"></p>" }, ct);
        var copied = await theirs.PutAsJsonAsync($"/api/admin/papers/{theirPaper}/questions/1", withPicture, ct);
        var inPassage = await theirs.PutAsJsonAsync($"/api/admin/papers/{theirPaper}/parts/2", new { title = "Notices", stimulusHtml = $"<p><img src=\"{url}\"></p>" }, ct);
        var inGroup = await theirs.PutAsJsonAsync($"/api/admin/papers/{theirPaper}/parts/4/groups/1", new { instructions = "x", bodyHtml = $"<p><img src=\"{url}\"> {{{{q:19}}}} {{{{q:20}}}} {{{{q:21}}}} {{{{q:22}}}} {{{{q:23}}}} {{{{q:24}}}}</p>" }, ct);
        // The same file uploaded by the other person is theirs to use.
        var theirUpload = await theirs.PostAsync($"/api/admin/papers/{theirPaper}/images", Picture(Png, "notice.png"), ct);
        var theirUrl = (string)(await theirUpload.Content.ReadFromJsonAsync<JsonObject>(ct))!["url"]!;
        var theirUse = await theirs.PutAsJsonAsync($"/api/admin/papers/{theirPaper}/questions/1", new { stimulusHtml = $"<p><img src=\"{theirUrl}\"></p>", answers = new[] { "A" }, points = 1 }, ct);

        Assert.Equal(HttpStatusCode.OK, ownUse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.All([copied, inPassage, inGroup], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Contains(url, await copied.Content.ReadAsStringAsync(ct));
        Assert.NotEqual(url, theirUrl);
        Assert.Equal(HttpStatusCode.OK, theirUse.StatusCode);
    }

    [Fact]
    public async Task APictureLinkWithAnImpossiblyLongNumber_IsRefusedNotAServerError()
    {
        api.SkipIfUnavailable();
        var author = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var mine = await api.StaffClientAsync(author.Username, Password);
        var myPaper = await BlankAsync(mine, 1, "C1 long");
        var link = new { stimulusHtml = "<p><img src=\"/api/assets/99999999999999999999999\"></p>", answers = new[] { "A" }, points = 1 };

        var response = await mine.PutAsJsonAsync($"/api/admin/papers/{myPaper}/questions/1", link, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TwoPeopleImportingTheSameSample_EachGetTheirOwnAssetRows_OverOneStoredFile()
    {
        api.SkipIfUnavailable();
        var first = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var second = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var scope = api.Factory.Services.CreateScope();
        var importer = scope.ServiceProvider.GetRequiredService<PaperPackageImporter>();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var zip = SamplePackages.Zip(SamplePackages.ReadingWriting);

        await importer.ImportAsync(new MemoryStream(zip), first.Id, default);
        await importer.ImportAsync(new MemoryStream(zip), second.Id, default);

        var mine = await db.Assets.AsNoTracking().Where(a => a.UploadedByUserId == first.Id).ToListAsync(ct);
        var theirs = await db.Assets.AsNoTracking().Where(a => a.UploadedByUserId == second.Id).ToListAsync(ct);
        Assert.NotEmpty(mine);
        Assert.Empty(mine.Select(a => a.Id).Intersect(theirs.Select(a => a.Id)));
        Assert.Equal(mine.Select(a => a.Sha256).Order(), theirs.Select(a => a.Sha256).Order());
        Assert.Equal(mine.Select(a => a.StoredPath).Order(), theirs.Select(a => a.StoredPath).Order());
    }

    [Fact]
    public async Task AnImportedPackage_CannotLinkAnotherPersonsPicture()
    {
        api.SkipIfUnavailable();
        var author = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var other = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        using var mine = await api.StaffClientAsync(author.Username, Password);
        using var theirs = await api.StaffClientAsync(other.Username, Password);
        var myPaper = await BlankAsync(mine, 1, "C1 import");
        var uploaded = await mine.PostAsync($"/api/admin/papers/{myPaper}/images", Picture(Png, "notice.png"), ct);
        var url = (string)(await uploaded.Content.ReadFromJsonAsync<JsonObject>(ct))!["url"]!;
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        paper["title"] = $"Linked {Guid.NewGuid():N}";
        SamplePackages.Question(paper, 1)["stimulusHtml"] = $"<p><img src=\"{url}\"></p>";

        async Task<HttpResponseMessage> ImportAsync(HttpClient client)
        {
            var file = new ByteArrayContent(SamplePackages.Zip(paper));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return await client.PostAsync("/api/admin/papers/import", new MultipartFormDataContent { { file, "package", "paper.zip" } }, ct);
        }

        var stolen = await ImportAsync(theirs);
        var own = await ImportAsync(mine);

        Assert.Equal(HttpStatusCode.BadRequest, stolen.StatusCode);
        Assert.Contains(url, await stolen.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
    }
}
