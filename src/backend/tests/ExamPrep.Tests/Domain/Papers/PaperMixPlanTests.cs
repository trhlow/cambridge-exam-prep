using ExamPrep.Domain.Common;
using ExamPrep.Domain.Papers;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>
/// VE-65, VE-70: choosing, part by part, which of N source papers each of K mixed papers takes (Victory 29/10: K is
/// chosen by the papers manager, at most three times the number of sources).
/// </summary>
public sealed class PaperMixPlanTests
{
    /// <summary>(sources, parts, versions) for every shape a centre is likely to use.</summary>
    public static TheoryData<int, int, int> Shapes()
    {
        var data = new TheoryData<int, int, int>();
        foreach (var sources in new[] { 2, 3, 4, 5, 6 })
        {
            foreach (var parts in new[] { 2, 3, 4, 5, 7 })
            {
                for (var versions = PaperMixPlan.MinVersions; versions <= PaperMixPlan.MaxVersions(sources, parts); versions++)
                {
                    data.Add(sources, parts, versions);
                }
            }
        }

        return data;
    }

    [Theory]
    [InlineData(2, 7, 6)]
    [InlineData(3, 7, 9)]
    [InlineData(4, 5, 12)]
    [InlineData(6, 7, 18)]
    [InlineData(2, 2, 4)]
    [InlineData(3, 2, 9)]
    public void AtMostThreeVersionsPerSource_WhenThePartsAllowThatMany(int sources, int parts, int max)
    {
        Assert.Equal(max, PaperMixPlan.MaxVersions(sources, parts));
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryVersion_IsDifferent_AndReadsOneSourcePerPart(int sources, int parts, int versions)
    {
        foreach (var seed in Enumerable.Range(0, 20))
        {
            var patterns = PaperMixPlan.Patterns(sources, parts, versions, seed);

            Assert.Equal(versions, patterns.Count);
            Assert.Equal(versions, patterns.Distinct().Count());
            Assert.All(patterns, p => Assert.Equal(parts, p.Length));
            Assert.All(patterns, p => Assert.All(p, letter => Assert.InRange(letter, 'A', (char)('A' + sources - 1))));
        }
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void TwoVersionsOfOneFamily_ShareNoPart(int sources, int parts, int versions)
    {
        foreach (var seed in Enumerable.Range(0, 10))
        {
            var patterns = PaperMixPlan.Patterns(sources, parts, versions, seed);
            foreach (var family in PaperMixPlan.Families(versions, sources))
            {
                var members = Enumerable.Range(family.First, family.Size).Select(v => patterns[v - 1]).ToList();
                foreach (var (x, y) in members.SelectMany((x, i) => members.Skip(i + 1).Select(y => (x, y))))
                {
                    Assert.All(Enumerable.Range(0, parts), i => Assert.NotEqual(x[i], y[i]));
                }
            }
        }
    }

    [Theory]
    [InlineData(2, 7, 6)]
    [InlineData(3, 5, 9)]
    [InlineData(4, 7, 8)]
    public void EveryVersion_MixesSources_WhenThereAreEnoughParts(int sources, int parts, int versions)
    {
        foreach (var seed in Enumerable.Range(0, 50))
        {
            Assert.All(PaperMixPlan.Patterns(sources, parts, versions, seed), p => Assert.True(p.Distinct().Count() > 1, p));
        }
    }

    [Theory]
    [InlineData(4, 4, 1, 4)]
    [InlineData(6, 2, 3, 2)]
    [InlineData(8, 3, 3, 3)]
    [InlineData(5, 2, 3, 2)]
    [InlineData(4, 6, 1, 4)]
    public void TheVersions_AreSplitIntoFamiliesOfEvenSize(int versions, int sources, int families, int largest)
    {
        var split = PaperMixPlan.Families(versions, sources);

        Assert.Equal(families, split.Count);
        Assert.Equal(largest, split.Max(f => f.Size));
        Assert.True(split.Max(f => f.Size) - split.Min(f => f.Size) <= 1);
        Assert.Equal(Enumerable.Range(1, versions), split.SelectMany(f => Enumerable.Range(f.First, f.Size)));
    }

    [Fact]
    public void TwoSourcesAndFourVersions_AreTwoPairsOfOpposites_AsBefore()
    {
        foreach (var seed in Enumerable.Range(0, 50))
        {
            var p = PaperMixPlan.Patterns(2, 7, 4, seed);
            Assert.All(Enumerable.Range(0, 7), i => Assert.NotEqual(p[0][i], p[1][i]));
            Assert.All(Enumerable.Range(0, 7), i => Assert.NotEqual(p[2][i], p[3][i]));
        }
    }

    [Fact]
    public void ATwoPartSection_OfTwoSources_UsesBothMixesAndBothSourcesWhole()
    {
        Assert.Equal(["AA", "AB", "BA", "BB"], PaperMixPlan.Patterns(2, 2, 4, 7).Order());
    }

    [Fact]
    public void TheSameSeed_GivesTheSamePapers_AndSeedsVary()
    {
        Assert.Equal(PaperMixPlan.Patterns(3, 7, 9, 42), PaperMixPlan.Patterns(3, 7, 9, 42));
        Assert.True(Enumerable.Range(0, 20).Select(seed => PaperMixPlan.Patterns(3, 7, 9, seed)[0]).Distinct().Count() > 5);
    }

    [Fact]
    public void SourceOf_ReadsThePatternByPartNumber()
    {
        Assert.Equal(0, PaperMixPlan.SourceOf("ACB", 1));
        Assert.Equal(2, PaperMixPlan.SourceOf("ACB", 2));
        Assert.Equal(1, PaperMixPlan.SourceOf("ACB", 3));
    }

    [Theory]
    [InlineData(2, 1, 4)]
    [InlineData(2, 17, 4)]
    [InlineData(1, 7, 4)]
    [InlineData(7, 7, 4)]
    [InlineData(2, 7, 3)]
    [InlineData(2, 7, 7)]
    [InlineData(3, 7, 10)]
    public void ShapesOutsideTheRules_AreRefused(int sources, int parts, int versions)
    {
        Assert.Throws<DomainRuleException>(() => PaperMixPlan.Patterns(sources, parts, versions, 1));
    }
}
