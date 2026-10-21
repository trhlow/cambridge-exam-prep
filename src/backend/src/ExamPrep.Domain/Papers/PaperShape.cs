using ExamPrep.Domain.Entities;

namespace ExamPrep.Domain.Papers;

/// <summary>
/// Whether two papers have the same layout, so that any part of one can stand in for the same part of the other
/// (Victory 20/10, paper shuffling): same section, same parts, and in each part the same groups with the same
/// way of answering and the same question numbers; a Listening part has a recording in both or in neither.
/// </summary>
public static class PaperShape
{
    /// <summary>Every difference as a sentence for the administrator; empty when the papers match. Needs parts, groups and questions loaded.</summary>
    public static IReadOnlyList<string> Differences(TestPaper a, TestPaper b)
    {
        if (a.Id != 0 && a.Id == b.Id)
        {
            return ["Choose two different papers."];
        }

        if (a.SectionTemplateId != b.SectionTemplateId)
        {
            return [$"The papers are for different parts of the test: \"{a.Title}\" is {TemplateName(a)}, \"{b.Title}\" is {TemplateName(b)}."];
        }

        var partsA = a.Parts.OrderBy(p => p.PartNo).ToList();
        var partsB = b.Parts.OrderBy(p => p.PartNo).ToList();
        if (partsA.Count != partsB.Count)
        {
            return [$"\"{a.Title}\" has {partsA.Count} parts and \"{b.Title}\" has {partsB.Count}."];
        }

        var problems = new List<string>();
        foreach (var (partA, partB) in partsA.Zip(partsB))
        {
            var groupsA = partA.Groups.OrderBy(g => g.OrderNo).ToList();
            var groupsB = partB.Groups.OrderBy(g => g.OrderNo).ToList();
            if (groupsA.Count != groupsB.Count)
            {
                problems.Add($"Part {partA.PartNo} has {groupsA.Count} question group(s) in \"{a.Title}\" and {groupsB.Count} in \"{b.Title}\".");
                continue;
            }

            foreach (var (groupA, groupB) in groupsA.Zip(groupsB))
            {
                var numbersA = Numbers(groupA);
                var numbersB = Numbers(groupB);
                if (groupA.InteractionType != groupB.InteractionType)
                {
                    problems.Add($"Part {partA.PartNo}, questions {Range(numbersA)}: answered as {groupA.InteractionType} in \"{a.Title}\" but as {groupB.InteractionType} in \"{b.Title}\".");
                }
                else if (!numbersA.SequenceEqual(numbersB))
                {
                    problems.Add($"Part {partA.PartNo} has questions {Range(numbersA)} in \"{a.Title}\" but {Range(numbersB)} in \"{b.Title}\".");
                }
                else if (groupA.ChooseCount != groupB.ChooseCount)
                {
                    problems.Add($"Part {partA.PartNo}, questions {Range(numbersA)}: candidates answer {Choice(groupA)} in \"{a.Title}\" but {Choice(groupB)} in \"{b.Title}\".");
                }
            }

            if ((partA.AudioAssetId is null) != (partB.AudioAssetId is null))
            {
                var (with, without) = partA.AudioAssetId is null ? (b.Title, a.Title) : (a.Title, b.Title);
                problems.Add($"Part {partA.PartNo} has a recording in \"{with}\" but not in \"{without}\".");
            }
        }

        return problems;
    }

    private static string Choice(QuestionGroup group) => group.ChooseCount is { } n ? $"{n} of them" : "all of them";

    private static List<int> Numbers(QuestionGroup group) => group.Questions.Select(q => q.Number).Order().ToList();

    private static string Range(List<int> numbers) => numbers.Count switch
    {
        0 => "(none)",
        1 => numbers[0].ToString(),
        _ => $"{numbers[0]}–{numbers[^1]}",
    };

    private static string TemplateName(TestPaper paper) => paper.SectionTemplate?.Title ?? $"section template {paper.SectionTemplateId}";
}
