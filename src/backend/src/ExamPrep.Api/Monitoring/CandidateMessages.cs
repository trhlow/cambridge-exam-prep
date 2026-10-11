using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Monitoring;

/// <param name="CandidateId">Null: sent to the whole room.</param>
public sealed record CandidateMessageView(int Id, string Text, DateTime SentAt, int? CandidateId);

public static class CandidateMessages
{
    /// <summary>What one candidate sees under the bell: messages to the whole room and to them, oldest first.</summary>
    public static Task<List<CandidateMessageView>> ForCandidateAsync(ExamPrepDbContext db, int examId, int candidateId, CancellationToken cancellationToken) =>
        db.CandidateMessages.AsNoTracking()
            .Where(m => m.ExamId == examId && (m.CandidateId == null || m.CandidateId == candidateId))
            .OrderBy(m => m.SentAt).ThenBy(m => m.Id)
            .Select(m => new CandidateMessageView(m.Id, m.Text, m.SentAt, m.CandidateId))
            .ToListAsync(cancellationToken);
}
