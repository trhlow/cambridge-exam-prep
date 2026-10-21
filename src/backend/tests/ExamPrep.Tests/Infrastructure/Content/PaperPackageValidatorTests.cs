using System.Text.Json.Nodes;
using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;

namespace ExamPrep.Tests.Infrastructure.Content;

/// <summary>VE-5: a wrong package is rejected with every problem listed, in words an exam administrator understands.</summary>
public sealed class PaperPackageValidatorTests
{
    private static SectionTemplate Template(JsonObject paper)
    {
        var examType = SeedData.ExamTypes.Single(t => t.Code == (string)paper["examType"]!);
        return SeedData.SectionTemplates.Single(t => t.ExamTypeId == examType.Id && t.OrderNo == (int)paper["section"]!);
    }

    private static IReadOnlyList<string> Validate(JsonObject paper, IEnumerable<string>? omit = null, IReadOnlyDictionary<string, byte[]>? extraFiles = null)
    {
        var read = PaperPackageReader.Read(new MemoryStream(SamplePackages.Zip(paper, omit, extraFiles)));
        Assert.Empty(read.Errors);
        return PaperPackageValidator.Validate(read.Package!, Template(paper));
    }

    private static JsonObject ReadingWriting() => SamplePackages.Load(SamplePackages.ReadingWriting);

    private static JsonObject Listening() => SamplePackages.Load(SamplePackages.Listening);

    [Theory]
    [InlineData(SamplePackages.ReadingWriting)]
    [InlineData(SamplePackages.Listening)]
    [InlineData(SamplePackages.B1Reading)]
    [InlineData(SamplePackages.B1Listening)]
    public void Validate_AcceptsSamplePackages(string name)
    {
        var errors = Validate(SamplePackages.Load(name));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReportsWrongNumberOfParts()
    {
        var paper = ReadingWriting();
        paper["parts"]!.AsArray().RemoveAt(6);

        var errors = Validate(paper);

        Assert.Contains("Expected 7 parts for Reading & Writing, found 6.", errors);
    }

    [Fact]
    public void Validate_ReportsGapInQuestionNumbers()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 10)["number"] = 11;

        var errors = Validate(paper);

