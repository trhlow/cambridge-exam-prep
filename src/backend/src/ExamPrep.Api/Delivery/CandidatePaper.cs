using System.Text.Json;
using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Content;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// What the candidate screen needs to show one section: parts, instructions, passages and questions.
/// Answer keys are deliberately absent from every type here.
/// </summary>
public sealed record CandidatePaper(int SectionId, string Title, int DurationMinutes, IReadOnlyList<CandidatePart> Parts);

public sealed record CandidatePart(int PartNo, string? Title, string Layout, string? StimulusHtml, string? AudioUrl, IReadOnlyList<CandidateGroup> Groups, bool QuestionPerPage = false);

public sealed record CandidateGroup(
    string InteractionType,
    string Instructions,
    string? BodyHtml,
    IReadOnlyList<CandidateOption>? OptionPool,
    bool AllowOptionReuse,
    int? MaxWordsPerAnswer,
    IReadOnlyList<CandidateQuestion> Questions,
    string? ItemsHeading = null,
    string? PoolHeading = null);

public sealed record CandidateQuestion(int Id, int Number, string? PromptHtml, string? StimulusHtml, IReadOnlyList<CandidateOption>? Options, int? MinWords);

/// <param name="Title">A card's heading, shown in bold before its text.</param>
public sealed record CandidateOption(string Key, string? Text, string? ImageUrl, string? Title = null);

internal static class CandidatePaperMapper
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record StoredOption(string Key, string? Text, int? ImageAssetId, string? Title = null);

    public static CandidatePaper From(ExamSection section, TestPaper paper) => new(
        section.Id,
        section.SectionTemplate!.Title,
        section.DurationMinutes,
        paper.Parts.OrderBy(p => p.PartNo).Select(part => new CandidatePart(
            part.PartNo,
            part.Title,
            part.Layout.ToString(),
            part.StimulusHtml,
            part.AudioAssetId is { } audio ? AssetUrl(audio) : null,
            part.Groups.OrderBy(g => g.OrderNo).Select(group => new CandidateGroup(
                group.InteractionType.ToString(),
                group.Instructions,
                group.BodyHtml,
                Options(group.OptionPoolJson),
                group.AllowOptionReuse,
                group.MaxWordsPerAnswer,
                group.Questions.OrderBy(q => q.OrderNo).Select(q => new CandidateQuestion(
                    q.Id, q.Number, q.PromptHtml, q.StimulusHtml, Options(q.OptionsJson), q.MinWords)).ToList(),
                group.ItemsHeading,
                group.PoolHeading)).ToList(),
            part.QuestionPerPage))
            .ToList());

    public static string AssetUrl(int assetId) => $"{PaperPackageImporter.AssetUrlPrefix}{assetId}";

    public static List<CandidateOption>? Options(string? json) =>
        json is null
            ? null
            : JsonSerializer.Deserialize<List<StoredOption>>(json, Json)!
                .Select(o => new CandidateOption(o.Key, o.Text, o.ImageAssetId is { } id ? AssetUrl(id) : null, o.Title))
                .ToList();
}
