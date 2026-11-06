using System.Text.Json;
using System.Text.RegularExpressions;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExamPrep.Api.Content;

/// <summary>
/// Which stored files a paper uses (pictures in HTML, picture options, audio tracks), so a candidate can fetch those of
/// the paper they are sitting and nothing else — not the audio of the next section, of another version, or of next
/// month's paper. Cached per paper because a whole room loads the same pictures at the start of a section.
/// </summary>
public sealed partial class ExamAssetIndex(ExamPrepDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private sealed record StoredOption(int? ImageAssetId);

    /// <summary>
    /// A candidate reads the files of the papers of their running attempts (in progress or paused) and no others: nothing
    /// before Start, nothing of a locked section, nothing of another version, nothing once the section has ended.
    /// </summary>
    public async Task<bool> CandidateMayReadAsync(int candidateId, int assetId, CancellationToken cancellationToken)
    {
        var paperIds = await db.Attempts.AsNoTracking()
            .Where(a => a.CandidateId == candidateId && (a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Paused))
            .Select(a => a.TestPaperId)
            .ToListAsync(cancellationToken);
        foreach (var paperId in paperIds)
        {
            var assets = await cache.GetOrCreateAsync($"paper-assets:{paperId}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheLifetime;
                return AssetsOfAsync([paperId], writingOnly: false, cancellationToken);
            });
            if (assets!.Contains(assetId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// VE-72: administrators read every file; other staff the files they uploaded and those of the papers they made, and
    /// examiners those of the scripts dealt to them (the Writing tasks they mark against, VE-73). Cached per person for
    /// a minute and rebuilt on a miss, so a picture just uploaded or imported shows at once.
    /// </summary>
    public async Task<bool> StaffMayReadAsync(System.Security.Claims.ClaimsPrincipal user, int assetId, CancellationToken cancellationToken)
    {
        if (user.IsInRole(nameof(Domain.Enums.UserRole.Admin)))
        {
            return true;
        }

        var userId = Auth.StaffAuthenticationSetup.GetStaffId(user);
        var key = $"staff-assets:{userId}";
        if (cache.TryGetValue(key, out HashSet<int>? cached) && cached!.Contains(assetId))
        {
            return true;
        }

        var assets = await StaffAssetsAsync(userId, cancellationToken);
        cache.Set(key, assets, TimeSpan.FromMinutes(1));
        return assets.Contains(assetId);
    }

    private async Task<HashSet<int>> StaffAssetsAsync(int userId, CancellationToken cancellationToken)
    {
        var markedExams = db.Exams.Where(e => e.Examiners.Any(x => x.UserId == userId)).Select(e => e.Id);
        var ownPapers = await db.TestPapers.Where(p => p.CreatedByUserId == userId).Select(p => p.Id).ToListAsync(cancellationToken);
        // An examiner needs the Writing tasks of the scripts dealt to them, not the Reading pictures or Listening audio.
        var scriptPapers = await db.Attempts
            .Where(a => markedExams.Contains(a.ExamSection!.ExamId) && a.Candidate!.ExaminerUserId == userId)
            .Select(a => a.TestPaperId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var assets = await AssetsOfAsync(ownPapers, writingOnly: false, cancellationToken);
        assets.UnionWith(await AssetsOfAsync(scriptPapers, writingOnly: true, cancellationToken));
        assets.UnionWith(await db.Assets.Where(a => a.UploadedByUserId == userId).Select(a => a.Id).ToListAsync(cancellationToken));
        return assets;
    }

    /// <summary>Every stored file these papers use; with <paramref name="writingOnly"/>, only those of their Writing parts.</summary>
    private async Task<HashSet<int>> AssetsOfAsync(List<int> paperIds, bool writingOnly, CancellationToken cancellationToken)
    {
        var parts = await db.PaperParts.AsNoTracking()
            .Where(p => paperIds.Contains(p.TestPaperId) && (!writingOnly || p.Groups.Any(g => g.InteractionType == InteractionType.Essay)))
            .Select(p => new { p.AudioAssetId, p.StimulusHtml })
            .ToListAsync(cancellationToken);
        var groups = await db.QuestionGroups.AsNoTracking()
            .Where(g => paperIds.Contains(g.PaperPart!.TestPaperId) && (!writingOnly || g.InteractionType == InteractionType.Essay))
            .Select(g => new { g.BodyHtml, g.OptionPoolJson })
            .ToListAsync(cancellationToken);
        var questions = await db.Questions.AsNoTracking()
            .Where(q => paperIds.Contains(q.QuestionGroup!.PaperPart!.TestPaperId) && (!writingOnly || q.QuestionGroup.InteractionType == InteractionType.Essay))
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
