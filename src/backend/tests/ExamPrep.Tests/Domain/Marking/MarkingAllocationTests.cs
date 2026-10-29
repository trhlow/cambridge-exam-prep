using ExamPrep.Domain.Marking;

namespace ExamPrep.Tests.Domain.Marking;

/// <summary>
/// VE-73 (Victory 29/10): the candidates whose Writing is to be marked are dealt to the session's examiners at random
/// and evenly — 20 candidates and 4 examiners give each examiner 5 — and each script carries a code instead of a name.
/// </summary>
public sealed class MarkingAllocationTests
{
    private static readonly int[] Examiners = [101, 102, 103, 104];

    [Fact]
    public void TwentyCandidates_AndFourExaminers_GiveFiveEach()
    {
        var dealt = MarkingAllocation.Deal(Enumerable.Range(1, 20).ToList(), Examiners, new Dictionary<int, int>(), new Random(1));

        Assert.Equal(20, dealt.Count);
        Assert.All(Examiners, examiner => Assert.Equal(5, dealt.Values.Count(v => v == examiner)));
    }

    [Theory]
    [InlineData(7, 3)]
    [InlineData(1, 4)]
    [InlineData(26, 4)]
    public void TheShares_DifferByOneAtMost(int candidates, int examiners)
    {
        var ids = Enumerable.Range(201, examiners).ToList();
        var dealt = MarkingAllocation.Deal(Enumerable.Range(1, candidates).ToList(), ids, new Dictionary<int, int>(), new Random(5));
        var shares = ids.Select(id => dealt.Values.Count(v => v == id)).ToList();

        Assert.Equal(candidates, dealt.Count);
        Assert.True(shares.Max() - shares.Min() <= 1, string.Join(",", shares));
    }

    [Fact]
    public void ScriptsAlreadyKept_CountTowardsTheShares()
    {
        // 101 keeps 4 scripts it has started marking; the 8 new ones even the shares out at 3 each.
        var kept = new Dictionary<int, int> { [101] = 4 };
        var dealt = MarkingAllocation.Deal(Enumerable.Range(1, 8).ToList(), Examiners, kept, new Random(3));

        Assert.Equal(0, dealt.Values.Count(v => v == 101));
        Assert.All(Examiners.Skip(1), examiner => Assert.InRange(dealt.Values.Count(v => v == examiner), 2, 3));
    }

    [Fact]
    public void TheDealIsRandom()
    {
        var candidates = Enumerable.Range(1, 20).ToList();
        var deals = Enumerable.Range(0, 10).Select(seed => string.Join(",", MarkingAllocation.Deal(candidates, Examiners, new Dictionary<int, int>(), new Random(seed)).OrderBy(p => p.Key).Select(p => p.Value))).ToList();

        Assert.True(deals.Distinct().Count() > 5);
    }

    [Fact]
    public void WithoutExaminers_NobodyIsDealt()
    {
        Assert.Empty(MarkingAllocation.Deal([1, 2, 3], [], new Dictionary<int, int>(), new Random(1)));
    }

    [Fact]
    public void Codes_AreSixLettersOrDigitsThatCannotBeMisread_AndNeverRepeatInASession()
    {
        var random = new Random(9);
        var taken = new HashSet<string>();
        for (var i = 0; i < 500; i++)
        {
            var code = MarkingAllocation.NewCode(random, taken);
            Assert.Matches("^[A-HJ-NP-Z2-9]{6}$", code);
            Assert.True(taken.Add(code));
        }
    }
}
