using System.Security.Cryptography;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Marking;

/// <summary>
/// VE-73 (Victory 29/10): deals the session's Writing scripts to its examiners (<see cref="MarkingAllocation"/>). A
/// candidate is dealt once a section with Writing has ended and been marked automatically (only then can its Writing
/// be marked); their script keeps its examiner while that
/// examiner stays on the session as an active examiner, and always once it is Final. When the examiners change, scripts nobody has started marking are dealt again, so
/// the shares stay even. One deal at a time in this process (the centre runs one API server; the unique code index
/// still holds with more). The background sweep deals after automatic marking (<see cref="EnsureAllAsync"/>).
/// </summary>
public sealed class MarkingAllocator(ExamPrepDbContext db)
{
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    /// <summary>
    /// Deals the scripts of every session that has an eligible examiner (active, role Examiner) and a marked Writing
    /// section whose candidate has no examiner, or one who left, was switched off or is no longer an examiner. A script
    /// already marked to the end (Final) stays with its examiner.
    /// </summary>
    public async Task EnsureAllAsync(CancellationToken cancellationToken)
    {
        var examIds = await db.Attempts
            .Where(a => a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded
                && a.ExamSection!.Exam!.Examiners.Any(x => x.User!.IsActive && x.User.Role == UserRole.Examiner)
                && db.QuestionGroups.Any(g => g.PaperPart!.TestPaperId == a.TestPaperId && g.InteractionType == InteractionType.Essay)
                && (a.Candidate!.ExaminerUserId == null
                    || (a.GradingStatus != GradingStatus.Final
                        && !a.ExamSection.Exam.Examiners.Any(x => x.UserId == a.Candidate.ExaminerUserId && x.User!.IsActive && x.User.Role == UserRole.Examiner))))
            .Select(a => a.ExamSection!.ExamId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var examId in examIds)
        {
            await EnsureAsync(examId, cancellationToken);
        }
    }

    /// <summary>Deals every finished script of the session that has no examiner (or one no longer on the session).</summary>
    public Task EnsureAsync(int examId, CancellationToken cancellationToken) => DealAsync(examId, reshuffleUnmarked: false, cancellationToken);

    /// <summary>After the examiners changed: scripts not yet marked at all are dealt again among the examiners now on the session.</summary>
    public Task RebalanceAsync(int examId, CancellationToken cancellationToken) => DealAsync(examId, reshuffleUnmarked: true, cancellationToken);

    private async Task DealAsync(int examId, bool reshuffleUnmarked, CancellationToken cancellationToken)
    {
        await OneAtATime.WaitAsync(cancellationToken);
        try
        {
            var examiners = await db.ExamExaminers
                .Where(x => x.ExamId == examId && x.User!.IsActive && x.User.Role == UserRole.Examiner)
                .Select(x => x.UserId)
                .OrderBy(id => id)
                .ToListAsync(cancellationToken);
            if (examiners.Count == 0)
            {
                // Nobody to deal to: scripts stay as they are until an examiner is on the session.
                return;
            }

            // Candidates with a finished, automatically marked section whose paper has Writing to mark; and those whose
            // Writing is marked to the end, whose scripts stay with their examiner.
            var writingAttempts = await db.Attempts
                .Where(a => a.ExamSection!.ExamId == examId && a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded
                    && db.QuestionGroups.Any(g => g.PaperPart!.TestPaperId == a.TestPaperId && g.InteractionType == InteractionType.Essay))
                .Select(a => new { a.CandidateId, a.GradingStatus })
                .ToListAsync(cancellationToken);
            var withWriting = writingAttempts.Select(a => a.CandidateId).ToHashSet();
            var final = writingAttempts.GroupBy(a => a.CandidateId).Where(g => g.All(a => a.GradingStatus == GradingStatus.Final)).Select(g => g.Key).ToHashSet();
            var candidates = await db.Candidates.Where(c => c.ExamId == examId).ToListAsync(cancellationToken);
            var marked = reshuffleUnmarked
                ? (await db.ManualMarks
                    .Where(m => m.Response!.Attempt!.ExamSection!.ExamId == examId)
                    .Select(m => m.Response!.Attempt!.CandidateId)
                    .Distinct()
                    .ToListAsync(cancellationToken)).ToHashSet()
                : [];

            // A script whose examiner left or was switched off is dealt again, marks included (they are kept per response);
            // a Final one is not.
            foreach (var candidate in candidates.Where(c => c.ExaminerUserId is { } id && !final.Contains(c.Id)
                && (!examiners.Contains(id) || (reshuffleUnmarked && !marked.Contains(c.Id)))))
            {
                candidate.ExaminerUserId = null;
            }

            var toDeal = candidates.Where(c => c.ExaminerUserId is null && withWriting.Contains(c.Id)).Select(c => c.Id).ToList();
            var kept = candidates.Where(c => c.ExaminerUserId is not null && examiners.Contains(c.ExaminerUserId.Value)).GroupBy(c => c.ExaminerUserId!.Value).ToDictionary(g => g.Key, g => g.Count());
            var random = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
            var dealt = MarkingAllocation.Deal(toDeal, examiners, kept, random);

            var codes = candidates.Select(c => c.MarkingCode).OfType<string>().ToHashSet();
            foreach (var candidate in candidates)
            {
                if (dealt.TryGetValue(candidate.Id, out var examiner))
                {
                    candidate.ExaminerUserId = examiner;
                }

                if (candidate.ExaminerUserId is not null && candidate.MarkingCode is null)
                {
                    candidate.MarkingCode = MarkingAllocation.NewCode(random, codes);
                    codes.Add(candidate.MarkingCode);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            OneAtATime.Release();
        }
    }
}
