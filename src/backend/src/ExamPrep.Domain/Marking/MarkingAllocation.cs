namespace ExamPrep.Domain.Marking;

/// <summary>
/// VE-73 (Victory 29/10): who marks whose Writing. The candidates still to be dealt are shuffled and each goes to the
/// examiner of the session with the fewest scripts so far (ties broken at random), so the shares never differ by more
/// than one — 20 candidates and 4 examiners give 5 each. Examiners see a script by its code, never by name.
/// </summary>
public static class MarkingAllocation
{
    public const int CodeLength = 6;

    /// <summary>Letters and digits that cannot be misread for one another (no I, O, 0, 1).</summary>
    public const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>The examiner for each candidate to deal; <paramref name="kept"/> counts the scripts each examiner keeps.</summary>
    public static IReadOnlyDictionary<int, int> Deal(IReadOnlyList<int> candidates, IReadOnlyList<int> examiners, IReadOnlyDictionary<int, int> kept, Random random)
    {
        var dealt = new Dictionary<int, int>();
        if (examiners.Count == 0)
        {
            return dealt;
        }

        var load = examiners.ToDictionary(e => e, e => kept.GetValueOrDefault(e));
        foreach (var candidate in candidates.OrderBy(_ => random.Next()))
        {
            var fewest = load.Values.Min();
            var choices = load.Where(p => p.Value == fewest).Select(p => p.Key).ToList();
            var examiner = choices[random.Next(choices.Count)];
            dealt[candidate] = examiner;
            load[examiner]++;
        }

        return dealt;
    }

    /// <summary>A new script code not yet in <paramref name="taken"/> (the codes of the session).</summary>
    public static string NewCode(Random random, IReadOnlySet<string> taken)
    {
        while (true)
        {
            var code = string.Concat(Enumerable.Range(0, CodeLength).Select(_ => CodeAlphabet[random.Next(CodeAlphabet.Length)]));
            if (!taken.Contains(code))
            {
                return code;
            }
        }
    }
}
