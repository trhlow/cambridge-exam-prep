using System.Text.Json;
using System.Text.RegularExpressions;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExamPrep.Api.Content;

/// <summary>
/// Which stored files an exam's papers use (pictures in HTML, picture options, audio tracks), so a candidate
/// can fetch those and nothing else — not, for example, the audio of next month's paper. Cached per exam
/// because a whole room loads the same pictures at the start of a section.
/// </summary>
public sealed partial class ExamAssetIndex(ExamPrepDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private sealed record StoredOption(int? ImageAssetId);

    public async Task<bool> ExamUsesAsync(int examId, int assetId, CancellationToken cancellationToken)
    {
        var assets = await cache.GetOrCreateAsync($"exam-assets:{examId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;
            return LoadAsync(examId, cancellationToken);
        });
        return assets!.Contains(assetId);
    }

    private async Task<HashSet<int>> LoadAsync(int examId, CancellationToken cancellationToken)
    {
        var sections = await db.ExamSections.Where(s => s.ExamId == examId).Select(s => new { s.TestPaperId, s.PaperMixId }).ToListAsync(cancellationToken);
        var paperIds = sections.Select(s => s.TestPaperId).ToList();
        // A section with a mix delivers all four of its papers.
        var mixIds = sections.Where(s => s.PaperMixId != null).Select(s => s.PaperMixId!.Value).ToList();
        if (mixIds.Count > 0)
        {
            paperIds.AddRange(await db.TestPapers.Where(p => p.PaperMixId != null && mixIds.Contains(p.PaperMixId.Value)).Select(p => p.Id).ToListAsync(cancellationToken));
        }

        var parts = await db.PaperParts.AsNoTracking()
            .Where(p => paperIds.Contains(p.TestPaperId))
            .Select(p => new { p.AudioAssetId, p.StimulusHtml })
            .ToListAsync(cancellationToken);
        var groups = await db.QuestionGroups.AsNoTracking()
            .Where(g => paperIds.Contains(g.PaperPart!.TestPaperId))
            .Select(g => new { g.BodyHtml, g.OptionPoolJson })
            .ToListAsync(cancellationToken);
        var questions = await db.Questions.AsNoTracking()
            .Where(q => paperIds.Contains(q.QuestionGroup!.PaperPart!.TestPaperId))
            .Select(q => new { q.PromptHtml, q.StimulusHtml, q.OptionsJson })
            .ToListAsync(cancellationToken);

        var html = parts.Select(p => p.StimulusHtml)
            .Concat(groups.Select(g => g.BodyHtml))
            .Concat(questions.SelectMany(q => new[] { q.PromptHtml, q.StimulusHtml }));
        var optionJson = groups.Select(g => g.OptionPoolJson).Concat(questions.Select(q => q.OptionsJson));

        var ids = parts.Select(p => p.AudioAssetId).OfType<int>().ToHashSet();
        ids.UnionWith(html.OfType<string>().SelectMany(h => AssetUrl().Matches(h).Select(m => int.Parse(m.Groups[1].Value))));
        ids.UnionWith(optionJson.OfType<string>()
            .SelectMany(j => JsonSerializer.Deserialize<List<StoredOption>>(j, JsonSerializerOptions.Web)!)
            .Select(o => o.ImageAssetId)
            .OfType<int>());
        return ids;
    }

    [GeneratedRegex(@"/api/assets/(\d+)")]
    private static partial Regex AssetUrl();
}
