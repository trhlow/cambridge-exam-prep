using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Marking;

/// <summary>
/// Marks submitted attempts from the answer keys. Each auto-marked response gets <c>is_correct</c> and
/// <c>auto_score</c>; the attempt gets its raw score and the section maximum. A paper with Writing questions
/// stays <see cref="GradingStatus.NeedsManualMarking"/> until a teacher marks them.
/// </summary>
public sealed class AttemptMarker(ExamPrepDbContext db)
{
    /// <summary>Marks every submitted attempt that has not been graded yet; returns how many were marked.</summary>
    public async Task<int> MarkPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await db.Attempts
            .Include(a => a.ExamSection)
            .Where(a => a.Status == AttemptStatus.Submitted && a.GradingStatus == GradingStatus.NotGraded)
            .ToListAsync(cancellationToken);

        foreach (var attempt in pending)
        {
            await MarkAsync(attempt, cancellationToken);
        }

        if (pending.Count == 0)
        {
            return 0;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return pending.Count;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Changed by someone else meanwhile; the next sweep marks it again from fresh data.
            return 0;
        }
    }

    private async Task MarkAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        var questions = await db.Questions
            .AsNoTracking()
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup)
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.ExamSection!.TestPaperId)
            .ToListAsync(cancellationToken);
        var responses = await db.Responses
            .Where(r => r.AttemptId == attempt.Id)
            .ToDictionaryAsync(r => r.QuestionId, cancellationToken);

        decimal raw = 0, max = 0;
        var needsTeacher = false;
        foreach (var question in questions)
        {
            max += question.Points;
            var type = question.QuestionGroup!.InteractionType;
            if (!AnswerMatcher.CanAutoMark(type))
            {
                needsTeacher = true;
                continue;
            }

            responses.TryGetValue(question.Id, out var response);
            var isCorrect = AnswerMatcher.IsCorrect(type, response?.Value, question.AnswerKeys.ToList());
            var score = isCorrect ? question.Points : 0;
            if (response is not null)
            {
                response.IsCorrect = isCorrect;
                response.AutoScore = score;
            }

            raw += score;
        }

        attempt.RawScore = raw;
        attempt.MaxScore = max;
        attempt.GradingStatus = needsTeacher ? GradingStatus.NeedsManualMarking : GradingStatus.AutoGraded;
    }
}
