using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ExamPrep.Domain.Enums;
using ExamPrep.Tests.Infrastructure.Content;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// VE-65 (Victory 20/10): two published papers of the same layout are mixed part by part into four published papers,
/// each part copied whole — questions, answer keys and recording — from the source the pattern names.
/// </summary>
public sealed class PaperMixTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Password = "Mixer#2026-pass";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private async Task<(int A, int B)> SourcesAsync(string sample)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return (await api.ImportPublishedSampleAsync(sample, $"Source A {suffix}"), await api.ImportPublishedSampleAsync(sample, $"Source B {suffix}"));
    }

    private static async Task<JsonObject> PaperAsync(HttpClient client, int paperId) =>
        (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{paperId}"))!;

    [Fact]
    public async Task TwoPapers_BecomeFourDifferentPublishedPapers_TakingEachPartWholeFromItsSource()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (a, b) = await SourcesAsync(SamplePackages.ReadingWriting);
        var sourceA = await PaperAsync(client, a);
        var sourceB = await PaperAsync(client, b);

        var response = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b, title = "Mock test 1", seed = 2026 }, ct);
        var mix = await response.Content.ReadFromJsonAsync<JsonObject>(ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Mock test 1", (string)mix!["title"]!);
        Assert.Equal(2026, (int)mix["seed"]!);
        var versions = mix["versions"]!.AsArray();
        Assert.Equal([1, 2, 3, 4], versions.Select(v => (int)v!["version"]!));
        Assert.Equal(4, versions.Select(v => (string)v!["pattern"]!).Distinct().Count());

        foreach (var version in versions)
        {
            var pattern = (string)version!["pattern"]!;
            var paper = await PaperAsync(client, (int)version["paperId"]!);
            Assert.Equal("Published", (string)paper["status"]!);
            Assert.Equal($"Mock test 1 – Version {(int)version["version"]!}", (string)paper["title"]!);
            var parts = paper["parts"]!.AsArray();
            Assert.Equal(7, parts.Count);
            for (var i = 0; i < parts.Count; i++)
            {
                var source = pattern[i] == 'A' ? sourceA : sourceB;
                var expected = source["parts"]![i]!;
                Assert.Equal((string)expected["title"]!, (string)parts[i]!["title"]!);
                // The whole part comes along: same questions, same answer keys.
                Assert.Equal(expected["groups"]!.ToJsonString(), parts[i]!["groups"]!.ToJsonString());
            }
        }
    }

    [Fact]
    public async Task TheSameSeed_GivesTheSamePatterns()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (a, b) = await SourcesAsync(SamplePackages.ReadingWriting);

        async Task<string[]> PatternsAsync()
        {
            var response = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b, seed = 7 }, ct);
            var mix = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
            return mix!["versions"]!.AsArray().Select(v => (string)v!["pattern"]!).ToArray();
        }

        Assert.Equal(await PatternsAsync(), await PatternsAsync());
    }

    [Fact]
    public async Task ListeningParts_KeepTheirRecordings()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (a, b) = await SourcesAsync(SamplePackages.Listening);

        var response = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct);
        var mix = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        var paper = await PaperAsync(client, (int)mix!["versions"]![0]!["paperId"]!);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.All(paper["parts"]!.AsArray(), part => Assert.NotNull(part!["audioUrl"]));
    }

    [Fact]
    public async Task PapersThatDoNotFit_AreRefusedWithEveryReason()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (readingWriting, _) = await SourcesAsync(SamplePackages.ReadingWriting);
        var (listening, _) = await SourcesAsync(SamplePackages.Listening);

        var differentSections = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = readingWriting, sourceBPaperId = listening }, ct);
        var samePaper = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = readingWriting, sourceBPaperId = readingWriting }, ct);
        var missing = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = readingWriting, sourceBPaperId = 999_999 }, ct);

        Assert.All([differentSections, samePaper, missing], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Contains("different parts of the test", await differentSections.Content.ReadAsStringAsync(ct));
        Assert.Contains("Choose two different papers.", await samePaper.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task ADraft_OrAnAlreadyMixedPaper_CannotBeASource()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (a, b) = await SourcesAsync(SamplePackages.ReadingWriting);
        var created = await (await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct)).Content.ReadFromJsonAsync<JsonObject>(ct);
        var mixed = (int)created!["versions"]![0]!["paperId"]!;
        var blank = await client.PostAsJsonAsync("/api/admin/papers/blank", new { sectionTemplateId = 1, title = "Unwritten" }, ct);
        var blankId = (int)(await blank.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;

        var fromMixed = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = mixed, sourceBPaperId = a }, ct);
        var fromDraft = await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = blankId, sourceBPaperId = a }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, fromMixed.StatusCode);
        Assert.Contains("is itself a mixed paper", await fromMixed.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.BadRequest, fromDraft.StatusCode);
        Assert.Contains("is not published yet", await fromDraft.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task MixedPapers_AndTheirSources_CannotBeDeleted()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();
        var (a, b) = await SourcesAsync(SamplePackages.ReadingWriting);
        var mix = await (await client.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct)).Content.ReadFromJsonAsync<JsonObject>(ct);

        var version = await client.DeleteAsync($"/api/admin/papers/{(int)mix!["versions"]![2]!["paperId"]!}", ct);
        var source = await client.DeleteAsync($"/api/admin/papers/{b}", ct);

        Assert.Equal(HttpStatusCode.Conflict, version.StatusCode);
        Assert.Contains("one of the four papers of a mix", await version.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.Conflict, source.StatusCode);
        Assert.Contains("Papers were mixed from this paper", await source.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task OnlyStaffWorkingOnPapers_MixThem_AndEveryoneThereSeesTheList()
    {
        api.SkipIfUnavailable();
        var (a, b) = await SourcesAsync(SamplePackages.ReadingWriting);
        var papersManager = await api.CreateManagerAsync(Password, ManagerArea.Papers);
        var examsManager = await api.CreateManagerAsync(Password, ManagerArea.Exams);
        using var allowed = await api.StaffClientAsync(papersManager.Username, Password);
        using var refused = await api.StaffClientAsync(examsManager.Username, Password);

        var denied = await refused.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct);
        var created = await allowed.PostAsJsonAsync("/api/admin/paper-mixes", new { sourceAPaperId = a, sourceBPaperId = b }, ct);
        var id = (int)(await created.Content.ReadFromJsonAsync<JsonObject>(ct))!["id"]!;
        var list = await allowed.GetFromJsonAsync<JsonArray>("/api/admin/paper-mixes", ct);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Contains(list!, m => (int)m!["id"]! == id && (int)m["sourceA"]!["id"]! == a && (int)m["sourceB"]!["id"]! == b);
        Assert.Equal(HttpStatusCode.NotFound, (await allowed.GetAsync("/api/admin/paper-mixes/999999", ct)).StatusCode);
    }
}
