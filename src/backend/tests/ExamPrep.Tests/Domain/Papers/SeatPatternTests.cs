using ExamPrep.Domain.Papers;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>VE-66: neighbouring computers — side by side, front and back, diagonal — never get the same paper.</summary>
public sealed class SeatPatternTests
{
    public static TheoryData<int> RowLengths => [1, 2, 3, 4, 5, 6, 7, 8, 10, 12];

    [Theory]
    [MemberData(nameof(RowLengths))]
    public void EveryPairOfNeighbouringComputers_HasDifferentPapers(int seatsPerRow)
    {
        const int seats = 60;
        for (var a = 1; a <= seats; a++)
        {
            for (var b = a + 1; b <= seats; b++)
            {
                var (rowA, columnA) = SeatPattern.Position(a, seatsPerRow);
                var (rowB, columnB) = SeatPattern.Position(b, seatsPerRow);
                if (Math.Abs(rowA - rowB) <= 1 && Math.Abs(columnA - columnB) <= 1)
                {
                    Assert.NotEqual(SeatPattern.VersionFor(a, seatsPerRow), SeatPattern.VersionFor(b, seatsPerRow));
                }
            }
        }
    }

    [Fact]
    public void ARoomOfFourPerRow_IsTiledTwoByTwo()
    {
        var room = Enumerable.Range(1, 12).Select(seat => SeatPattern.VersionFor(seat, 4)).ToArray();

        Assert.Equal([1, 2, 1, 2, 3, 4, 3, 4, 1, 2, 1, 2], room);
    }

    [Fact]
    public void InOneRow_TheVersionsRunInTurn()
    {
        Assert.Equal([1, 2, 3, 4, 1, 2], Enumerable.Range(1, 6).Select(seat => SeatPattern.VersionFor(seat, null)));
    }

    [Fact]
    public void EveryVersionIsUsed_InABigEnoughRoom()
    {
        Assert.Equal([1, 2, 3, 4], Enumerable.Range(1, 20).Select(seat => SeatPattern.VersionFor(seat, 5)).Distinct().Order());
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
