using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Delivery;

/// <summary>
/// The candidate's sections in delivery order with their state. Sections run one after another, as in the
/// Cambridge exam room: a section opens only once every earlier section has ended for this candidate
/// (submitted, time expired or ended by the invigilator); until then it is <see cref="LockedStatus"/>.
/// </summary>
public sealed class CandidateSectionQuery(ExamPrepDbContext db)
{
    public const string LockedStatus = "Locked";

    public async Task<List<CandidateSectionSummary>> LoadAsync(int candidateId, int examId, CancellationToken cancellationToken)
    {
        var sections = await db.ExamSections
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Id, s.OrderNo, s.SectionTemplate!.Title, s.DurationMinutes })
            .ToListAsync(cancellationToken);

        var attemptStatuses = await db.Attempts
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .ToDictionaryAsync(a => a.ExamSectionId, a => a.Status, cancellationToken);

        var earlierSectionsEnded = true;
        var result = new List<CandidateSectionSummary>(sections.Count);
        foreach (var s in sections)
        {
            var status = attemptStatuses.GetValueOrDefault(s.Id, AttemptStatus.NotStarted);
            var shown = earlierSectionsEnded ? status.ToString() : LockedStatus;
            result.Add(new CandidateSectionSummary(s.Id, s.OrderNo, s.Title, s.DurationMinutes, shown));
            earlierSectionsEnded &= status == AttemptStatus.Submitted;
        }

        return result;
    }
}
