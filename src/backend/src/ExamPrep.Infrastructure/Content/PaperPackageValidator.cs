using System.Text.RegularExpressions;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// Checks a package against its section template and the rules of each interaction type. Every problem is
/// reported (not just the first), worded for the administrator who prepared the package.
/// </summary>
public static partial class PaperPackageValidator
{
    public const int SupportedFormatVersion = 1;
    private const int MaxTitleLength = 256;
    private const int MaxInstructionsLength = 1000;
    private const int MaxHeadingLength = 128;
    private const int MaxAnswerLength = 255;

    public static IReadOnlyList<string> Validate(PaperPackage package, SectionTemplate template)
    {
        var errors = new List<string>();
        var paper = package.Paper;

        if (paper.FormatVersion != SupportedFormatVersion)
        {
            errors.Add($"formatVersion {paper.FormatVersion} is not supported; use {SupportedFormatVersion}.");
        }

        if (string.IsNullOrWhiteSpace(paper.Title) || paper.Title.Length > MaxTitleLength)
        {
            errors.Add($"The paper needs a title of at most {MaxTitleLength} characters.");
        }

        if (paper.Parts.Count != template.PartCount)
        {
            errors.Add($"Expected {template.PartCount} parts for {template.Title}, found {paper.Parts.Count}.");
        }

        if (!paper.Parts.Select(p => p.PartNo).SequenceEqual(Enumerable.Range(1, paper.Parts.Count)))
        {
            errors.Add("Parts must be numbered 1, 2, 3… in order.");
        }

        var isListening = template.Skill == Skill.Listening;
        foreach (var part in paper.Parts)
        {
            ValidatePart(part, isListening, package.Files, errors);
        }

        ValidateNumbering(paper, errors);
        return errors;
    }

    private static void ValidatePart(PackagePart part, bool isListening, IReadOnlyDictionary<string, byte[]> files, List<string> errors)
    {
        var label = $"Part {part.PartNo}";
        if (part.Title?.Length > MaxTitleLength)
        {
            errors.Add($"{label}: the title is longer than {MaxTitleLength} characters.");
        }

        CheckHtmlImages(part.StimulusHtml, label, files, errors);

        if (isListening && part.Audio is null)
        {
            errors.Add($"{label}: Listening parts need an audio track.");
        }
        else if (!isListening && part.Audio is not null)
        {
            errors.Add($"{label}: only Listening parts can have audio.");
        }
        else if (part.Audio is not null)
        {
            CheckFile(part.Audio, AssetKind.Audio, label, files, errors);
        }

        if (part.Groups.Count == 0)
        {
            errors.Add($"{label}: at least one group of questions is required.");
        }

        for (var i = 0; i < part.Groups.Count; i++)
        {
            var groupLabel = part.Groups.Count > 1 ? $"{label}, group {i + 1}" : label;
            ValidateGroup(part.Groups[i], groupLabel, label, files, errors);
        }
    }

    private static void ValidateGroup(PackageGroup group, string groupLabel, string partLabel, IReadOnlyDictionary<string, byte[]> files, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(group.Instructions) || group.Instructions.Length > MaxInstructionsLength)
        {
            errors.Add($"{groupLabel}: instructions are required (at most {MaxInstructionsLength} characters).");
        }

        if (group.Questions.Count == 0)
        {
            errors.Add($"{groupLabel}: at least one question is required.");
        }

        CheckHtmlImages(group.BodyHtml, groupLabel, files, errors);

        if (group.InteractionType is InteractionType.InlineChoice or InteractionType.InlineText)
        {
            CheckGaps(group, groupLabel, errors);
        }

        if (group.InteractionType == InteractionType.DragDropMatch)
        {
            CheckOptions(group.OptionPool, needsPicture: false, groupLabel, files, errors, "drag-and-drop groups need a pool of cards");

            // Cards dragged into gaps of a text (B1 Reading Part 4) need every gap; rows of people need labels instead.
            if (HasGaps(group.BodyHtml))
            {
                CheckGaps(group, groupLabel, errors);
            }

            foreach (var (text, name) in new[] { (group.ItemsHeading, "itemsHeading"), (group.PoolHeading, "poolHeading") })
            {
                if (text?.Length > MaxHeadingLength)
                {
                    errors.Add($"{groupLabel}: {name} can be at most {MaxHeadingLength} characters.");
                }
            }
        }

        if (group.ChooseCount is { } choose)
        {
            if (group.InteractionType != InteractionType.Essay)
            {
                errors.Add($"{groupLabel}: chooseCount is only for Writing (Essay) groups.");
            }
            else if (choose < 1 || choose >= group.Questions.Count)
            {
                errors.Add($"{groupLabel}: chooseCount must be at least 1 and less than the group's {group.Questions.Count} questions.");
            }
        }

