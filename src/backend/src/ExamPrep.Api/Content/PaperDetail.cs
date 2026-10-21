using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Entities;

namespace ExamPrep.Api.Content;

/// <summary>
/// A paper as the administrator reviews it before publishing: everything the candidate sees, plus the answer key.
/// Administrators only — invigilators are in the room while a paper is live.
/// </summary>
/// <param name="MixPattern">A mixed paper (VE-65): which source each part came from, e.g. "ABBAABA".</param>
public sealed record PaperDetail(int Id, string Title, string ExamType, string Section, string Status, IReadOnlyList<PaperDetailPart> Parts, int? PaperMixId = null, string? MixTitle = null, int? MixVersion = null, string? MixPattern = null);

public sealed record PaperDetailPart(int PartNo, string? Title, string Layout, string? StimulusHtml, string? AudioUrl, IReadOnlyList<PaperDetailGroup> Groups, bool QuestionPerPage = false);

public sealed record PaperDetailGroup(int OrderNo, string InteractionType, string Instructions, string? BodyHtml, IReadOnlyList<CandidateOption>? OptionPool, IReadOnlyList<PaperDetailQuestion> Questions, string? ItemsHeading = null, string? PoolHeading = null, int? ChooseCount = null);

public sealed record PaperDetailQuestion(int Number, string? PromptHtml, string? StimulusHtml, IReadOnlyList<CandidateOption>? Options, IReadOnlyList<string> Answers, decimal Points, int? MinWords);

internal static class PaperDetailMapper
{
    public static PaperDetail From(TestPaper paper) => new(
        paper.Id,
        paper.Title,
        paper.SectionTemplate!.ExamType!.Name,
        paper.SectionTemplate.Title,
        paper.Status.ToString(),
        paper.Parts.OrderBy(p => p.PartNo).Select(part => new PaperDetailPart(
            part.PartNo,
            part.Title,
            part.Layout.ToString(),
            part.StimulusHtml,
            part.AudioAssetId is { } audio ? CandidatePaperMapper.AssetUrl(audio) : null,
            part.Groups.OrderBy(g => g.OrderNo).Select(group => new PaperDetailGroup(
                group.OrderNo,
                group.InteractionType.ToString(),
                group.Instructions,
                group.BodyHtml,
                CandidatePaperMapper.Options(group.OptionPoolJson),
                group.Questions.OrderBy(q => q.OrderNo).Select(q => new PaperDetailQuestion(
                    q.Number,
                    q.PromptHtml,
                    q.StimulusHtml,
                    CandidatePaperMapper.Options(q.OptionsJson),
                    q.AnswerKeys.OrderBy(k => k.Id).Select(k => k.Value).ToList(),
                    q.Points,
                    q.MinWords)).ToList(),
                group.ItemsHeading,
                group.PoolHeading,
                group.ChooseCount)).ToList(),
            part.QuestionPerPage))
            .ToList(),
        paper.PaperMixId,
        paper.PaperMix?.Title,
        paper.MixVersion,
        paper.MixPattern);
}
