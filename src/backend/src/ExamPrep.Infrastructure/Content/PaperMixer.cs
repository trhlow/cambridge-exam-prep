using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Papers;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Content;

public sealed record PaperMixResult(PaperMix? Mix, IReadOnlyList<string> Errors);

/// <summary>
/// Mixes 2–6 published source papers of the same layout into 4–3N published papers (Victory 20/10, widened 29/10).
/// Each version copies whole parts — passage, pictures, recording, questions and answer keys — from the source
/// <see cref="PaperMixPlan"/> chooses, so marking, Writing marks and the paper review work on them like on any paper.
/// Pictures and recordings are shared, not copied: assets are stored once by content.
/// </summary>
public sealed class PaperMixer(ExamPrepDbContext db, TimeProvider clock)
{
    public const int MaxTitleLength = 200;

    /// <param name="sourcePaperIds">The sources in letter order (A first).</param>
    /// <param name="canUse">Whether the person mixing may use this paper (VE-72: their own papers); false reads as missing.</param>
    public async Task<PaperMixResult> CreateAsync(
        IReadOnlyList<int> sourcePaperIds, int versionCount, string? title, int? seed, int userId, Func<TestPaper, bool>? canUse, CancellationToken cancellationToken)
    {
        if (sourcePaperIds.Count is < PaperMixPlan.MinSources or > PaperMixPlan.MaxSources)
        {
            return Failed($"Choose between {PaperMixPlan.MinSources} and {PaperMixPlan.MaxSources} source papers.");
        }

        if (sourcePaperIds.Distinct().Count() != sourcePaperIds.Count)
        {
            return Failed("Choose each source paper only once.");
        }

        var sources = new List<TestPaper>();
        foreach (var id in sourcePaperIds)
        {
            if (await LoadAsync(id, cancellationToken) is not { } paper || (canUse is not null && !canUse(paper)))
            {
                return Failed("Choose papers that exist.");
            }

            sources.Add(paper);
        }

        var problems = new List<string>();
        foreach (var paper in sources)
        {
            if (paper.Status != PaperStatus.Published)
            {
                problems.Add($"\"{paper.Title}\" is not published yet: publish it before mixing it.");
            }

            if (paper.PaperMixId is not null)
            {
                problems.Add($"\"{paper.Title}\" is itself a mixed paper: mix its source papers instead.");
            }
        }

        foreach (var other in sources.Skip(1))
        {
            problems.AddRange(PaperShape.Differences(sources[0], other));
        }

        if (problems.Count > 0)
        {
            return new PaperMixResult(null, problems);
        }

        var name = string.IsNullOrWhiteSpace(title) ? DefaultTitle(sources) : title.Trim();
        if (name.Length > MaxTitleLength)
        {
            return Failed($"The title can have at most {MaxTitleLength} characters.");
        }

        var partCount = sources[0].Parts.Count;
        if (partCount < 2 || partCount > PaperMixPlan.MaxParts)
        {
            return Failed($"Papers with {partCount} part(s) cannot be mixed.");
        }

        var max = PaperMixPlan.MaxVersions(sources.Count, partCount);
        if (versionCount < PaperMixPlan.MinVersions || versionCount > max)
        {
            return Failed($"{sources.Count} papers of {partCount} parts can make between {PaperMixPlan.MinVersions} and {max} mixed papers.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var mix = new PaperMix
        {
            Title = name,
            SectionTemplateId = sources[0].SectionTemplateId,
            VersionCount = versionCount,
            Seed = seed ?? Random.Shared.Next(1, int.MaxValue),
            CreatedByUserId = userId,
            CreatedAt = now,
            Sources = sources.Select((paper, i) => new PaperMixSource { Position = i + 1, TestPaperId = paper.Id }).ToList(),
        };

        var patterns = PaperMixPlan.Patterns(sources.Count, partCount, versionCount, mix.Seed);
        for (var version = 1; version <= versionCount; version++)
        {
            mix.Versions.Add(Version(sources, mix, version, patterns[version - 1], userId, now));
        }

        db.PaperMixes.Add(mix);
        await db.SaveChangesAsync(cancellationToken);
        return new PaperMixResult(mix, []);
    }

    /// <summary>"A2 Key – Test 1 × A2 Key for Schools – Test 2 × …", cut to the column size.</summary>
    public static string DefaultTitle(IReadOnlyList<TestPaper> sources)
    {
        var title = string.Join(" × ", sources.Select(p => p.Title));
        return title.Length <= MaxTitleLength ? title : title[..(MaxTitleLength - 1)] + "…";
    }

    private static TestPaper Version(IReadOnlyList<TestPaper> sources, PaperMix mix, int version, string pattern, int userId, DateTime now)
    {
        var parts = sources.Select(s => s.Parts.OrderBy(p => p.PartNo).ToList()).ToList();
        var paper = new TestPaper
        {
            SectionTemplateId = mix.SectionTemplateId,
            Title = $"{mix.Title} – Version {version}",
            Status = PaperStatus.Published,
            CreatedByUserId = userId,
            CreatedAt = now,
            PublishedAt = now,
            MixVersion = version,
            MixPattern = pattern,
        };

        for (var i = 0; i < parts[0].Count; i++)
        {
            paper.Parts.Add(CopyPart(parts[PaperMixPlan.SourceOf(pattern, i + 1)][i]));
        }

        return paper;
    }

    private static PaperPart CopyPart(PaperPart part) => new()
    {
        PartNo = part.PartNo,
        Title = part.Title,
        StimulusHtml = part.StimulusHtml,
        Layout = part.Layout,
        QuestionPerPage = part.QuestionPerPage,
        AudioAssetId = part.AudioAssetId,
        Groups = part.Groups.OrderBy(g => g.OrderNo).Select(group => new QuestionGroup
        {
            OrderNo = group.OrderNo,
            InteractionType = group.InteractionType,
            Instructions = group.Instructions,
            BodyHtml = group.BodyHtml,
            OptionPoolJson = group.OptionPoolJson,
            AllowOptionReuse = group.AllowOptionReuse,
            MaxWordsPerAnswer = group.MaxWordsPerAnswer,
            ItemsHeading = group.ItemsHeading,
            PoolHeading = group.PoolHeading,
            ChooseCount = group.ChooseCount,
            Questions = group.Questions.OrderBy(q => q.OrderNo).Select(question => new Question
            {
                Number = question.Number,
                OrderNo = question.OrderNo,
                PromptHtml = question.PromptHtml,
                StimulusHtml = question.StimulusHtml,
                OptionsJson = question.OptionsJson,
                Points = question.Points,
                MinWords = question.MinWords,
                AnswerKeys = question.AnswerKeys.Select(key => new AnswerKey { Value = key.Value, IsCaseSensitive = key.IsCaseSensitive }).ToList(),
            }).ToList(),
        }).ToList(),
    };

    private Task<TestPaper?> LoadAsync(int paperId, CancellationToken cancellationToken) =>
        db.TestPapers.AsNoTracking()
            .Include(p => p.SectionTemplate)
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.AnswerKeys)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);

    private static PaperMixResult Failed(string error) => new(null, [error]);
}
