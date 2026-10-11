using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// VE-7: the candidate screen loads the paper of a section — everything needed to show the questions, never
/// the answer keys — and the pictures and audio it refers to.
/// </summary>
public sealed partial class CandidatePaperTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "kq7m-2xpt";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record LoginRequest(string Username, string Password);

    private sealed record PinRequest(string Pin);

    private async Task<(HttpClient Client, ApiFixture.SeededCandidate Candidate)> CandidateWithPinAsync(bool enterPin = true)
    {
        var candidate = await api.CreateCandidateAsync(Password, withSamplePapers: true);
        var client = api.CreateClient();
        (await client.PostAsJsonAsync("/api/candidate/auth/login", new LoginRequest(candidate.Username, Password), ct)).EnsureSuccessStatusCode();
        if (enterPin)
        {
            (await client.PostAsJsonAsync("/api/candidate/auth/pin", new PinRequest(candidate.SessionPin), ct)).EnsureSuccessStatusCode();
        }

        return (client, candidate);
    }

    private async Task<JsonObject> GetPaperAsync(HttpClient client, int sectionId)
    {
        var response = await client.GetAsync($"/api/candidate/sections/{sectionId}/paper", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>(ct))!;
    }

    private static IEnumerable<JsonObject> Questions(JsonObject paper) =>
        paper["parts"]!.AsArray()
            .SelectMany(p => p!["groups"]!.AsArray())
            .SelectMany(g => g!["questions"]!.AsArray())
            .Select(q => q!.AsObject());

    private static IEnumerable<string> PropertyNames(JsonNode? node) => node switch
    {
        JsonObject obj => obj.SelectMany(p => PropertyNames(p.Value).Prepend(p.Key)),
        JsonArray array => array.SelectMany(PropertyNames),
        _ => [],
    };

    [Fact]
    public async Task Paper_ReturnsEveryPartAndQuestionOfTheSection()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync();
        using var _ = client;

        var paper = await GetPaperAsync(client, candidate.ReadingWritingSectionId);

        Assert.Equal("Reading & Writing", (string)paper["title"]!);
        Assert.Equal(60, (int)paper["durationMinutes"]!);
        Assert.Equal(7, paper["parts"]!.AsArray().Count);
        Assert.Equal(Enumerable.Range(1, 32), Questions(paper).Select(q => (int)q["number"]!));
        var part2 = paper["parts"]![1]!;
        Assert.Equal("Split", (string)part2["layout"]!);
        var part4Group = paper["parts"]![3]!["groups"]![0]!;
        Assert.Equal("InlineChoice", (string)part4Group["interactionType"]!);
        Assert.Contains("{{q:19}}", (string)part4Group["bodyHtml"]!);
        var question1Options = Questions(paper).First()["options"]!.AsArray();
        Assert.Equal(["A", "B", "C"], question1Options.Select(o => (string)o!["key"]!));
        Assert.Equal(25, (int)Questions(paper).Single(q => (int)q["number"]! == 31)["minWords"]!);
    }

    [Fact]
    public async Task Paper_NeverContainsAnswers()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync();
        using var _ = client;

        var paper = await GetPaperAsync(client, candidate.ReadingWritingSectionId);

        // maxWordsPerAnswer is a display hint ("Write ONE word"); anything that could carry a key is forbidden.
        string[] keyLike = ["answers", "answerKeys", "answerKey", "correct", "isCorrect", "value"];
        Assert.DoesNotContain(PropertyNames(paper), name => keyLike.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Paper_GivesListeningPartsAnAudioUrl_AndPictureOptionsAnImageUrl()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync();
        using var _ = client;

        var paper = await GetPaperAsync(client, candidate.ListeningSectionId);

        Assert.All(paper["parts"]!.AsArray(), p => Assert.StartsWith("/api/assets/", (string)p!["audioUrl"]!));
        var firstOption = Questions(paper).First()["options"]![0]!;
        Assert.StartsWith("/api/assets/", (string)firstOption["imageUrl"]!);
    }

    [Fact]
    public async Task Paper_Returns403_BeforePinIsEntered()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync(enterPin: false);
        using var _ = client;

        var response = await client.GetAsync($"/api/candidate/sections/{candidate.ReadingWritingSectionId}/paper", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Paper_Returns404_ForSectionOfAnotherExam()
    {
        api.SkipIfUnavailable();
        var other = await api.CreateCandidateAsync(Password);
        var (client, _) = await CandidateWithPinAsync();
        using var __ = client;

        var response = await client.GetAsync($"/api/candidate/sections/{other.ReadingWritingSectionId}/paper", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Asset_ServesPictureUsedInTheCandidatesPaper()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync();
        using var _ = client;
        var paper = await GetPaperAsync(client, candidate.ReadingWritingSectionId);
        var src = AssetUrl().Match((string)Questions(paper).First()["stimulusHtml"]!).Value;

        var response = await client.GetAsync(src, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(SamplePackages.Placeholder("images/rw-p1-q1.png"), await response.Content.ReadAsByteArrayAsync(ct));
    }

    [Fact]
    public async Task Asset_ServesListeningAudio()
    {
        api.SkipIfUnavailable();
        var (client, candidate) = await CandidateWithPinAsync();
        using var _ = client;
        var paper = await GetPaperAsync(client, candidate.ListeningSectionId);

        var response = await client.GetAsync((string)paper["parts"]![0]!["audioUrl"]!, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("audio/mpeg", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Asset_Returns404_ToCandidate_WhenFileIsNotPartOfTheirExam()
    {
        api.SkipIfUnavailable();
        var (client, _) = await CandidateWithPinAsync();
        using var __ = client;
        var unrelated = await CreateUnrelatedAssetAsync();

        var response = await client.GetAsync($"/api/assets/{unrelated}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Asset_Returns401_WhenNotSignedIn()
    {
        api.SkipIfUnavailable();
        var unrelated = await CreateUnrelatedAssetAsync();
        using var client = api.CreateClient();

        var response = await client.GetAsync($"/api/assets/{unrelated}", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asset_IsAvailableToStaff()
    {
        api.SkipIfUnavailable();
        var unrelated = await CreateUnrelatedAssetAsync();
        using var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/staff/auth/login", new LoginRequest(ApiFixture.AdminUsername, ApiFixture.AdminPassword), ct);

        var response = await client.GetAsync($"/api/assets/{unrelated}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>A stored picture that no paper refers to, e.g. one belonging to next month's exam.</summary>
    private async Task<int> CreateUnrelatedAssetAsync()
    {
        var folder = Path.Combine(api.AssetsRoot, "zz");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}.png";
        await File.WriteAllBytesAsync(Path.Combine(folder, name), [1, 2, 3], ct);

        using var scope = api.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var asset = new Asset
        {
            Kind = AssetKind.Image,
            OriginalFileName = "secret.png",
            StoredPath = $"zz/{name}",
            ContentType = "image/png",
            SizeBytes = 3,
            Sha256 = new string('0', 64),
            CreatedAt = DateTime.UtcNow,
        };
        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        return asset.Id;
    }

    [GeneratedRegex(@"/api/assets/\d+")]
    private static partial Regex AssetUrl();
}
