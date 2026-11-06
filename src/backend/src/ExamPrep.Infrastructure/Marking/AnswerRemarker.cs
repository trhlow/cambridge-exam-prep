using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Marking;

/// <param name="Attempts">How many marked attempts on the paper were looked at.</param>
/// <param name="Changed">How many of them got a different score.</param>
/// <param name="Skipped">Attempts that kept changing under the sweep and were left as they were; correcting again marks them.</param>
public sealed record RemarkResult(int Attempts, int Changed, IReadOnlyList<int> Skipped);

/// <summary>
/// A6 repair: after the answer key of one question of a published paper is corrected, marks that question again on
/// every attempt already marked on the paper. Only the question's own mark moves: the attempt's score changes by the
/// difference, so Writing marks (and their history) and the other questions are left alone.
/// </summary>
public sealed class AnswerRemarker(ExamPrepDbContext db)
{
    private const int Tries = 5;

    public async Task<RemarkResult> RemarkAsync(int questionId, CancellationToken cancellationToken)
    {
        var paperId = await db.Questions.Where(q => q.Id == questionId).Select(q => q.QuestionGroup!.PaperPart!.TestPaperId).SingleAsync(cancellationToken);
        var attemptIds = await db.Attempts.AsNoTracking()
            .Where(a => a.TestPaperId == paperId && a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var changed = 0;
        var skipped = new List<int>();
        foreach (var attemptId in attemptIds)
        {
            var done = false;
            for (var attempt = 0; attempt < Tries && !done; attempt++)
            {
                try
                {
                    changed += await RemarkOneAsync(attemptId, questionId, cancellationToken);
                    done = true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // An invigilator or a marker saved the attempt meanwhile: look at it again from fresh data.
                    db.ChangeTracker.Clear();
                }
            }

            if (!done)
            {
                skipped.Add(attemptId);
            }
        }

        return new RemarkResult(attemptIds.Count, changed, skipped);
    }

    /// <returns>1 when the attempt's score changed, otherwise 0.</returns>
    private async Task<int> RemarkOneAsync(int attemptId, int questionId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.SingleAsync(a => a.Id == attemptId, cancellationToken);
        var question = await db.Questions.AsNoTracking().Include(q => q.AnswerKeys).Include(q => q.QuestionGroup).SingleAsync(q => q.Id == questionId, cancellationToken);
        var response = await db.Responses.SingleOrDefaultAsync(r => r.AttemptId == attemptId && r.QuestionId == questionId, cancellationToken);
        if (response is null || attempt.RawScore is null)
        {
            // Nothing was answered, so the question scored 0 before and still does.
            return 0;
        }

        var isCorrect = AnswerMatcher.IsCorrect(question.QuestionGroup!.InteractionType, response.Value, question.AnswerKeys.ToList());
        var score = isCorrect ? question.Points : 0;
        var difference = score - (response.AutoScore ?? 0);
        response.IsCorrect = isCorrect;
        response.AutoScore = score;
        attempt.RawScore += difference;
        await db.SaveChangesAsync(cancellationToken);
        return difference == 0 ? 0 : 1;
    }
}
