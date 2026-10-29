namespace ExamPrep.Domain.Papers;

/// <summary>How many papers a mix gives and from how many sources (<see cref="PaperMixPlan"/>).</summary>
public readonly record struct MixShape(int Versions, int Sources);

/// <summary>
/// Which mixed paper a computer gets (Victory 20/10: neighbouring computers never sit the same paper). Seats are
/// numbered from 1 row by row, <c>seatsPerRow</c> to a row.
/// <para>
/// Each row takes one family of versions (<see cref="PaperMixPlan.Families"/>) in turn, and the seats of a row run
/// through that family: side by side sit two papers that share no part at all, and the rows in front and behind
/// take other families. Two sources and four versions give the 2×2 tiles <c>[1 2 / 3 4]</c>. When the versions form
/// a single family, or a family of one paper, version (column + 2 × row) mod K is used: the computers left, right, in
/// front, behind and diagonally next to any computer still all have a different paper. In one row
/// (<c>seatsPerRow</c> null) the versions simply run 1, 2, …, K, 1, …
/// </para>
/// </summary>
public static class SeatPattern
{
    public static int VersionFor(int seat, int? seatsPerRow, MixShape shape)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seat, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(shape.Versions, PaperMixPlan.MinVersions);
        if (seatsPerRow is not { } perRow)
        {
            return (seat - 1) % shape.Versions + 1;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(perRow, 1);
        var (row, column) = Position(seat, perRow);
        var families = PaperMixPlan.Families(shape.Versions, shape.Sources);
        if (families.Count >= 2 && families.All(f => f.Size >= 2))
        {
            var (first, size) = families[row % families.Count];
            return first + column % size;
        }

        return (column + 2 * row) % shape.Versions + 1;
    }

    /// <summary>Zero-based row and column of a seat.</summary>
    public static (int Row, int Column) Position(int seat, int seatsPerRow) => ((seat - 1) / seatsPerRow, (seat - 1) % seatsPerRow);

    /// <summary>A seat number from what staff typed ("12", " 7 "); null when it is not a positive whole number.</summary>
    public static int? Parse(string? seatNo) =>
        int.TryParse(seatNo?.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var seat) && seat > 0
            ? seat
            : null;
}
