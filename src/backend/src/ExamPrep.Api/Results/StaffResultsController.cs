using ExamPrep.Api.Auth;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Results;

public sealed record SectionResult(int SectionId, string Title, string Status, string GradingStatus, decimal? RawScore, decimal? MaxScore, int? AttemptId);

public sealed record CandidateResult(int CandidateId, string CandidateNumber, string FullName, IReadOnlyList<SectionResult> Sections);

public sealed record QuestionMark(int Number, int PartNo, string InteractionType, string? Response, IReadOnlyList<string> AcceptedAnswers, bool? IsCorrect, decimal? Score, decimal Points);

public sealed record AttemptMarking(int AttemptId, string SectionTitle, string GradingStatus, decimal? RawScore, decimal? MaxScore, IReadOnlyList<QuestionMark> Questions);

/// <summary>
/// Scores for staff. Candidates never see marks in the exam room, as in Cambridge exams; results go through the
/// centre afterwards.
/// </summary>
[ApiController]
[Route("api/staff")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class StaffResultsController(ExamPrepDbContext db) : ControllerBase
{
    /// <summary>One row per candidate with every section's status and score; invigilators see their own sessions only.</summary>
    [HttpGet("exams/{examId:int}/results")]
    public async Task<ActionResult<IReadOnlyList<CandidateResult>>> Results(int examId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.VisibleTo(User).AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return NotFound();
        }

        var sections = await db.ExamSections.AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Id, s.SectionTemplate!.Title })
            .ToListAsync(cancellationToken);
        var candidates = await db.Candidates.AsNoTracking()
            .Where(c => c.ExamId == examId)
            .OrderBy(c => c.CandidateNumber)
            .Select(c => new { c.Id, c.CandidateNumber, c.FirstName, c.LastName })
            .ToListAsync(cancellationToken);
        var attempts = await db.Attempts.AsNoTracking()
            .Where(a => a.ExamSection!.ExamId == examId)
            .Select(a => new { a.Id, a.CandidateId, a.ExamSectionId, a.Status, a.GradingStatus, a.RawScore, a.MaxScore })
            .ToListAsync(cancellationToken);
        var byKey = attempts.ToDictionary(a => (a.CandidateId, a.ExamSectionId));

        return candidates.Select(c => new CandidateResult(
            c.Id,
            c.CandidateNumber,
            $"{c.FirstName} {c.LastName}",
            sections.Select(s => byKey.TryGetValue((c.Id, s.Id), out var a)
                ? new SectionResult(s.Id, s.Title, a.Status.ToString(), a.GradingStatus.ToString(), a.RawScore, a.MaxScore, a.Id)
                : new SectionResult(s.Id, s.Title, AttemptStatus.NotStarted.ToString(), GradingStatus.NotGraded.ToString(), null, null, null)).ToList()))
            .ToList();
    }

    /// <summary>
    /// Every question of an attempt with the candidate's answer, the accepted answers and the mark. Administrators
    /// only: invigilators are in the exam room while other candidates may still be taking the same paper, so they
    /// must not see the answer key.
    /// </summary>
    [HttpGet("attempts/{attemptId:int}/marking")]
    [Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
    public async Task<ActionResult<AttemptMarking>> Marking(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .Include(a => a.ExamSection!).ThenInclude(s => s.SectionTemplate)
            .SingleOrDefaultAsync(a => a.Id == attemptId, cancellationToken);
        if (attempt is null)
        {
            return NotFound();
        }

        var questions = await db.Questions.AsNoTracking()
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup!).ThenInclude(g => g.PaperPart)
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.ExamSection!.TestPaperId)
            .OrderBy(q => q.Number)
            .ToListAsync(cancellationToken);
        var responses = await db.Responses.AsNoTracking()
            .Where(r => r.AttemptId == attemptId)
            .ToDictionaryAsync(r => r.QuestionId, r => r.Value, cancellationToken);

        var marks = questions.Select(q =>
        {
            var type = q.QuestionGroup!.InteractionType;
            var response = responses.GetValueOrDefault(q.Id);
            bool? isCorrect = AnswerMatcher.CanAutoMark(type) ? AnswerMatcher.IsCorrect(type, response, q.AnswerKeys.ToList()) : null;
            decimal? score = isCorrect is null ? null : isCorrect.Value ? q.Points : 0;
            return new QuestionMark(q.Number, q.QuestionGroup.PaperPart!.PartNo, type.ToString(), response, q.AnswerKeys.Select(k => k.Value).ToList(), isCorrect, score, q.Points);
        }).ToList();

        return new AttemptMarking(attempt.Id, attempt.ExamSection!.SectionTemplate!.Title, attempt.GradingStatus.ToString(), attempt.RawScore, attempt.MaxScore, marks);
    }
}
