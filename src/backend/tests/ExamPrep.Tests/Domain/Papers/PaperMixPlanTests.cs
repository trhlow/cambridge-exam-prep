using ExamPrep.Domain.Common;
using ExamPrep.Domain.Papers;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>VE-65: choosing, part by part, which source paper each of the four mixed papers takes.</summary>
public sealed class PaperMixPlanTests
{
    public static TheoryData<int> PartCounts => [2, 3, 4, 5, 6, 7];

    [Theory]
    [MemberData(nameof(PartCounts))]
    public void TheFourVersions_AreAllDifferent(int parts)
    {
        foreach (var seed in Enumerable.Range(0, 200))
        {
            var patterns = PaperMixPlan.Patterns(parts, seed);

            Assert.Equal(4, patterns.Count);
            Assert.Equal(4, patterns.Distinct().Count());
            Assert.All(patterns, p => Assert.Equal(parts, p.Length));
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void EveryVersion_TakesPartsFromBothSources_FromThreePartsOn(int parts)
    {
        foreach (var seed in Enumerable.Range(0, 200))
        {
            Assert.All(PaperMixPlan.Patterns(parts, seed), p => Assert.True(p.Contains('A') && p.Contains('B'), p));
        }
    }

    [Fact]
    public void ATwoPartSection_UsesBothMixesAndBothSourcesWhole()
    {
        Assert.Equal(["AA", "AB", "BA", "BB"], PaperMixPlan.Patterns(2, 7).Order());
    }

    [Theory]
    [MemberData(nameof(PartCounts))]
    public void VersionsOneAndTwo_AndThreeAndFour_ShareNoPart(int parts)
    {
        foreach (var seed in Enumerable.Range(0, 50))
        {
            var p = PaperMixPlan.Patterns(parts, seed);
            Assert.All(Enumerable.Range(0, parts), i => Assert.NotEqual(p[0][i], p[1][i]));
            Assert.All(Enumerable.Range(0, parts), i => Assert.NotEqual(p[2][i], p[3][i]));
        }
    }

    [Fact]
    public void TheSameSeed_GivesTheSamePapers_AndSeedsVary()
    {
        Assert.Equal(PaperMixPlan.Patterns(7, 42), PaperMixPlan.Patterns(7, 42));
        Assert.True(Enumerable.Range(0, 20).Select(seed => PaperMixPlan.Patterns(7, seed)[0]).Distinct().Count() > 5);
    }

    [Fact]
    public void FromSourceB_ReadsThePatternByPartNumber()
    {
        Assert.False(PaperMixPlan.FromSourceB("ABA", 1));
        Assert.True(PaperMixPlan.FromSourceB("ABA", 2));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    public void TooFewOrTooManyParts_AreRefused(int parts)
    {
        Assert.Throws<DomainRuleException>(() => PaperMixPlan.Patterns(parts, 1));
    }
}
