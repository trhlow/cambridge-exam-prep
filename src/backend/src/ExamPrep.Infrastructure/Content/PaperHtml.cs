using System.Text.RegularExpressions;
using Ganss.Xss;

namespace ExamPrep.Infrastructure.Content;

/// <summary>Exam HTML rules shared by the importer and the web editor (VE-25): sanitising and inline gaps.</summary>
public static partial class PaperHtml
{
    private static readonly HtmlSanitizer Sanitizer = new();

    /// <summary>Removes scripts, event handlers and other unsafe markup; null and blank stay null.</summary>
    public static string? Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? null : Sanitizer.Sanitize(html);

    /// <summary>Question numbers of the <c>{{q:19}}</c> gaps, in order of appearance.</summary>
    public static IReadOnlyList<int> Gaps(string? html) =>
        html is null ? [] : Gap().Matches(html).Select(m => int.Parse(m.Groups[1].Value)).ToList();

    /// <summary>Every gap of an inline group must appear exactly once and belong to the group.</summary>
    /// <summary>True when the text has at least one {{q:n}} gap (B1 Reading Part 4 drags sentences into them).</summary>
    public static bool HasGaps(string? html) => Gaps(html).Count > 0;

    public static IEnumerable<string> GapProblems(string? html, IReadOnlyCollection<int> numbers, string label)
    {
        var gaps = Gaps(html);
        foreach (var number in numbers.Where(n => !gaps.Contains(n)))
        {
            yield return $"{label}: gap {{{{q:{number}}}}} is missing from the text.";
        }

        foreach (var gap in gaps.Distinct())
        {
            if (!numbers.Contains(gap))
            {
                yield return $"{label}: gap {{{{q:{gap}}}}} does not match any question in this group.";
            }
            else if (gaps.Count(g => g == gap) > 1)
            {
                yield return $"{label}: gap {{{{q:{gap}}}}} appears more than once.";
            }
        }
    }

    [GeneratedRegex(@"\{\{q:(\d+)\}\}")]
    private static partial Regex Gap();
}
