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
/// examiner stays on the session. When the examiners change, scripts nobody has started marking are dealt again, so
/// the shares stay even. One deal at a time in this process (the centre runs one API server; the unique code index
/// still holds with more). The background sweep deals after automatic marking (<see cref="EnsureAllAsync"/>).
/// </summary>
public sealed class MarkingAllocator(ExamPrepDbContext db)
{
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    /// <summary>Deals the scripts of every session that has an examiner and a marked Writing section of an undealt candidate.</summary>
    public async Task EnsureAllAsync(CancellationToken cancellationToken)
    {
        var examIds = await db.Attempts
            .Where(a => a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded && a.Candidate!.ExaminerUserId == null
                && a.ExamSection!.Exam!.Examiners.Any()
                && db.QuestionGroups.Any(g => g.PaperPart!.TestPaperId == a.TestPaperId && g.InteractionType == InteractionType.Essay))
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
                .Where(x => x.ExamId == examId && x.User!.IsActive)
                .Select(x => x.UserId)
                .OrderBy(id => id)
                .ToListAsync(cancellationToken);

            // Candidates with a finished, automatically marked section whose paper has Writing to mark.
            var withWriting = await db.Attempts
                .Where(a => a.ExamSection!.ExamId == examId && a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded
                    && db.QuestionGroups.Any(g => g.PaperPart!.TestPaperId == a.TestPaperId && g.InteractionType == InteractionType.Essay))
                .Select(a => a.CandidateId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var candidates = await db.Candidates.Where(c => c.ExamId == examId).ToListAsync(cancellationToken);
            var marked = reshuffleUnmarked
                ? (await db.ManualMarks
                    .Where(m => m.Response!.Attempt!.ExamSection!.ExamId == examId)
                    .Select(m => m.Response!.Attempt!.CandidateId)
                    .Distinct()
                    .ToListAsync(cancellationToken)).ToHashSet()
                : [];

            foreach (var candidate in candidates.Where(c => c.ExaminerUserId is { } id
                && (!examiners.Contains(id) || (reshuffleUnmarked && !marked.Contains(c.Id)))))
            {
                candidate.ExaminerUserId = null;
            }

            var toDeal = candidates.Where(c => c.ExaminerUserId is null && withWriting.Contains(c.Id)).Select(c => c.Id).ToList();
            var kept = candidates.Where(c => c.ExaminerUserId is not null).GroupBy(c => c.ExaminerUserId!.Value).ToDictionary(g => g.Key, g => g.Count());
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