        Assert.Contains(errors, e => e.Contains("Part 2") && e.Contains("question 11") && e.Contains("10 was expected"));
    }

    [Fact]
    public void Validate_ReportsAnswerThatIsNotAnOption()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 1)["answers"] = new JsonArray("D");

        var errors = Validate(paper);

        Assert.Contains("Part 1, question 1: answer 'D' is not one of the options (A, B, C).", errors);
    }

    [Fact]
    public void Validate_ReportsChoiceQuestionWithoutAnswer()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 14).Remove("answers");

        var errors = Validate(paper);

        Assert.Contains("Part 3, question 14: choice questions need exactly one answer.", errors);
    }

    [Fact]
    public void Validate_ReportsTypedGapWithoutAnswer()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 25)["answers"] = new JsonArray();

        var errors = Validate(paper);

        Assert.Contains("Part 5, question 25: at least one accepted answer is required.", errors);
    }

    [Fact]
    public void Validate_ReportsQuestionWhoseGapIsMissingFromTheText()
    {
        var paper = ReadingWriting();
        var group = SamplePackages.Group(paper, 4);
        group["bodyHtml"] = ((string)group["bodyHtml"]!).Replace("{{q:21}}", "brought");

        var errors = Validate(paper);

        Assert.Contains("Part 4: gap {{q:21}} is missing from the text.", errors);
    }

    [Fact]
    public void Validate_ReportsGapWithoutQuestion()
    {
        var paper = ReadingWriting();
        var group = SamplePackages.Group(paper, 5);
        group["bodyHtml"] = (string)group["bodyHtml"]! + "<p>{{q:99}}</p>";

        var errors = Validate(paper);

        Assert.Contains("Part 5: gap {{q:99}} does not match any question in this group.", errors);
    }

    [Fact]
    public void Validate_ReportsReferencedFileMissingFromPackage()
    {
        var errors = Validate(ReadingWriting(), omit: ["images/rw-p1-q1.png"]);

        Assert.Contains("Part 1, question 1: file 'images/rw-p1-q1.png' is not in the package.", errors);
    }

    [Fact]
    public void Validate_ReportsUnsupportedImageType()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 2)["stimulusHtml"] = "<img src='asset:images/notice.svg'>";

        var errors = Validate(paper);

        Assert.Contains(errors, e => e.StartsWith("Part 1, question 2: 'images/notice.svg' is not a supported image"));
    }

    [Fact]
    public void Validate_ReportsListeningPartWithoutAudio()
    {
        var paper = Listening();
        SamplePackages.Part(paper, 3).Remove("audio");

        var errors = Validate(paper);

        Assert.Contains("Part 3: Listening parts need an audio track.", errors);
    }

    [Fact]
    public void Validate_ReportsAudioOnReadingPart()
    {
        var paper = ReadingWriting();
        SamplePackages.Part(paper, 1)["audio"] = "audio/part1.mp3";

        var errors = Validate(paper);

        Assert.Contains("Part 1: only Listening parts can have audio.", errors);
    }

    [Fact]
    public void Validate_ReportsAudioTrackThatIsNotAudio()
    {
        var paper = Listening();
        SamplePackages.Part(paper, 2)["audio"] = "audio/part2.exe";

        var errors = Validate(paper);

        Assert.Contains(errors, e => e.StartsWith("Part 2: 'audio/part2.exe' is not a supported audio"));
    }

    [Fact]
    public void Validate_ReportsImageChoiceOptionWithoutPicture()
    {
        var paper = Listening();
        SamplePackages.Question(paper, 3)["options"]![1]!.AsObject().Remove("image");

        var errors = Validate(paper);

        Assert.Contains("Part 1, question 3: option B needs a picture.", errors);
    }

    [Fact]
    public void Validate_ReportsDragDropAnswerThatIsNotACard()
    {
        var paper = Listening();
        SamplePackages.Question(paper, 22)["answers"] = new JsonArray("Z");

        var errors = Validate(paper);

        Assert.Contains("Part 5, question 22: answer 'Z' is not one of the cards (A, B, C, D, E, F, G, H).", errors);
    }

    [Fact]
    public void Validate_ReportsEssayWithAnswers()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 31)["answers"] = new JsonArray("model answer");

        var errors = Validate(paper);

        Assert.Contains("Part 6, question 31: Essay questions are marked by a teacher and cannot have answers.", errors);
    }

    [Fact]
    public void Validate_ReportsEssayWithoutMinimumWords()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 32).Remove("minWords");

        var errors = Validate(paper);

        Assert.Contains("Part 7, question 32: Essay questions need minWords.", errors);
    }

    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        var paper = ReadingWriting();
        SamplePackages.Question(paper, 1)["answers"] = new JsonArray("D");
        SamplePackages.Question(paper, 31)["answers"] = new JsonArray("model answer");

        var errors = Validate(paper, omit: ["images/rw-p7-2.png"]);

        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void Validate_DragAndDropIntoAText_NeedsEveryGap_ButNoLabels()
    {
        // B1 Reading Part 4: sentences are dragged into gaps of the text, so the questions have no labels.
        var paper = SamplePackages.Load(SamplePackages.B1Reading);
        var group = paper["parts"]![3]!["groups"]![0]!.AsObject();
        group["bodyHtml"] = ((string)group["bodyHtml"]!).Replace("{{q:18}}", string.Empty);

        var errors = Validate(paper);

        Assert.Equal(["Part 4: gap {{q:18}} is missing from the text."], errors);
    }

    [Fact]
    public void Validate_DragAndDropRowsWithoutAText_StillNeedLabels()
    {
        var paper = SamplePackages.Load(SamplePackages.B1Reading);
        SamplePackages.Question(paper, 7).Remove("promptHtml");

        var errors = Validate(paper);

        Assert.Contains(errors, e => e.Contains("question 7") && e.Contains("need a label"));
    }

    [Fact]
    public void Validate_RefusesOverlongColumnHeadings()
    {
        var paper = SamplePackages.Load(SamplePackages.B1Reading);
        paper["parts"]![1]!["groups"]![0]!["poolHeading"] = new string('x', 129);

        var errors = Validate(paper);

        Assert.Contains(errors, e => e.Contains("poolHeading can be at most 128 characters"));
    }
}
