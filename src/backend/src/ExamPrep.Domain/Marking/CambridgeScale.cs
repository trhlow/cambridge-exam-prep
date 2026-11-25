namespace ExamPrep.Domain.Marking;

/// <summary>
/// Raw marks to Cambridge English Scale scores, per skill, with the overall score, grade and CEFR level. The anchors are
/// Cambridge's conversion tables for exams from 2020 ("Converting practice test scores to Cambridge English Scale
/// scores", CER/4240/V2/AUG19); between two anchors the score is read off the straight line joining them, and full
/// marks give the top of the exam's range. Cambridge calls these figures indicative for practice tests: the result
/// tells a centre how ready a student is, not what the live exam will give.
/// </summary>
public static class CambridgeScale
{
    public const string Reading = "Reading";
    public const string Writing = "Writing";
    public const string Listening = "Listening";
    public const string Speaking = "Speaking";

    /// <summary>The four skills of a result, in Cambridge's order.</summary>
    public static readonly IReadOnlyList<string> Skills = [Reading, Writing, Listening, Speaking];

    /// <param name="MaxRaw">The paper's marks in Cambridge's own paper; a centre's paper with other points is scaled to it.</param>
    /// <param name="Anchors">Raw mark → scale score, lowest first; the lowest is the minimum score reported.</param>
    private sealed record Table(decimal MaxRaw, int TopScale, (decimal Raw, int Scale)[] Anchors);

    /// <param name="From">The lowest overall score of the band.</param>
    public sealed record Band(int From, string Grade, string Cefr);

    private sealed record ExamScale(IReadOnlyDictionary<string, Table> Tables, Band[] Bands);

    /// <summary>By exam type code (<c>exam_types.code</c>).</summary>
    private static readonly Dictionary<string, ExamScale> Exams = new()
    {
        ["A2_KEY_FS"] = new ExamScale(
            new Dictionary<string, Table>
            {
                [Reading] = new(30, 150, [(7, 82), (13, 100), (20, 120), (28, 140)]),
                [Writing] = new(30, 150, [(8, 82), (12, 100), (18, 120), (26, 140)]),
                [Listening] = new(25, 150, [(6, 82), (11, 100), (17, 120), (23, 140)]),
                [Speaking] = new(45, 150, [(10, 82), (18, 100), (27, 120), (41, 140)]),
            },
            [new(140, "A", "B1"), new(133, "B", "A2"), new(120, "C", "A2"), new(100, "Level A1", "A1")]),
        ["B1_PRELIM_FS"] = new ExamScale(
            new Dictionary<string, Table>
            {
                [Reading] = new(32, 170, [(5, 102), (13, 120), (23, 140), (29, 160)]),
                [Writing] = new(40, 170, [(10, 102), (16, 120), (24, 140), (34, 160)]),
                [Listening] = new(25, 170, [(5, 102), (11, 120), (18, 140), (23, 160)]),
                [Speaking] = new(30, 170, [(7, 102), (12, 120), (18, 140), (27, 160)]),
            },
            [new(160, "A", "B2"), new(153, "B", "B1"), new(140, "C", "B1"), new(120, "Level A2", "A2")]),
    };

    /// <summary>Whether results of this exam type can be put on the scale.</summary>
    public static bool Covers(string examTypeCode) => Exams.ContainsKey(examTypeCode);

    /// <summary>The most Speaking marks the examiners can give (A2 Key 45, B1 Preliminary 30); null for another exam type.</summary>
    public static decimal? SpeakingMax(string examTypeCode) => Exams.TryGetValue(examTypeCode, out var exam) ? exam.Tables[Speaking].MaxRaw : null;

    /// <summary>
    /// The scale score of <paramref name="raw"/> out of <paramref name="max"/> in one skill; null when the exam type has
    /// no table, the paper has no marks, or the score is below the minimum Cambridge reports.
    /// </summary>
    public static int? Score(string examTypeCode, string skill, decimal raw, decimal max)
    {
        if (!Exams.TryGetValue(examTypeCode, out var exam) || !exam.Tables.TryGetValue(skill, out var table) || max <= 0)
        {
            return null;
        }

        var marks = Math.Clamp(raw, 0, max) * table.MaxRaw / max;
        (decimal Raw, int Scale)[] points = [.. table.Anchors, (table.MaxRaw, table.TopScale)];
        if (marks < points[0].Raw)
        {
            return null;
        }

        var upper = Array.FindIndex(points, p => p.Raw >= marks);
        if (points[upper].Raw == marks)
        {
            return points[upper].Scale;
        }

        var (r0, s0) = points[upper - 1];
        var (r1, s1) = points[upper];
        return (int)Math.Round(s0 + (marks - r0) * (s1 - s0) / (r1 - r0), MidpointRounding.AwayFromZero);
    }

    /// <summary>The overall score: the four skill scores added up and divided by four, rounded to the nearest whole number.</summary>
    public static int Overall(IReadOnlyCollection<int> skillScores) =>
        (int)Math.Round(skillScores.Sum() / 4m, MidpointRounding.AwayFromZero);

    /// <summary>The grade and CEFR level of an overall score; null below the exam's lowest band (no result at this level).</summary>
    public static Band? GradeFor(string examTypeCode, int overall) =>
        Exams.TryGetValue(examTypeCode, out var exam) ? exam.Bands.FirstOrDefault(b => overall >= b.From) : null;
}
