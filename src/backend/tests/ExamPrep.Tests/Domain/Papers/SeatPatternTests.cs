using ExamPrep.Domain.Papers;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>
/// VE-66, VE-70: neighbouring computers — side by side, front and back, diagonal — never get the same paper, whatever
/// the number of versions; side by side they share no part when the versions allow it.
/// </summary>
public sealed class SeatPatternTests
{
    private static readonly MixShape TwoByFour = new(4, 2);

    public static TheoryData<int> RowLengths => [1, 2, 3, 4, 5, 6, 7, 8, 10, 12];

    /// <summary>Every mix shape from 2 to 6 sources, with every allowed number of versions.</summary>
    public static TheoryData<int, int, int> RoomsAndShapes()
    {
        var data = new TheoryData<int, int, int>();
        foreach (var sources in new[] { 2, 3, 4, 6 })
        {
            for (var versions = PaperMixPlan.MinVersions; versions <= 3 * sources; versions++)
            {
                foreach (var perRow in new[] { 1, 2, 3, 4, 5, 7, 10 })
                {
                    data.Add(perRow, versions, sources);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RoomsAndShapes))]
    public void EveryPairOfNeighbouringComputers_HasDifferentPapers(int seatsPerRow, int versions, int sources)
    {
        var shape = new MixShape(versions, sources);
        const int seats = 60;
        for (var a = 1; a <= seats; a++)
        {
            for (var b = a + 1; b <= seats; b++)
            {
                var (rowA, columnA) = SeatPattern.Position(a, seatsPerRow);
                var (rowB, columnB) = SeatPattern.Position(b, seatsPerRow);
                if (Math.Abs(rowA - rowB) <= 1 && Math.Abs(columnA - columnB) <= 1)
                {
                    Assert.NotEqual(SeatPattern.VersionFor(a, seatsPerRow, shape), SeatPattern.VersionFor(b, seatsPerRow, shape));
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(RoomsAndShapes))]
    public void SideBySide_TheTwoPapersAreOfOneFamily_WhenEveryFamilyHasTwo(int seatsPerRow, int versions, int sources)
    {
        var shape = new MixShape(versions, sources);
        var families = PaperMixPlan.Families(versions, sources).ToList();
        if (families.Any(f => f.Size < 2) || seatsPerRow < 2)
        {
            return;
        }

        int FamilyOf(int version) => families.FindIndex(f => version >= f.First && version < f.First + f.Size);
        for (var seat = 1; seat < 60; seat++)
        {
            if (SeatPattern.Position(seat, seatsPerRow).Row == SeatPattern.Position(seat + 1, seatsPerRow).Row)
            {
                Assert.Equal(FamilyOf(SeatPattern.VersionFor(seat, seatsPerRow, shape)), FamilyOf(SeatPattern.VersionFor(seat + 1, seatsPerRow, shape)));
            }
        }
    }

    [Theory]
    [MemberData(nameof(RowLengths))]
    public void TwoSourcesAndFourVersions_KeepNeighboursApart(int seatsPerRow)
    {
        for (var seat = 1; seat < 60; seat++)
        {
            if (SeatPattern.Position(seat, seatsPerRow).Row == SeatPattern.Position(seat + 1, seatsPerRow).Row)
            {
                Assert.NotEqual(SeatPattern.VersionFor(seat, seatsPerRow, TwoByFour), SeatPattern.VersionFor(seat + 1, seatsPerRow, TwoByFour));
            }
        }
    }

    [Fact]
    public void ARoomOfFourPerRow_OfTwoSourcesAndFourVersions_IsTiledTwoByTwo_AsBefore()
    {
        var room = Enumerable.Range(1, 12).Select(seat => SeatPattern.VersionFor(seat, 4, TwoByFour)).ToArray();

        Assert.Equal([1, 2, 1, 2, 3, 4, 3, 4, 1, 2, 1, 2], room);
    }

    [Fact]
    public void EightVersionsOfFourSources_RunAsTwoFamilies_RowByRow()
    {
        var room = Enumerable.Range(1, 15).Select(seat => SeatPattern.VersionFor(seat, 5, new MixShape(8, 4))).ToArray();

        Assert.Equal([1, 2, 3, 4, 1, 5, 6, 7, 8, 5, 1, 2, 3, 4, 1], room);
    }

    [Fact]
    public void InOneRow_TheVersionsRunInTurn()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6, 1], Enumerable.Range(1, 7).Select(seat => SeatPattern.VersionFor(seat, null, new MixShape(6, 2))));
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(8, 4)]
    [InlineData(9, 3)]
    [InlineData(4, 6)]
    public void EveryVersionIsUsed_InABigEnoughRoom(int versions, int sources)
    {
        var used = Enumerable.Range(1, 60).Select(seat => SeatPattern.VersionFor(seat, 6, new MixShape(versions, sources))).Distinct().Order();

        Assert.Equal(Enumerable.Range(1, versions), used);
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData(" 7 ", 7)]
    [InlineData("0", null)]
    [InlineData("-3", null)]
    [InlineData("A3", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void SeatNumbers_AreReadFromWhatStaffTyped(string? typed, int? seat)
    {
        Assert.Equal(seat, SeatPattern.Parse(typed));
    }
}
