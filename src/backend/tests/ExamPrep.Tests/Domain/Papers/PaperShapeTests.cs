using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Papers;
using static ExamPrep.Tests.Domain.Papers.PaperFactory;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>VE-64: two source papers can be mixed only when every part of one fits the same part of the other.</summary>
public sealed class PaperShapeTests
{
    [Fact]
    public void TwoA2ReadingAndWritingPapers_HaveTheSameShape()
    {
        Assert.Empty(PaperShape.Differences(A2ReadingWriting(1, "Key"), A2ReadingWriting(2, "Key for Schools")));
    }

    [Fact]
    public void TheSamePaperTwice_IsRefused()
    {
        var paper = A2ReadingWriting(1, "Key");

        Assert.Equal(["Choose two different papers."], PaperShape.Differences(paper, paper));
    }

    [Fact]
    public void PapersOfDifferentSections_AreRefused()
    {
        var listening = Paper(2, "Listening", templateId: 2, withAudio: true, (InteractionType.ImageChoice, 5));

        var problem = Assert.Single(PaperShape.Differences(A2ReadingWriting(1, "Key"), listening));
        Assert.Contains("different parts of the test", problem);
    }

    [Fact]
    public void ADifferentNumberOfParts_IsRefused()
    {
        var shorter = Paper(2, "Short", (InteractionType.SingleChoice, 6));

        Assert.Equal(["\"Key\" has 7 parts and \"Short\" has 1."], PaperShape.Differences(A2ReadingWriting(1, "Key"), shorter));
    }

    [Fact]
    public void EveryPartThatDiffers_IsListed()
    {
        var a = Paper(1, "A", (InteractionType.SingleChoice, 6), (InteractionType.InlineChoice, 6), (InteractionType.Essay, 1));
        var b = Paper(2, "B", (InteractionType.SingleChoice, 5), (InteractionType.InlineText, 6), (InteractionType.Essay, 1));

        Assert.Equal(
            [
                "Part 1 has questions 1–6 in \"A\" but 1–5 in \"B\".",
                "Part 2, questions 7–12: answered as InlineChoice in \"A\" but as InlineText in \"B\".",
                "Part 3 has questions 13 in \"A\" but 12 in \"B\".",
            ],
            PaperShape.Differences(a, b));
    }

    [Fact]
    public void AListeningPartWithoutItsRecording_IsRefused()
    {
        var a = Paper(1, "A", templateId: 2, withAudio: true, (InteractionType.ImageChoice, 5), (InteractionType.InlineText, 5));
        var b = Paper(2, "B", templateId: 2, withAudio: true, (InteractionType.ImageChoice, 5), (InteractionType.InlineText, 5));
        b.Parts.Single(p => p.PartNo == 2).AudioAssetId = null;

        Assert.Equal(["Part 2 has a recording in \"A\" but not in \"B\"."], PaperShape.Differences(a, b));
    }
}
