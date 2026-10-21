namespace ExamPrep.Domain.Papers;

/// <summary>
/// Which of the four mixed papers a computer gets (Victory 20/10: neighbouring computers never sit the same paper).
/// Seats are numbered from 1 row by row, <c>seatsPerRow</c> to a row. The room is tiled with 2×2 blocks
/// <c>[1 2 / 3 4]</c>, so the computers left, right, in front, behind and diagonally next to any computer all have a
/// different paper; side by side sit versions 1 and 2 (or 3 and 4), which share no part at all. In one row
/// (<c>seatsPerRow</c> null) the versions simply run 1, 2, 3, 4, 1, …
/// </summary>
public static class SeatPattern
{
    public static int VersionFor(int seat, int? seatsPerRow)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seat, 1);
        if (seatsPerRow is not { } perRow)
        {
            return (seat - 1) % PaperMixPlan.Versions + 1;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(perRow, 1);
        var (row, column) = Position(seat, perRow);
        return 1 + row % 2 * 2 + column % 2;
    }

    /// <summary>Zero-based row and column of a seat.</summary>
    public static (int Row, int Column) Position(int seat, int seatsPerRow) => ((seat - 1) / seatsPerRow, (seat - 1) % seatsPerRow);

    /// <summary>A seat number from what staff typed ("12", " 7 "); null when it is not a positive whole number.</summary>
    public static int? Parse(string? seatNo) =>
        int.TryParse(seatNo?.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var seat) && seat > 0
            ? seat
            : null;
}
