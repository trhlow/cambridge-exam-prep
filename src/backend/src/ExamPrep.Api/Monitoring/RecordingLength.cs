using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Monitoring;

/// <summary>
/// How long a paper's recording is: the parts' tracks play back to back, so the sum of <c>assets.duration_ms</c>. Known
/// only when every track has its length; otherwise null and the monitor cannot say where the recording ends.
/// </summary>
public static class RecordingLength
{
    public static async Task<Dictionary<int, TimeSpan>> OfPapersAsync(ExamPrepDbContext db, IEnumerable<int> paperIds, CancellationToken cancellationToken)
    {
        var ids = paperIds.Distinct().ToList();
        var tracks = await db.PaperParts.AsNoTracking()
            .Where(p => ids.Contains(p.TestPaperId) && p.AudioAssetId != null)
            .Select(p => new { p.TestPaperId, p.AudioAsset!.DurationMs })
            .ToListAsync(cancellationToken);
        return tracks.GroupBy(t => t.TestPaperId)
            .Where(g => g.All(t => t.DurationMs is > 0))
            .ToDictionary(g => g.Key, g => TimeSpan.FromMilliseconds(g.Sum(t => (long)t.DurationMs!.Value)));
    }
}