        foreach (var question in group.Questions)
        {
            ValidateQuestion(question, group, $"{partLabel}, question {question.Number}", files, errors);
        }
    }

    private static void ValidateQuestion(PackageQuestion question, PackageGroup group, string label, IReadOnlyDictionary<string, byte[]> files, List<string> errors)
    {
        CheckHtmlImages(question.PromptHtml, label, files, errors);
        CheckHtmlImages(question.StimulusHtml, label, files, errors);
        if (question.Points < 0)
        {
            errors.Add($"{label}: points cannot be negative.");
        }

        var answers = question.Answers ?? [];
        switch (group.InteractionType)
        {
            case InteractionType.SingleChoice or InteractionType.ImageChoice or InteractionType.InlineChoice:
                var needsPicture = group.InteractionType == InteractionType.ImageChoice;
                CheckOptions(question.Options, needsPicture, label, files, errors, "choice questions need at least two options");
                CheckSingleAnswer(answers, question.Options, "options", label, errors);
                break;
            case InteractionType.DragDropMatch:
                if (string.IsNullOrWhiteSpace(question.PromptHtml) && !HasGaps(group.BodyHtml))
                {
                    errors.Add($"{label}: drag-and-drop questions need a label (promptHtml), or a gap in the group's text.");
                }

                CheckSingleAnswer(answers, group.OptionPool, "cards", label, errors);
                break;
            case InteractionType.InlineText:
                if (answers.Count == 0 || answers.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"{label}: at least one accepted answer is required.");
                }

                break;
            case InteractionType.Essay:
                if (answers.Count > 0)
                {
                    errors.Add($"{label}: Essay questions are marked by a teacher and cannot have answers.");
                }

                if (question.MinWords is null or <= 0)
                {
                    errors.Add($"{label}: Essay questions need minWords.");
                }

                break;
        }

        if (answers.Any(a => a?.Length > MaxAnswerLength))
        {
            errors.Add($"{label}: answers can be at most {MaxAnswerLength} characters.");
        }
    }

    private static void CheckOptions(
        IReadOnlyList<PackageOption>? options, bool needsPicture, string label, IReadOnlyDictionary<string, byte[]> files, List<string> errors, string missingMessage)
    {
        if (options is null || options.Count < 2)
        {
            errors.Add($"{label}: {missingMessage}.");
            return;
        }

        if (options.Any(o => string.IsNullOrWhiteSpace(o.Key)) || options.Select(o => o.Key).Distinct().Count() != options.Count)
        {
            errors.Add($"{label}: every option needs its own key (A, B, C…).");
        }

        foreach (var option in options)
        {
            if (needsPicture && option.Image is null)
            {
                errors.Add($"{label}: option {option.Key} needs a picture.");
            }

            if (option.Image is not null)
            {
                CheckFile(option.Image, AssetKind.Image, label, files, errors);
            }
        }
    }

    private static void CheckSingleAnswer(IReadOnlyList<string> answers, IReadOnlyList<PackageOption>? options, string optionsName, string label, List<string> errors)
    {
        if (answers.Count != 1)
        {
            errors.Add($"{label}: choice questions need exactly one answer.");
            return;
        }

        var keys = options?.Select(o => o.Key).ToList() ?? [];
        if (keys.Count > 0 && !keys.Contains(answers[0]))
        {
            errors.Add($"{label}: answer '{answers[0]}' is not one of the {optionsName} ({string.Join(", ", keys)}).");
        }
    }

    private static bool HasGaps(string? html) => html is not null && Gap().IsMatch(html);

    private static void CheckGaps(PackageGroup group, string label, List<string> errors)
    {
        var gaps = Gap().Matches(group.BodyHtml ?? string.Empty).Select(m => int.Parse(m.Groups[1].Value)).ToList();
        var numbers = group.Questions.Select(q => q.Number).ToHashSet();

        foreach (var number in numbers.Where(n => !gaps.Contains(n)))
        {
            errors.Add($"{label}: gap {{{{q:{number}}}}} is missing from the text.");
        }

        foreach (var gap in gaps.Distinct())
        {
            if (!numbers.Contains(gap))
            {
                errors.Add($"{label}: gap {{{{q:{gap}}}}} does not match any question in this group.");
            }
            else if (gaps.Count(g => g == gap) > 1)
            {
                errors.Add($"{label}: gap {{{{q:{gap}}}}} appears more than once.");
            }
        }
    }

    /// <summary>Numbers run through the whole paper: 1, 2, 3… in the order the questions appear. Only the first break is reported.</summary>
    private static void ValidateNumbering(PackagePaper paper, List<string> errors)
    {
        var expected = 1;
        foreach (var part in paper.Parts)
        {
            foreach (var question in part.Groups.SelectMany(g => g.Questions))
            {
                if (question.Number != expected)
                {
                    errors.Add($"Question numbers must run from 1 without gaps: Part {part.PartNo} has question {question.Number} where {expected} was expected.");
                    return;
                }

                expected++;
            }
        }
    }

    private static void CheckHtmlImages(string? html, string label, IReadOnlyDictionary<string, byte[]> files, List<string> errors)
    {
        foreach (var path in PackageFileTypes.ReferencesIn(html))
        {
            CheckFile(path, AssetKind.Image, label, files, errors);
        }
    }

    private static void CheckFile(string path, AssetKind kind, string label, IReadOnlyDictionary<string, byte[]> files, List<string> errors)
    {
        var types = PackageFileTypes.For(kind);
        if (!types.ContainsKey(Path.GetExtension(path)))
        {
            var kindName = kind == AssetKind.Audio ? "audio" : "image";
            errors.Add($"{label}: '{path}' is not a supported {kindName} type ({string.Join(", ", types.Keys)}).");
        }
        else if (!files.ContainsKey(path))
        {
            errors.Add($"{label}: file '{path}' is not in the package.");
        }
    }

    [GeneratedRegex(@"\{\{q:(\d+)\}\}")]
    private static partial Regex Gap();
}
