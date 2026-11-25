using ExamPrep.Api.Results;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Api.Results;

/// <summary>Section scores split into Cambridge's four skills and put on the Cambridge English Scale.</summary>
public sealed class SkillScoresTests
{
    private static SkillScores.SectionAttempt Sat(Skill skill, GradingStatus grading, decimal raw, decimal max, decimal autoRaw = 0, decimal autoMax = 0) =>
        new(skill, AttemptStatus.Submitted, grading, raw, max, autoRaw, autoMax);

    [Fact]
    public void A2ReadingAndWriting_GivesReadingFromTheAutomaticMarks_AndWritingFromTheEssays()
    {
        var result = SkillScores.For("A2_KEY_FS", [Sat(Skill.ReadingWriting, GradingStatus.Final, 54, 60, autoRaw: 28, autoMax: 30), Sat(Skill.Listening, GradingStatus.AutoGraded, 23, 25)], 41)!.Value;

        Assert.Equal(["Reading", "Writing", "Listening", "Speaking"], result.Skills.Select(s => s.Skill));
        Assert.Equal(new SkillResult("Reading", 28, 30, 140), result.Skills[0]);
        Assert.Equal(new SkillResult("Writing", 26, 30, 140), result.Skills[1]);
        Assert.Equal(140, result.Overall);
        Assert.Equal("A", result.Band!.Grade);
        Assert.Equal("B1", result.Band.Cefr);
    }

    [Fact]
    public void WritingStillToMark_AndSpeakingNotEntered_LeaveTheOverallOpen()
    {
        var result = SkillScores.For("A2_KEY_FS", [Sat(Skill.ReadingWriting, GradingStatus.NeedsManualMarking, 20, 60, autoRaw: 20, autoMax: 30)], null)!.Value;

        Assert.Equal(120, result.Skills[0].Scale);
        Assert.Equal(new SkillResult("Writing", null, null, null), result.Skills[1]);
        Assert.Equal(new SkillResult("Speaking", null, 45, null), result.Skills[3]);
        Assert.Null(result.Overall);
        Assert.Null(result.Band);
    }

    [Fact]
    public void B1_HasOneSectionPerSkill()
    {
        var result = SkillScores.For("B1_PRELIM_FS",
            [Sat(Skill.Reading, GradingStatus.AutoGraded, 23, 32), Sat(Skill.Writing, GradingStatus.Final, 24, 40), Sat(Skill.Listening, GradingStatus.AutoGraded, 18, 25)], 18)!.Value;

        Assert.All(result.Skills, s => Assert.Equal(140, s.Scale));
        Assert.Equal(140, result.Overall);
        Assert.Equal("C", result.Band!.Grade);
    }

    [Fact]
    public void AnExamTypeWithoutATable_HasNoScale() =>
        Assert.Null(SkillScores.For("IELTS", [], null));
}
