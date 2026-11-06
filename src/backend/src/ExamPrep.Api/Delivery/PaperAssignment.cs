using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Papers;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// The paper a candidate gets in a section (VE-66). A section with one paper gives it to everyone; a section with a mix
/// gives the version of the candidate's seat (<see cref="SeatPattern"/>, by the mix's number of versions and sources). A candidate without a numeric seat takes, in
/// candidate-list order, the lowest number nobody typed (<see cref="SeatsOf"/>), so a room seated in that order still works.
/// Once the attempt exists its paper is fixed (<see cref="Attempt.TestPaperId"/>).
/// </summary>
/// <param name="Section">The section's title, e.g. "Reading &amp; Writing".</param>
public sealed record SectionVersion(int SectionId, string Section, int Version);

public sealed class PaperAssignment(ExamPrepDbContext db)
{
    public async Task<int> PaperIdForAsync(ExamSection section, Candidate candidate, CancellationToken cancellationToken)
    {
        if (section.PaperMixId is not { } mixId)
        {
            return section.TestPaperId;
        }

        var shape = await ShapeOfAsync(db, mixId, cancellationToken);
        var seatsPerRow = await db.Exams.Where(e => e.Id == candidate.ExamId).Select(e => e.SeatsPerRow).SingleAsync(cancellationToken);
        var version = SeatPattern.VersionFor(await SeatOfAsync(candidate, cancellationToken), seatsPerRow, shape);
        return await db.TestPapers.AsNoTracking()
            .Where(p => p.PaperMixId == mixId && p.MixVersion == version)
            .Select(p => p.Id)
            .SingleAsync(cancellationToken);
    }

    /// <summary>How many versions the mix has, from how many sources.</summary>
    public static async Task<MixShape> ShapeOfAsync(ExamPrepDbContext db, int mixId, CancellationToken cancellationToken) =>
        await db.PaperMixes.AsNoTracking()
            .Where(m => m.Id == mixId)
            .Select(m => new MixShape(m.VersionCount, m.Sources.Count))
            .SingleAsync(cancellationToken);

    /// <summary>The mixed sections of an exam in order, each with its mix's shape.</summary>
    public static async Task<List<(int SectionId, string Title, MixShape Shape)>> MixedSectionsAsync(ExamPrepDbContext db, int examId, CancellationToken cancellationToken)
    {
        var sections = await db.ExamSections.AsNoTracking()
            .Where(s => s.ExamId == examId && s.PaperMixId != null)
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Id, s.SectionTemplate!.Title, s.PaperMix!.VersionCount, Sources = s.PaperMix.Sources.Count })
            .ToListAsync(cancellationToken);
        return sections.Select(s => (s.Id, s.Title, new MixShape(s.VersionCount, s.Sources))).ToList();
    }

    /// <summary>For each candidate, the version their seat gets in every mixed section (VE-70: mixes may differ in size).</summary>
    public static Dictionary<int, IReadOnlyList<SectionVersion>> SectionVersionsOf(
        IReadOnlyCollection<Candidate> candidates, int? seatsPerRow, IReadOnlyList<(int SectionId, string Title, MixShape Shape)> mixed) =>
        mixed.Count == 0
            ? new()
            : SeatsOf(candidates).ToDictionary(
                p => p.Key,
                p => (IReadOnlyList<SectionVersion>)mixed.Select(m => new SectionVersion(m.SectionId, m.Title, SeatPattern.VersionFor(p.Value, seatsPerRow, m.Shape))).ToList());

    /// <summary>
    /// <see cref="SectionVersionsOf"/> with the version actually sat wherever an attempt exists: a seat changed after Start
    /// must not make the list or the room plan show a paper the candidate never had (A12).
    /// </summary>
    public static async Task<Dictionary<int, IReadOnlyList<SectionVersion>>> SectionVersionsWithStartedAsync(
        ExamPrepDbContext db, int examId, IReadOnlyCollection<Candidate> candidates, int? seatsPerRow, IReadOnlyList<(int SectionId, string Title, MixShape Shape)> mixed, CancellationToken cancellationToken)
    {
        var versions = SectionVersionsOf(candidates, seatsPerRow, mixed);
        if (mixed.Count == 0)
        {
            return versions;
        }

        var sectionIds = mixed.Select(m => m.SectionId).ToList();
        var started = await db.Attempts.AsNoTracking()
            .Where(a => a.Candidate!.ExamId == examId && sectionIds.Contains(a.ExamSectionId) && a.TestPaper!.MixVersion != null)
            .Select(a => new { a.CandidateId, a.ExamSectionId, Version = a.TestPaper!.MixVersion!.Value })
            .ToListAsync(cancellationToken);
        foreach (var byCandidate in started.GroupBy(a => a.CandidateId).Where(g => versions.ContainsKey(g.Key)))
        {
            versions[byCandidate.Key] = versions[byCandidate.Key]
                .Select(v => byCandidate.FirstOrDefault(a => a.ExamSectionId == v.SectionId) is { } a ? v with { Version = a.Version } : v)
                .ToList();
        }

        return versions;
    }

    private async Task<int> SeatOfAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        if (SeatPattern.Parse(candidate.SeatNo) is { } seat)
        {
            return seat;
        }

        var all = await db.Candidates.AsNoTracking().Where(c => c.ExamId == candidate.ExamId).ToListAsync(cancellationToken);
        return SeatsOf(all)[candidate.Id];
    }

    /// <summary>
    /// Each candidate's seat: the number staff typed, or, in list order (by candidate number), the lowest number nobody
    /// typed. Without typed seats that is their place in the list; with some, no two candidates share a seat (A12, B16).
    /// </summary>
    public static Dictionary<int, int> SeatsOf(IEnumerable<Candidate> candidates)
    {
        var ordered = candidates.OrderBy(c => c.CandidateNumber, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id).ToList();
        var typed = ordered.Select(c => SeatPattern.Parse(c.SeatNo)).OfType<int>().ToHashSet();
        var next = 1;
        var seats = new Dictionary<int, int>();
        foreach (var candidate in ordered)
        {
            if (SeatPattern.Parse(candidate.SeatNo) is { } seat)
            {
                seats[candidate.Id] = seat;
                continue;
            }

            while (typed.Contains(next))
            {
                next++;
            }

            seats[candidate.Id] = next++;
        }

        return seats;
    }

    /// <summary>The version each candidate's seat gets in a section with this mix, for the monitor.</summary>
    public static Dictionary<int, int> VersionsOf(IEnumerable<Candidate> candidates, int? seatsPerRow, MixShape shape) =>
        SeatsOf(candidates).ToDictionary(p => p.Key, p => SeatPattern.VersionFor(p.Value, seatsPerRow, shape));

    /// <summary>The mix version of each of these papers (papers that are not mixed are left out).</summary>
    public static async Task<Dictionary<int, int>> MixVersionsAsync(ExamPrepDbContext db, IEnumerable<int> paperIds, CancellationToken cancellationToken)
    {
        var ids = paperIds.Distinct().ToList();
        return await db.TestPapers.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.MixVersion != null)
            .ToDictionaryAsync(p => p.Id, p => p.MixVersion!.Value, cancellationToken);
    }
}
