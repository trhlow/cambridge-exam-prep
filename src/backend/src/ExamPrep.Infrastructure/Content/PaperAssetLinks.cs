using System.Text.RegularExpressions;
using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Content;

/// <summary>
/// C1 (review 02/11): HTML of a paper may point at <c>/api/assets/{id}</c>. A papers manager reads only the files of
/// their own papers and those they uploaded, so copying another person's link into a draft would fetch that
/// person's file through the draft. A link is therefore accepted only for a file the paper already uses or the person
/// uploaded.
/// </summary>
public static partial class PaperAssetLinks
{
    /// <summary>The ids of the stored files these HTML texts link to.</summary>
    public static HashSet<int> In(IEnumerable<string?> html) =>
        html.OfType<string>().SelectMany(h => AssetUrl().Matches(h).Select(m => int.Parse(m.Groups[1].Value))).ToHashSet();

    /// <summary>Every file the HTML of the paper links to (needs parts, groups and questions loaded).</summary>
    public static HashSet<int> In(TestPaper paper) => In(paper.Parts
        .SelectMany(p => p.Groups.SelectMany(g => g.Questions.SelectMany(q => new[] { q.PromptHtml, q.StimulusHtml }).Append(g.BodyHtml)).Append(p.StimulusHtml)));

    /// <summary>The ids among <paramref name="ids"/> that this person did not upload.</summary>
    public static async Task<List<int>> NotUploadedByAsync(ExamPrepDbContext db, IReadOnlyCollection<int> ids, int userId, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var own = await db.Assets.AsNoTracking().Where(a => ids.Contains(a.Id) && a.UploadedByUserId == userId).Select(a => a.Id).ToListAsync(cancellationToken);
        return ids.Except(own).Order().ToList();
    }

    public static string Problem(IEnumerable<int> ids) =>
        $"The picture link {string.Join(", ", ids.Select(id => $"/api/assets/{id}"))} is not a file you uploaded or this paper already uses. Upload the picture again to use it.";

    [GeneratedRegex(@"/api/assets/(\d+)")]
    private static partial Regex AssetUrl();
}
