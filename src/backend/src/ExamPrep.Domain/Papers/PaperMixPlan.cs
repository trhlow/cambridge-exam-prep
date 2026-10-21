using ExamPrep.Domain.Common;

namespace ExamPrep.Domain.Papers;

/// <summary>
/// Which source paper each part of the four mixed papers comes from (Victory 20/10). A pattern such as "ABBAABA" reads
/// part by part. Versions 1 and 2 are opposites, and so are versions 3 and 4: every part is taken from source A by two
/// versions and from source B by the other two, and the two papers of a pair share no part at all — they go side by
/// side in the room. All four patterns differ; every version mixes both sources once the section has three parts
/// or more (two-part sections fall back to the two sources whole as versions 3 and 4).
/// </summary>
public static class PaperMixPlan
{
    public const int Versions = 4;

    /// <summary>Longest supported section: the pattern is kept in a 16-character column.</summary>
    public const int MaxParts = 16;

    /// <summary>The four patterns in version order; the same part count and seed always give the same patterns.</summary>
    public static IReadOnlyList<string> Patterns(int partCount, int seed)
    {
        if (partCount < 2)
        {
            throw new DomainRuleException("A paper needs at least two parts to be mixed.");
        }

        if (partCount > MaxParts)
        {
            throw new DomainRuleException($"A paper can be mixed only when it has at most {MaxParts} parts.");
        }

        var all = (1 << partCount) - 1;
        var random = new Random(seed);

        // Masks with at least one part from each source; bit i set = part i+1 from source B.
        var mixed = Enumerable.Range(1, all - 1).ToList();
        var first = mixed[random.Next(mixed.Count)];
        var others = mixed.Where(m => m != first && m != (all ^ first)).ToList();
        var second = others.Count > 0 ? others[random.Next(others.Count)] : 0;

        return [Pattern(first, partCount), Pattern(all ^ first, partCount), Pattern(second, partCount), Pattern(all ^ second, partCount)];
    }

    /// <summary>True when part <paramref name="partNo"/> (1-based) of the pattern comes from source B.</summary>
    public static bool FromSourceB(string pattern, int partNo) => pattern[partNo - 1] == 'B';

    private static string Pattern(int mask, int partCount) =>
        string.Concat(Enumerable.Range(0, partCount).Select(i => (mask & (1 << i)) != 0 ? 'B' : 'A'));
}
