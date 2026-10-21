using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Marking;

/// <summary>
/// "Choose one of these questions" (B1 Writing Part 2, <c>question_groups.choose_count</c>): which questions of a
/// group are marked and what the group is worth. The questions the candidate set to Yes count; when fewer were
/// chosen than the group allows, written answers left Undecided fill the rest in question order, so a candidate who
/// forgot to choose is still marked. A question set to No is never marked.
/// </summary>
public static class ChosenQuestions
{
    public sealed record Answer(int Number, QuestionSelection Selection, bool IsWritten);

    /// <summary>The question numbers that are marked; every question when the group has no choice.</summary>
    public static IReadOnlySet<int> Counted(int? chooseCount, IEnumerable<Answer> answers)
    {
        var all = answers.OrderBy(a => a.Number).ToList();
        if (chooseCount is not { } count)
        {
            return all.Select(a => a.Number).ToHashSet();
        }

        var counted = all.Where(a => a.Selection == QuestionSelection.Yes).Take(count).Select(a => a.Number).ToList();
        counted.AddRange(all.Where(a => a.Selection == QuestionSelection.Undecided && a.IsWritten).Take(count - counted.Count).Select(a => a.Number));
        return counted.ToHashSet();
    }

    /// <summary>What the group is worth: its highest <paramref name="chooseCount"/> question points, or all of them.</summary>
    public static decimal MaxPoints(int? chooseCount, IEnumerable<decimal> points) =>
        chooseCount is { } count ? points.OrderDescending().Take(count).Sum() : points.Sum();
}
