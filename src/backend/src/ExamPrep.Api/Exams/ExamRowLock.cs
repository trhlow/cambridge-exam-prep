using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Exams;

/// <summary>
/// Row lock on an exam, taken inside a transaction. A status change takes it exclusively and a candidate's Start takes
/// it shared, so a Close or a back-to-Scheduled and a Start can never both pass their checks and then both save: one
/// waits for the other and then sees the result (review of Task 5). Two status changes also queue up.
/// </summary>
public static class ExamRowLock
{
    public static Task ExclusiveAsync(ExamPrepDbContext db, int examId, CancellationToken cancellationToken) =>
        db.Database.SqlQuery<int>($"SELECT id AS Value FROM exams WHERE id = {examId} FOR UPDATE").ToListAsync(cancellationToken);

    public static Task SharedAsync(ExamPrepDbContext db, int examId, CancellationToken cancellationToken) =>
        db.Database.SqlQuery<int>($"SELECT id AS Value FROM exams WHERE id = {examId} FOR SHARE").ToListAsync(cancellationToken);
}
