using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Exams;

public sealed record ExamSectionView(int Id, int SectionTemplateId, int OrderNo, string Title, int DurationMinutes, int TestPaperId, string PaperTitle);

public sealed record InvigilatorView(int Id, string Username, string FullName);

/// <summary>A session as the staff portal lists it.</summary>
public sealed record ExamListItem(int Id, string Name, string ExamType, DateOnly ScheduledDate, string? Room, string Status, int Candidates, IReadOnlyList<ExamSectionView> Sections);

/// <summary>
/// A session in full (Inspera "parent test"): what the invigilator reads out (PIN), the invigilator password and
/// whether any candidate has started, after which the papers can no longer change.
/// </summary>
public sealed record ExamDetail(
    int Id,
    string Name,
    int ExamTypeId,
    string ExamType,
    DateOnly ScheduledDate,
    string? Room,
    string Status,
    string SessionPin,
    string InvigilatorPassword,
    int Candidates,
    bool HasStarted,
    IReadOnlyList<ExamSectionView> Sections,
    IReadOnlyList<InvigilatorView> Invigilators);

/// <param name="Password">Null when it can no longer be read back (data-protection keys replaced); reset it then.</param>
public sealed record LoginSlip(int CandidateId, string CandidateNumber, string FullName, string Username, string? Password, string ExamName, DateOnly ScheduledDate, string? Room);

/// <summary>
/// Read models for sessions. Collections are loaded with separate flat queries: the MySQL provider does not translate
/// nested collection projections reliably.
/// </summary>
public static class ExamViews
{
    public static async Task<IReadOnlyList<ExamListItem>> ListAsync(ExamPrepDbContext db, IQueryable<Exam> exams, CancellationToken cancellationToken)
    {
        var rows = await exams.AsNoTracking()
            .OrderByDescending(e => e.ScheduledDate).ThenByDescending(e => e.Id)
            .Select(e => new { e.Id, e.Name, ExamType = e.ExamType!.Name, e.ScheduledDate, e.Room, e.Status, Candidates = e.Candidates.Count })
            .ToListAsync(cancellationToken);
        var sections = await SectionsAsync(db, rows.Select(r => r.Id).ToList(), cancellationToken);
        return rows
            .Select(r => new ExamListItem(r.Id, r.Name, r.ExamType, r.ScheduledDate, r.Room, r.Status.ToString(), r.Candidates, sections.GetValueOrDefault(r.Id, [])))
            .ToList();
    }

    public static async Task<ExamDetail?> DetailAsync(ExamPrepDbContext db, IQueryable<Exam> exams, int examId, CancellationToken cancellationToken)
    {
        var exam = await exams.AsNoTracking()
            .Where(e => e.Id == examId)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.ExamTypeId,
                ExamType = e.ExamType!.Name,
                e.ScheduledDate,
                e.Room,
                e.Status,
                e.SessionPin,
                e.InvigilatorPassword,
                Candidates = e.Candidates.Count,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (exam is null)
        {
            return null;
        }

        var sections = await SectionsAsync(db, [examId], cancellationToken);
        var invigilators = await db.ExamInvigilators.AsNoTracking()
            .Where(i => i.ExamId == examId)
            .OrderBy(i => i.User!.FullName)
            .Select(i => new InvigilatorView(i.UserId, i.User!.Username, i.User.FullName))
            .ToListAsync(cancellationToken);
        var hasStarted = await HasStartedAsync(db, examId, cancellationToken);
        return new ExamDetail(
            exam.Id, exam.Name, exam.ExamTypeId, exam.ExamType, exam.ScheduledDate, exam.Room, exam.Status.ToString(), exam.SessionPin,
            exam.InvigilatorPassword, exam.Candidates, hasStarted, sections.GetValueOrDefault(examId, []), invigilators);
    }

    /// <summary>True once any candidate has an attempt at any section of the session.</summary>
    public static Task<bool> HasStartedAsync(ExamPrepDbContext db, int examId, CancellationToken cancellationToken) =>
        db.Attempts.AnyAsync(a => a.ExamSection!.ExamId == examId, cancellationToken);

    private static async Task<Dictionary<int, IReadOnlyList<ExamSectionView>>> SectionsAsync(ExamPrepDbContext db, List<int> examIds, CancellationToken cancellationToken)
    {
        var sections = await db.ExamSections.AsNoTracking()
            .Where(s => examIds.Contains(s.ExamId))
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.ExamId, View = new ExamSectionView(s.Id, s.SectionTemplateId, s.OrderNo, s.SectionTemplate!.Title, s.DurationMinutes, s.TestPaperId, s.TestPaper!.Title) })
            .ToListAsync(cancellationToken);
        return sections.GroupBy(s => s.ExamId).ToDictionary(g => g.Key, g => (IReadOnlyList<ExamSectionView>)g.Select(s => s.View).ToList());
    }
}
