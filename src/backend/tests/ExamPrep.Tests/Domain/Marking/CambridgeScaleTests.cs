using ExamPrep.Domain.Marking;

namespace ExamPrep.Tests.Domain.Marking;

/// <summary>Cambridge's 2020 conversion tables (CER/4240/V2/AUG19) and the grade bands of A2 Key and B1 Preliminary.</summary>
public sealed class CambridgeScaleTests
{
    [Theory]
    [InlineData("A2_KEY_FS", CambridgeScale.Reading, 28, 30, 140)]
    [InlineData("A2_KEY_FS", CambridgeScale.Reading, 20, 30, 120)]
    [InlineData("A2_KEY_FS", CambridgeScale.Writing, 12, 30, 100)]
    [InlineData("A2_KEY_FS", CambridgeScale.Listening, 6, 25, 82)]
    [InlineData("A2_KEY_FS", CambridgeScale.Speaking, 41, 45, 140)]
    [InlineData("B1_PRELIM_FS", CambridgeScale.Reading, 29, 32, 160)]
    [InlineData("B1_PRELIM_FS", CambridgeScale.Writing, 24, 40, 140)]
    [InlineData("B1_PRELIM_FS", CambridgeScale.Listening, 11, 25, 120)]
    [InlineData("B1_PRELIM_FS", CambridgeScale.Speaking, 7, 30, 102)]
    public void TheAnchors_GiveCambridgesScores(string exam, string skill, int raw, int max, int expected) =>
        Assert.Equal(expected, CambridgeScale.Score(exam, skill, raw, max));

    [Fact]
    public void BetweenAnchors_TheScoreLiesOnTheLineJoiningThem()
    {
        // A2 Reading: 20 → 120, 28 → 140, so 24 → 130; 21 → 122.5, rounded up.
        Assert.Equal(130, CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Reading, 24, 30));
        Assert.Equal(123, CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Reading, 21, 30));
    }

    [Fact]
    public void FullMarks_GiveTheTopOfTheRange_AndBelowTheMinimumNothingIsReported()
    {
        Assert.Equal(150, CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Listening, 25, 25));
        Assert.Equal(170, CambridgeScale.Score("B1_PRELIM_FS", CambridgeScale.Speaking, 30, 30));
        Assert.Null(CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Reading, 6, 30));
    }

    [Fact]
    public void APaperWithOtherPoints_IsScaledToCambridgesMarks()
    {
        // 14 of 15 is 28 of 30.
        Assert.Equal(140, CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Reading, 14, 15));
    }

    [Fact]
    public void AnExamTypeWithoutATable_OrAPaperWithoutMarks_HasNoScore()
    {
        Assert.Null(CambridgeScale.Score("IELTS", CambridgeScale.Reading, 20, 40));
        Assert.Null(CambridgeScale.Score("A2_KEY_FS", CambridgeScale.Reading, 0, 0));
        Assert.Null(CambridgeScale.SpeakingMax("IELTS"));
        Assert.Equal(45, CambridgeScale.SpeakingMax("A2_KEY_FS"));
    }

    [Fact]
    public void TheOverallScore_IsTheRoundedMean_AndGivesTheGrade()
    {
        Assert.Equal(134, CambridgeScale.Overall([140, 130, 133, 132]));
        Assert.Equal(new CambridgeScale.Band(133, "B", "A2"), CambridgeScale.GradeFor("A2_KEY_FS", 134));
        Assert.Equal("A", CambridgeScale.GradeFor("A2_KEY_FS", 140)!.Grade);
        Assert.Equal("Level A1", CambridgeScale.GradeFor("A2_KEY_FS", 100)!.Grade);
        Assert.Null(CambridgeScale.GradeFor("A2_KEY_FS", 99));
        Assert.Equal(new CambridgeScale.Band(140, "C", "B1"), CambridgeScale.GradeFor("B1_PRELIM_FS", 152));
        Assert.Equal("Level A2", CambridgeScale.GradeFor("B1_PRELIM_FS", 120)!.Grade);
    }
}
