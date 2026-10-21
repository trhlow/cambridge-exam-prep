using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Papers;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Content;

public sealed record PaperMixResult(PaperMix? Mix, IReadOnlyList<string> Errors);

/// <summary>
/// Mixes two published source papers of the same layout into four published papers (Victory 20/10). Each version copies
/// whole parts — passage, pictures, recording, questions and answer keys — from source A or B as
/// <see cref="PaperMixPlan"/> chooses, so marking, Writing marks and the paper review work on them like on any paper.
/// Pictures and recordings are shared, not copied: assets are stored once by content.
/// </summary>
public sealed class PaperMixer(ExamPrepDbContext db, TimeProvider clock)
{
    public const int MaxTitleLength = 200;

    public async Task<PaperMixResult> CreateAsync(int sourceAPaperId, int sourceBPaperId, string? title, int? seed, int userId, CancellationToken cancellationToken)
    {
        var a = await LoadAsync(sourceAPaperId, cancellationToken);
        var b = await LoadAsync(sourceBPaperId, cancellationToken);
        if (a is null || b is null)
        {
            return Failed("Choose two papers that exist.");
        }

        var problems = new List<string>();
        foreach (var paper in a.Id == b.Id ? [a] : new[] { a, b })
        {
            if (paper.Status != PaperStatus.Published)
            {
                problems.Add($"\"{paper.Title}\" is not published yet: publish it before mixing it.");
            }

            if (paper.PaperMixId is not null)
            {
                problems.Add($"\"{paper.Title}\" is itself a mixed paper: mix the two source papers instead.");
            }
        }

        problems.AddRange(PaperShape.Differences(a, b));
        if (problems.Count > 0)
        {
            return new PaperMixResult(null, problems);
        }

        var name = string.IsNullOrWhiteSpace(title) ? DefaultTitle(a, b) : title.Trim();
        if (name.Length > MaxTitleLength)
        {
            return Failed($"The title can have at most {MaxTitleLength} characters.");
        }

        var partCount = a.Parts.Count;
        if (partCount < 2 || partCount > PaperMixPlan.MaxParts)
        {
            return Failed($"Papers with {partCount} part(s) cannot be mixed.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var mix = new PaperMix
        {
            Title = name,
            SectionTemplateId = a.SectionTemplateId,
            SourceAPaperId = a.Id,
            SourceBPaperId = b.Id,
            Seed = seed ?? Random.Shared.Next(1, int.MaxValue),
            CreatedByUserId = userId,
            CreatedAt = now,
        };

        var patterns = PaperMixPlan.Patterns(partCount, mix.Seed);
        for (var version = 1; version <= PaperMixPlan.Versions; version++)
        {
            mix.Versions.Add(Version(a, b, mix, version, patterns[version - 1], userId, now));
        }

        db.PaperMixes.Add(mix);
        await db.SaveChangesAsync(cancellationToken);
        return new PaperMixResult(mix, []);
    }

    /// <summary>"A2 Key – Test 1 × A2 Key for Schools – Test 2", cut to the column size.</summary>
    public static string DefaultTitle(TestPaper a, TestPaper b)
    {
        var title = $"{a.Title} × {b.Title}";
        return title.Length <= MaxTitleLength ? title : title[..(MaxTitleLength - 1)] + "…";
    }

    private static TestPaper Version(TestPaper a, TestPaper b, PaperMix mix, int version, string pattern, int userId, DateTime now)
    {
        var partsA = a.Parts.OrderBy(p => p.PartNo).ToList();
        var partsB = b.Parts.OrderBy(p => p.PartNo).ToList();
        var paper = new TestPaper
        {
            SectionTemplateId = a.SectionTemplateId,
            Title = $"{mix.Title} – Version {version}",
            Status = PaperStatus.Published,
            CreatedByUserId = userId,
            CreatedAt = now,
            PublishedAt = now,
            MixVersion = version,
            MixPattern = pattern,
        };

        for (var i = 0; i < partsA.Count; i++)
        {
            var source = PaperMixPlan.FromSourceB(pattern, i + 1) ? partsB[i] : partsA[i];
            paper.Parts.Add(CopyPart(source));
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
