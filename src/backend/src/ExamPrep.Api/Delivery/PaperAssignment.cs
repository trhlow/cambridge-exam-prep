using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Papers;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// The paper a candidate gets in a section (VE-66). A section with one paper gives it to everyone; a section with a mix
/// gives the version of the candidate's seat (<see cref="SeatPattern"/>). A candidate without a numeric seat takes the
/// seat of their place in the candidate list (by candidate number), so a room seated in that order still works.
/// Once the attempt exists its paper is fixed (<see cref="Attempt.TestPaperId"/>).
/// </summary>
public sealed class PaperAssignment(ExamPrepDbContext db)
{
    public async Task<int> PaperIdForAsync(ExamSection section, Candidate candidate, CancellationToken cancellationToken)
    {
        if (section.PaperMixId is not { } mixId)
        {
            return section.TestPaperId;
        }

        var version = await VersionForAsync(candidate, cancellationToken);
        return await db.TestPapers.AsNoTracking()
            .Where(p => p.PaperMixId == mixId && p.MixVersion == version)
            .Select(p => p.Id)
            .SingleAsync(cancellationToken);
    }

    /// <summary>The version (1–4) the candidate's seat gets.</summary>
    public async Task<int> VersionForAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        var seatsPerRow = await db.Exams.Where(e => e.Id == candidate.ExamId).Select(e => e.SeatsPerRow).SingleAsync(cancellationToken);
        return SeatPattern.VersionFor(await SeatOfAsync(candidate, cancellationToken), seatsPerRow);
    }

    private async Task<int> SeatOfAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        if (SeatPattern.Parse(candidate.SeatNo) is { } seat)
        {
            return seat;
        }

        var order = await db.Candidates.AsNoTracking()
            .Where(c => c.ExamId == candidate.ExamId)
            .OrderBy(c => c.CandidateNumber).ThenBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        return order.IndexOf(candidate.Id) + 1;
    }
}
