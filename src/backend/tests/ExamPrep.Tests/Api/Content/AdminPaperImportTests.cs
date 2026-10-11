using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Content;

/// <summary>VE-5: an administrator imports an exam package; the paper is created as a draft with its files stored.</summary>
public sealed class AdminPaperImportTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string ImportUrl = "/api/admin/papers/import";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PaperImportSummary(int Id, string Title, string ExamType, string Section, string Status, int Parts, int Questions);

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = api.CreateClient();
        var login = await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(ApiFixture.AdminUsername, ApiFixture.AdminPassword), ct);
        login.EnsureSuccessStatusCode();
        return client;
    }

    private Task<HttpResponseMessage> ImportAsync(HttpClient client, byte[] zip)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(zip);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(file, "package", "paper.zip");
        return client.PostAsync(ImportUrl, content, ct);
    }

    private ExamPrepDbContext CreateDb(out IServiceScope scope)
    {
        scope = api.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
    }

    [Fact]
    public async Task Import_CreatesDraftReadingWritingPaperWithEveryPartQuestionAndAnswer()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();

        var response = await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaperImportSummary>(ct);
        Assert.Equal("Reading & Writing", summary!.Section);
        Assert.Equal("Draft", summary.Status);
        Assert.Equal(7, summary.Parts);
        Assert.Equal(32, summary.Questions);

        var db = CreateDb(out var scope);
        using (scope)
        {
            var paper = await db.TestPapers
                .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.AnswerKeys)
                .AsSplitQuery()
                .SingleAsync(p => p.Id == summary.Id, ct);
            var questions = paper.Parts.SelectMany(p => p.Groups).SelectMany(g => g.Questions).ToList();

            Assert.Equal(PaperStatus.Draft, paper.Status);
            Assert.Equal(Enumerable.Range(1, 32), questions.Select(q => q.Number).Order());
            Assert.Equal(["will", "'ll"], questions.Single(q => q.Number == 27).AnswerKeys.Select(k => k.Value).Order(StringComparer.Ordinal).Reverse());
            Assert.Empty(questions.Single(q => q.Number == 31).AnswerKeys);
            Assert.Equal(25, questions.Single(q => q.Number == 31).MinWords);

            var part4 = paper.Parts.Single(p => p.PartNo == 4);
            Assert.Equal(InteractionType.InlineChoice, part4.Groups.Single().InteractionType);
            Assert.Contains("{{q:19}}", part4.Groups.Single().BodyHtml);
            Assert.Equal(PartLayout.Split, paper.Parts.Single(p => p.PartNo == 2).Layout);
        }
    }

    [Fact]
    public async Task Import_StoresPicturesAndPointsHtmlAtThem()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();

        var response = await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));
        var summary = await response.Content.ReadFromJsonAsync<PaperImportSummary>(ct);

        var db = CreateDb(out var scope);
        using (scope)
        {
            var question = await db.Questions
                .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == summary!.Id && q.Number == 1)
                .SingleAsync(ct);
            var assetId = int.Parse(System.Text.RegularExpressions.Regex.Match(question.StimulusHtml!, @"/api/assets/(\d+)").Groups[1].Value);
            var asset = await db.Assets.SingleAsync(a => a.Id == assetId, ct);

            Assert.DoesNotContain("asset:", question.StimulusHtml);
            Assert.Equal(AssetKind.Image, asset.Kind);
            Assert.Equal("image/png", asset.ContentType);
            Assert.Equal(SamplePackages.Placeholder("images/rw-p1-q1.png"), await File.ReadAllBytesAsync(Path.Combine(api.AssetsRoot, asset.StoredPath), ct));
        }
    }

    [Fact]
    public async Task Import_GivesEveryListeningPartItsOwnAudioTrack()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();

        var response = await ImportAsync(client, SamplePackages.Zip(SamplePackages.Listening));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaperImportSummary>(ct);
        var db = CreateDb(out var scope);
        using (scope)
        {
            var parts = await db.PaperParts
                .Include(p => p.AudioAsset)
                .Include(p => p.Groups).ThenInclude(g => g.Questions)
                .Where(p => p.TestPaperId == summary!.Id)
                .OrderBy(p => p.PartNo)
                .ToListAsync(ct);

            Assert.Equal(5, parts.Count);
            Assert.All(parts, p => Assert.Equal(AssetKind.Audio, p.AudioAsset!.Kind));
            Assert.Equal(5, parts.Select(p => p.AudioAssetId).Distinct().Count());
            Assert.Equal("audio/mpeg", parts[0].AudioAsset!.ContentType);

            var dragDrop = parts[4].Groups.Single();
            Assert.Equal(InteractionType.DragDropMatch, dragDrop.InteractionType);
            Assert.Equal(8, JsonDocument.Parse(dragDrop.OptionPoolJson!).RootElement.GetArrayLength());

            var pictureQuestion = parts[0].Groups.Single().Questions.Single(q => q.Number == 1);
            var options = JsonDocument.Parse(pictureQuestion.OptionsJson!).RootElement;
            Assert.True(options[0].GetProperty("imageAssetId").GetInt32() > 0);
        }
    }

    [Fact]
    public async Task Import_ReusesStoredFile_WhenTheSameFileIsImportedAgain()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();
        await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));

        int assetsBefore;
        var db = CreateDb(out var scope);
        using (scope)
        {
            assetsBefore = await db.Assets.CountAsync(ct);
        }

        var second = await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));

        db = CreateDb(out scope);
        using (scope)
        {
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
            Assert.Equal(assetsBefore, await db.Assets.CountAsync(ct));
        }
    }

    [Fact]
    public async Task Import_RemovesScriptsFromHtml()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        SamplePackages.Part(paper, 3)["stimulusHtml"] = "<p onclick='steal()'>Story</p><script>steal()</script><img src='x' onerror='steal()'>";

        var response = await ImportAsync(client, SamplePackages.Zip(paper));
        var summary = await response.Content.ReadFromJsonAsync<PaperImportSummary>(ct);

        var db = CreateDb(out var scope);
        using (scope)
        {
            var part = await db.PaperParts.SingleAsync(p => p.TestPaperId == summary!.Id && p.PartNo == 3, ct);
            Assert.Contains("Story", part.StimulusHtml);
            Assert.DoesNotContain("script", part.StimulusHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("steal", part.StimulusHtml);
        }
    }

    [Fact]
    public async Task Import_Returns400WithEveryProblem_AndCreatesNothing_WhenPackageIsInvalid()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        paper["title"] = $"Broken {Guid.NewGuid():N}";
        SamplePackages.Question(paper, 1)["answers"] = new JsonArray("D");
        SamplePackages.Question(paper, 32).Remove("minWords");

        var response = await ImportAsync(client, SamplePackages.Zip(paper));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        var errors = problem!["errors"]!.AsArray().Select(e => (string)e!).ToList();
        Assert.Equal(2, errors.Count);
        Assert.Contains("Part 1, question 1: answer 'D' is not one of the options (A, B, C).", errors);

        var db = CreateDb(out var scope);
        using (scope)
        {
            Assert.False(await db.TestPapers.AnyAsync(p => p.Title == (string)paper["title"]!, ct));
        }
    }

    [Theory]
    [InlineData("B2_FIRST", 1, "Unknown exam type 'B2_FIRST'.")]
    [InlineData("A2_KEY_FS", 3, "A2 Key for Schools has no section 3.")]
    public async Task Import_Returns400_WhenExamTypeOrSectionIsUnknown(string examType, int section, string error)
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();
        var paper = SamplePackages.Load(SamplePackages.ReadingWriting);
        paper["examType"] = examType;
        paper["section"] = section;

        var response = await ImportAsync(client, SamplePackages.Zip(paper));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        Assert.Contains(error, problem!["errors"]!.AsArray().Select(e => (string)e!));
    }

    [Fact]
    public async Task Import_Returns400_WhenNoFileIsSent()
    {
        api.SkipIfUnavailable();
        using var client = await AdminClientAsync();

        var response = await client.PostAsync(ImportUrl, new MultipartFormDataContent(), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Returns403_ForInvigilator()
    {
        api.SkipIfUnavailable();
        const string password = "Invigilator#2026";
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, password);
        using var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(invigilator.Username, password), ct);

        var response = await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Import_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        using var client = api.CreateClient();

        var response = await ImportAsync(client, SamplePackages.Zip(SamplePackages.ReadingWriting));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
