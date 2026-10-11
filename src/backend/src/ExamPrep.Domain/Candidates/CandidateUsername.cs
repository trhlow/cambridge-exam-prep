using System.Text;

namespace ExamPrep.Domain.Candidates;

/// <summary>
/// Cambridge login usernames: the first 5 letters of the family name, the first letter of the given name and the
/// candidate number, in lower case without accents (Nguyễn An 0001 → nguyea0001).
/// </summary>
public static class CandidateUsername
{
    private const int FamilyLetters = 5;

    public static string For(string familyName, string givenName, string candidateNumber)
    {
        var family = Letters(familyName);
        var given = Letters(givenName);
        if (family.Length == 0 || given.Length == 0)
        {
            throw new ArgumentException("The family and given names must contain letters.");
        }

        var number = new string(candidateNumber.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return family[..Math.Min(FamilyLetters, family.Length)] + given[0] + number;
    }

    /// <summary>Lower-case ASCII letters only: accents dropped, Vietnamese đ read as d, everything else removed.</summary>
    private static string Letters(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD))
        {
            // After FormD the accents are separate marks; they and any letter with no ASCII form are left out.
            if (char.IsAsciiLetter(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
