using ExamPrep.Domain.Common;

namespace ExamPrep.Domain.Papers;

/// <summary>
/// Which source paper each part of the mixed papers comes from (Victory 20/10, widened 29/10): N source papers of the
/// same layout become K papers, K chosen by the papers manager between 4 and 3N. A pattern such as "ACBBA" reads part
/// by part, A being the first source.
/// <para>
/// The K versions are split into families of even size, at most N each (<see cref="Families"/>). A family is one
/// pattern turned round the sources: its j-th version takes part p from source (b[p] + j) mod N, so two versions of
/// one family share no part at all — they go side by side in the room (<see cref="SeatPattern"/>). Each family starts
/// from a different pattern b, chosen at random from the seed among the patterns that mix sources; only when the
/// section has too few parts for that does a family take the sources whole.
/// </para>
/// </summary>
public static class PaperMixPlan
{
    public const int MinSources = 2;

    public const int MaxSources = 6;

    /// <summary>The fewest versions that keep all eight computers round a computer on other papers.</summary>
    public const int MinVersions = 4;

    /// <summary>Victory 29/10: at most three versions per source paper.</summary>
    public const int VersionsPerSource = 3;

    /// <summary>Longest supported section: the pattern is kept in a 16-character column.</summary>
    public const int MaxParts = 16;

    /// <summary>The most versions N sources of this many parts can give: 3N, or every pattern there is when fewer.</summary>
    public static int MaxVersions(int sources, int parts) => (int)Math.Min(VersionsPerSource * sources, Power(sources, parts, VersionsPerSource * sources));

    /// <summary>The versions in families of even size, at most one per source: (first version, size), in version order.</summary>
    public static IReadOnlyList<(int First, int Size)> Families(int versions, int sources)
    {
        var count = (versions + sources - 1) / sources;
        var families = new List<(int First, int Size)>();
        var first = 1;
        for (var f = 0; f < count; f++)
        {
            var size = versions / count + (f < versions % count ? 1 : 0);
            families.Add((first, size));
            first += size;
        }

        return families;
    }

    /// <summary>The patterns in version order; the same shape and seed always give the same patterns.</summary>
    public static IReadOnlyList<string> Patterns(int sources, int parts, int versions, int seed)
    {
        if (sources is < MinSources or > MaxSources)
        {
            throw new DomainRuleException($"Choose between {MinSources} and {MaxSources} source papers.");
        }

        if (parts < 2)
        {
            throw new DomainRuleException("A paper needs at least two parts to be mixed.");
        }

        if (parts > MaxParts)
        {
            throw new DomainRuleException($"A paper can be mixed only when it has at most {MaxParts} parts.");
        }

        var max = MaxVersions(sources, parts);
        if (versions < MinVersions || versions > max)
        {
            throw new DomainRuleException($"{sources} source papers of {parts} parts can make between {MinVersions} and {max} papers.");
        }

        var families = Families(versions, sources);
        var starts = Starts(sources, parts, families.Count, new Random(seed));
        var patterns = new List<string>(versions);
        for (var f = 0; f < families.Count; f++)
        {
            for (var j = 0; j < families[f].Size; j++)
            {
                patterns.Add(string.Concat(starts[f].Select(source => (char)('A' + (source + j) % sources))));
            }
        }

        return patterns;
    }

    /// <summary>The source (0 = A) part <paramref name="partNo"/> (1-based) of the pattern comes from.</summary>
    public static int SourceOf(string pattern, int partNo) => pattern[partNo - 1] - 'A';

    /// <summary>
    /// One starting pattern per family, all different even when turned round the sources: each starts with source A
    /// (a turn of it is the same family). Patterns that mix sources come first; the one taking source A whole is the
    /// last resort.
    /// </summary>
    private static List<int[]> Starts(int sources, int parts, int count, Random random)
    {
        var mixing = Power(sources, parts - 1, int.MaxValue) - 1;
        if (mixing <= 64)
        {
            var all = Enumerable.Range(1, (int)mixing).Select(n => Digits(n, sources, parts)).OrderBy(_ => random.Next()).ToList();
            all.Add(new int[parts]);
            return all.Take(count).ToList();
        }

        var chosen = new List<int[]>();
        while (chosen.Count < count)
        {
            var start = new int[parts];
            for (var p = 1; p < parts; p++)
            {
                start[p] = random.Next(sources);
            }

            if (start.Any(s => s != 0) && !chosen.Any(c => c.SequenceEqual(start)))
            {
                chosen.Add(start);
            }
        }

        return chosen;
    }

    /// <summary>n written in base <paramref name="sources"/> over parts 2..P (part 1 stays source A).</summary>
    private static int[] Digits(int n, int sources, int parts)
    {
        var digits = new int[parts];
        for (var p = parts - 1; p >= 1; p--)
        {
            digits[p] = n % sources;
            n /= sources;
        }

        return digits;
    }

    /// <summary>b to the power e, or <paramref name="cap"/> once it grows past it.</summary>
    private static long Power(int b, int e, long cap)
    {
        long result = 1;
        for (var i = 0; i < e && result <= cap; i++)
        {
            result *= b;
        }

        return Math.Min(result, cap);
    }
}
