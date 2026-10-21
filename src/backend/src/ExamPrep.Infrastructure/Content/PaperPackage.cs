using ExamPrep.Domain.Enums;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// An exam package: <c>paper.json</c> describing one paper (one section of an exam type) plus the pictures and
/// audio tracks it references by relative path. The format is documented in <c>setup/exam-package/FORMAT.md</c>.
/// </summary>
public sealed record PaperPackage(PackagePaper Paper, IReadOnlyDictionary<string, byte[]> Files);

public sealed record PaperPackageReadResult(PaperPackage? Package, IReadOnlyList<string> Errors);

public sealed record PackagePaper(
    int FormatVersion,
    string ExamType,
    int Section,
    string Title,
    IReadOnlyList<PackagePart> Parts);

public sealed record PackagePart(
    int PartNo,
    IReadOnlyList<PackageGroup> Groups,
    PartLayout Layout = PartLayout.Single,
    string? Title = null,
    string? StimulusHtml = null,
    string? Audio = null);

public sealed record PackageGroup(
    InteractionType InteractionType,
    string Instructions,
    IReadOnlyList<PackageQuestion> Questions,
    string? BodyHtml = null,
    IReadOnlyList<PackageOption>? OptionPool = null,
    bool AllowOptionReuse = false,
    int? MaxWordsPerAnswer = null,
    string? ItemsHeading = null,
    string? PoolHeading = null);

public sealed record PackageQuestion(
    int Number,
    string? PromptHtml = null,
    string? StimulusHtml = null,
    IReadOnlyList<PackageOption>? Options = null,
    IReadOnlyList<string>? Answers = null,
    decimal Points = 1,
    int? MinWords = null);

/// <param name="Title">A card's heading, shown in bold before its text (B1 Reading Part 2: the course or place).</param>
public sealed record PackageOption(string Key, string? Text = null, string? Image = null, string? Title = null);
