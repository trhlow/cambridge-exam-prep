using System.Text;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Marking;

/// <summary>
/// Decides whether a response matches the answer key. Several keys are accepted alternatives. Choices compare
/// option keys; typed answers forgive case (unless the key is case-sensitive), surrounding and repeated spaces,
/// curly apostrophes and a final full stop — but not spelling, which Cambridge marks as wrong.
/// </summary>
public static class AnswerMatcher
{
    /// <summary>Writing (Essay) has no key and is marked by a teacher.</summary>
    public static bool CanAutoMark(InteractionType type) => type != InteractionType.Essay;

    public static bool IsCorrect(InteractionType type, string? response, IReadOnlyCollection<AnswerKey> keys)
    {
        if (!CanAutoMark(type) || string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        if (type != InteractionType.InlineText)
        {
            var chosen = response.Trim();
            return keys.Any(k => string.Equals(k.Value.Trim(), chosen, StringComparison.OrdinalIgnoreCase));
        }

        var typed = Normalise(response);
        return keys.Any(k => string.Equals(
            Normalise(k.Value),
            typed,
            k.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalise(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var c in text.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            builder.Append(c is '’' or '‘' or '`' ? '\'' : c);
            lastWasSpace = false;
        }

        return builder.ToString().TrimEnd('.');
    }
}
