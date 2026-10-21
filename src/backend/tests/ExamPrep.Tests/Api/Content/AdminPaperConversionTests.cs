using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using ExamPrep.Api.Content;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api.Content;

/// <summary>
/// An administrator uploads the question paper and answer key PDFs (no key for B1 Writing; tracks for Listening); the server converts
/// them and imports the result as a draft. The Python converter is replaced by a fake that writes a sample package.
/// </summary>
public sealed class AdminPaperConversionTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string ConvertUrl = "/api/admin/papers/convert";
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n% made up for a test\n");
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private sealed record ConversionResult(int Id, string Title, string Section, string Status, int Parts, int Questions, List<string> Checks);

    private sealed class FakeConverter(Func<PaperConversionInput, PaperConversionReport> convert) : IPaperConverter
    {
        public PaperConversionInput? Input { get; private set; }

        public List<string> AudioFiles { get; } = [];

        public string? QuestionsStart { get; private set; }

        public Task<PaperConversionReport> ConvertAsync(PaperConversionInput input, CancellationToken cancellationToken)
        {
            Input = input;
            AudioFiles.AddRange(Directory.GetFiles(input.AudioFolder).Select(Path.GetFileName).Order()!);
            QuestionsStart = Encoding.ASCII.GetString(File.ReadAllBytes(input.QuestionsPdf)[..5]);
            return Task.FromResult(convert(input));
        }
    }

    private WebApplicationFactory<Program> WithConverter(IPaperConverter converter) =>
        api.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(converter)));

    private static async Task<HttpClient> AdminClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/staff/auth/login", new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword })).EnsureSuccessStatusCode();
        return client;
    }

    private Task<HttpResponseMessage> ConvertAsync(HttpClient client, byte[]? questions = null, bool withKey = true, params (int Part, string Name)[] audio)
    {
        var form = new MultipartFormDataContent { { new StringContent("Converted test paper"), "title" } };
        AddFile(form, "questions", "questions.pdf", questions ?? Pdf, "application/pdf");
        if (withKey)
        {
            AddFile(form, "answerKey", "key.pdf", Pdf, "application/pdf");
        }

        foreach (var (part, name) in audio)
        {
            AddFile(form, $"audio{part}", name, Encoding.ASCII.GetBytes($"track {part}"), "audio/mpeg");
        }

        return client.PostAsync(ConvertUrl, form, ct);
    }

    private static void AddFile(MultipartFormDataContent form, string field, string name, byte[] content, string type)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(file, field, name);
    }

    private async Task<int> PaperCountAsync()
    {
        using var scope = api.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>().TestPapers.CountAsync(ct);
    }

    private static PaperConversionReport WriteSample(PaperConversionInput input, JsonObject paper, params string[] checks)
    {
        SamplePackages.WriteFolder(paper, input.OutputFolder);
        return new PaperConversionReport([], checks);
    }

    [Fact]
    public async Task Convert_ImportsTheConvertedPaperAsADraftAndReturnsWhatToCheck()
    {
        api.SkipIfUnavailable();
        var converter = new FakeConverter(input => WriteSample(input, SamplePackages.Load(SamplePackages.Listening), "Part 2, question 8: 7:30 is accepted too."));
        using var factory = WithConverter(converter);
        using var client = await AdminClientAsync(factory);

        var response = await ConvertAsync(client, audio: [(1, "P1 sample.mp3"), (3, "track.m4a")]);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ConversionResult>(ct))!;
        Assert.Equal(("Listening", "Draft", 5, 25), (result.Section, result.Status, result.Parts, result.Questions));
        Assert.Equal(["Part 2, question 8: 7:30 is accepted too."], result.Checks);
        Assert.Equal("Converted test paper", converter.Input!.Title);
        Assert.Equal("%PDF-", converter.QuestionsStart);
        Assert.Equal(["part1.mp3", "part3.m4a"], converter.AudioFiles);
        Assert.EndsWith("answer-key.pdf", converter.Input.AnswerKeyPdf);
    }

    [Fact]
    public async Task Convert_RunsWithoutAnAnswerKeyAndLetsTheConverterDecide()
    {
        api.SkipIfUnavailable();
        var converter = new FakeConverter(_ => new PaperConversionReport(["This paper needs its answer key PDF."], []));
        using var factory = WithConverter(converter);
        using var client = await AdminClientAsync(factory);

        var response = await ConvertAsync(client, withKey: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(converter.Input!.AnswerKeyPdf);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>(ct))!;
        Assert.Contains("This paper needs its answer key PDF.", problem["errors"]!.AsArray().Select(e => (string)e!));
    }

    [Fact]
    public async Task Convert_ReturnsTheConverterErrorsAndCreatesNothing()
    {
        api.SkipIfUnavailable();
        var converter = new FakeConverter(_ => new PaperConversionReport(["Part 3 has no audio track."], ["Check the pictures."]));
        using var factory = WithConverter(converter);
        using var client = await AdminClientAsync(factory);
        var before = await PaperCountAsync();

        var response = await ConvertAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>(ct))!;
        Assert.Equal("The PDF could not be converted.", (string)problem["title"]!);
        Assert.Contains("Part 3 has no audio track.", problem["errors"]!.AsArray().Select(e => (string)e!));
        Assert.Equal(before, await PaperCountAsync());
    }

    [Fact]
    public async Task Convert_ReportsWhatTheImporterRejects()
    {
        api.SkipIfUnavailable();
        var paper = SamplePackages.Load(SamplePackages.Listening);
        SamplePackages.Question(paper, 3)["answers"] = new JsonArray();
        using var factory = WithConverter(new FakeConverter(input => WriteSample(input, paper)));
        using var client = await AdminClientAsync(factory);

        var response = await ConvertAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>(ct))!;
        Assert.Contains(problem["errors"]!.AsArray(), e => ((string)e!).Contains("question 3"));
    }

    [Fact]
    public async Task Convert_RefusesAQuestionPaperThatIsNotAPdf()
    {
        api.SkipIfUnavailable();
        var converter = new FakeConverter(_ => throw new InvalidOperationException("must not run"));
        using var factory = WithConverter(converter);
        using var client = await AdminClientAsync(factory);

        var response = await ConvertAsync(client, Encoding.ASCII.GetBytes("not a pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>(ct))!;
        Assert.Contains("The question paper must be a PDF file.", problem["errors"]!.AsArray().Select(e => (string)e!));
        Assert.Null(converter.Input);
    }

    [Fact]
    public async Task Convert_SaysSoWhenTheServerHasNoConverter()
    {
        api.SkipIfUnavailable();
        using var client = await api.AdminClientAsync();

        var response = await ConvertAsync(client);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Convert_IsForAdministratorsOnly()
    {
        api.SkipIfUnavailable();
        var invigilator = await api.CreateStaffAsync(UserRole.Invigilator, "invig-pass-1");
        using var client = await api.StaffClientAsync(invigilator.Username, "invig-pass-1");

        var response = await ConvertAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Detail_ShowsEveryQuestionWithItsAnswersPicturesAndAudio()
    {
        api.SkipIfUnavailable();
        using var factory = WithConverter(new FakeConverter(input => WriteSample(input, SamplePackages.Load(SamplePackages.Listening))));
        using var client = await AdminClientAsync(factory);
        var created = (await (await ConvertAsync(client)).Content.ReadFromJsonAsync<ConversionResult>(ct))!;

        var paper = (await client.GetFromJsonAsync<JsonObject>($"/api/admin/papers/{created.Id}", ct))!;

        Assert.Equal("Draft", (string)paper["status"]!);
        var parts = paper["parts"]!.AsArray();
        Assert.All(parts, part => Assert.StartsWith("/api/assets/", (string)part!["audioUrl"]!));
        var first = parts[0]!["groups"]![0]!["questions"]![0]!;
        Assert.Equal(["B"], first["answers"]!.AsArray().Select(a => (string)a!));
        Assert.StartsWith("/api/assets/", (string)first["options"]![0]!["imageUrl"]!);
        var date = parts[1]!["groups"]![0]!["questions"]![1]!;
        Assert.Equal(["14", "14th", "fourteenth"], date["answers"]!.AsArray().Select(a => (string)a!));
        Assert.Equal(8, parts[4]!["groups"]![0]!["optionPool"]!.AsArray().Count);
    }
}
