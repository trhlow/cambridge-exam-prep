using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Results;

/// <param name="PaperVersion">The mixed-paper version the candidate sat (VE-67); null for a section with one paper.</param>
public sealed record SectionResult(int SectionId, string Title, string Status, string GradingStatus, decimal? RawScore, decimal? MaxScore, int? AttemptId, int? PaperVersion = null);

public sealed record CandidateResult(int CandidateId, string CandidateNumber, string FullName, IReadOnlyList<SectionResult> Sections);

/// <param name="Comment">Writing: the teacher's comment with the mark.</param>
public sealed record QuestionMark(int Number, int PartNo, string InteractionType, string? Response, IReadOnlyList<string> AcceptedAnswers, bool? IsCorrect, decimal? Score, decimal Points, string? Comment);

/// <param name="PaperTitle">The paper the candidate sat: with mixed papers, the version of their seat (VE-67).</param>
public sealed record AttemptMarking(int AttemptId, string SectionTitle, string GradingStatus, decimal? RawScore, decimal? MaxScore, IReadOnlyList<QuestionMark> Questions, string? PaperTitle = null, int? PaperVersion = null);

/// <summary>
/// Scores for staff. Candidates never see marks in the exam room, as in Cambridge exams; results go through the
/// centre afterwards.
/// </summary>
[ApiController]
[Route("api/staff")]
[Authorize(Policy = StaffAuthDefaults.ResultsPolicy)]
public sealed class StaffResultsController(ExamPrepDbContext db) : ControllerBase
{
    /// <summary>
    /// One row per candidate with every section's status and score: administrators see every session, examiners the
    /// sessions they are assigned to. Invigilators and managers do not see scores.
    /// </summary>
    [HttpGet("exams/{examId:int}/results")]
    public async Task<ActionResult<IReadOnlyList<CandidateResult>>> Results(int examId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.ResultsVisibleTo(User).AnyAsync(e => e.Id == examId, cancellationToken))
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
            .Select(a => new { a.Id, a.CandidateId, a.ExamSectionId, a.Status, a.GradingStatus, a.RawScore, a.MaxScore, a.TestPaperId })
            .ToListAsync(cancellationToken);
        var byKey = attempts.ToDictionary(a => (a.CandidateId, a.ExamSectionId));
        var versions = await Delivery.PaperAssignment.MixVersionsAsync(db, attempts.Select(a => a.TestPaperId), cancellationToken);

        return candidates.Select(c => new CandidateResult(
            c.Id,
            c.CandidateNumber,
            $"{c.FirstName} {c.LastName}",
            sections.Select(s => byKey.TryGetValue((c.Id, s.Id), out var a)
                ? new SectionResult(s.Id, s.Title, a.Status.ToString(), a.GradingStatus.ToString(), a.RawScore, a.MaxScore, a.Id, versions.TryGetValue(a.TestPaperId, out var v) ? v : null)
                : new SectionResult(s.Id, s.Title, AttemptStatus.NotStarted.ToString(), GradingStatus.NotGraded.ToString(), null, null, null)).ToList()))
            .ToList();
    }

    /// <summary>
    /// Every question of a finished attempt with the candidate's answer, the accepted answers and the mark. Examiners
    /// of the session only (Victory: only examiners see candidates' work), and only once the section has ended.
    /// </summary>
    [HttpGet("attempts/{attemptId:int}/marking")]
    [Authorize(Policy = StaffAuthDefaults.ExaminerPolicy)]
    public async Task<ActionResult<AttemptMarking>> Marking(int attemptId, CancellationToken cancellationToken)
    {
        var status = await WritingMarks.AttemptStatusForAsync(db, attemptId, User, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        if (status != AttemptStatus.Submitted)
        {
            return Problem(title: "The candidate's work can be reviewed once the section has ended.", statusCode: StatusCodes.Status409Conflict);
        }

        return (await WritingMarks.MarkingAsync(db, attemptId, cancellationToken))!;
    }
}

/// <summary>
/// VE-26: the marking view of an attempt, and Writing marks entered by a teacher. Once every Writing question has
/// a mark, the attempt's score is the automatic marks plus the Writing marks and its grading status is Final.
/// </summary>
public static class WritingMarks
{
    /// <summary>The attempt's status when it belongs to a session the examiner is assigned to; null otherwise.</summary>
    public static async Task<AttemptStatus?> AttemptStatusForAsync(ExamPrepDbContext db, int attemptId, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var markable = db.Exams.MarkableBy(user).Select(e => e.Id);
        return await db.Attempts.AsNoTracking()
            .Where(a => a.Id == attemptId && markable.Contains(a.ExamSection!.ExamId))
            .Select(a => (AttemptStatus?)a.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<AttemptMarking?> MarkingAsync(ExamPrepDbContext db, int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .Include(a => a.ExamSection!).ThenInclude(s => s.SectionTemplate)
            .Include(a => a.TestPaper)
            .SingleOrDefaultAsync(a => a.Id == attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        var questions = await db.Questions.AsNoTracking()
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup!).ThenInclude(g => g.PaperPart)
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId)
            .OrderBy(q => q.Number)
            .ToListAsync(cancellationToken);
        var responses = await db.Responses.AsNoTracking()
            .Include(r => r.ManualMarks)
            .Where(r => r.AttemptId == attemptId)
            .ToDictionaryAsync(r => r.QuestionId, cancellationToken);

        var marks = questions.Select(q =>
        {
            var type = q.QuestionGroup!.InteractionType;
            var response = responses.GetValueOrDefault(q.Id);
            var manual = response?.ManualMarks.OrderByDescending(m => m.MarkedAt).FirstOrDefault();
            bool? isCorrect = AnswerMatcher.CanAutoMark(type) ? AnswerMatcher.IsCorrect(type, response?.Value, q.AnswerKeys.ToList()) : null;
            decimal? score = isCorrect is null ? manual?.Score : isCorrect.Value ? q.Points : 0;
            return new QuestionMark(q.Number, q.QuestionGroup.PaperPart!.PartNo, type.ToString(), response?.Value, q.AnswerKeys.Select(k => k.Value).ToList(), isCorrect, score, q.Points, manual?.Comment);
        }).ToList();

        return new AttemptMarking(attempt.Id, attempt.ExamSection!.SectionTemplate!.Title, attempt.GradingStatus.ToString(), attempt.RawScore, attempt.MaxScore, marks, attempt.TestPaper!.Title, attempt.TestPaper.MixVersion);
    }

    /// <summary>
    /// Records (or replaces) the mark of one Writing question and recomputes the attempt's score. Returns the status
    /// code, with a sentence for the teacher when the mark is refused.
    /// </summary>
    public static async Task<(int Status, string? Error)> SaveAsync(ExamPrepDbContext db, int attemptId, WritingMarkRequest request, int markerId, DateTime now, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.Include(a => a.ExamSection).SingleOrDefaultAsync(a => a.Id == attemptId, cancellationToken);
        if (attempt is null)
        {
            return (StatusCodes.Status404NotFound, null);
        }

        if (attempt.Status != AttemptStatus.Submitted || attempt.GradingStatus == GradingStatus.NotGraded)
        {
            return (StatusCodes.Status409Conflict, "Writing can be marked once the section has ended and been marked automatically.");
        }

        var essays = await db.Questions
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId && q.QuestionGroup.InteractionType == InteractionType.Essay)
            .ToDictionaryAsync(q => q.Number, cancellationToken);
        if (!essays.TryGetValue(request.QuestionNumber, out var question))
        {
            return (StatusCodes.Status400BadRequest, $"Question {request.QuestionNumber} is not a Writing question of this paper.");
        }

        if (request.Score < 0 || request.Score > question.Points)
        {
            return (StatusCodes.Status400BadRequest, $"The mark must be between 0 and {question.Points:0.##}.");
        }

        var response = await db.Responses.Include(r => r.ManualMarks)
            .SingleOrDefaultAsync(r => r.AttemptId == attemptId && r.QuestionId == question.Id, cancellationToken);
        if (response is null)
        {
            // Nothing was written: the mark (usually 0) still needs a response to belong to.
            response = new Response { AttemptId = attemptId, QuestionId = question.Id, UpdatedAt = now };
            db.Responses.Add(response);
        }

        db.ManualMarks.RemoveRange(response.ManualMarks);
        response.ManualMarks.Add(new ManualMark
        {
            MarkerUserId = markerId,
            Score = request.Score,
            MaxScore = question.Points,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            MarkedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);

        await RecomputeAsync(db, attempt, essays.Values.Select(q => q.Id).ToList(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (StatusCodes.Status200OK, null);
    }

    /// <summary>Automatic marks plus Writing marks; Final once every Writing question has a mark.</summary>
    private static async Task RecomputeAsync(ExamPrepDbContext db, Attempt attempt, List<int> essayIds, CancellationToken cancellationToken)
    {
        var responses = await db.Responses.AsNoTracking().Include(r => r.ManualMarks).Where(r => r.AttemptId == attempt.Id).ToListAsync(cancellationToken);
        var automatic = responses.Sum(r => r.AutoScore ?? 0);
        var writing = responses.Where(r => essayIds.Contains(r.QuestionId)).Select(r => r.ManualMarks.FirstOrDefault()).OfType<ManualMark>().ToList();
        attempt.RawScore = automatic + writing.Sum(m => m.Score);
        attempt.GradingStatus = writing.Count == essayIds.Count ? GradingStatus.Final : GradingStatus.NeedsManualMarking;
    }
}

public sealed record WritingMarkRequest([Range(1, 1000)] int QuestionNumber, decimal Score, [StringLength(2000)] string? Comment = null);

/// <summary>VE-26, VE-61: Writing marks, entered by an examiner assigned to the session.</summary>
[ApiController]
[Route("api/staff/attempts")]
[Authorize(Policy = StaffAuthDefaults.ExaminerPolicy)]
public sealed class WritingMarksController(ExamPrepDbContext db, TimeProvider clock, ILogger<WritingMarksController> logger) : ControllerBase
{
    [HttpPost("{attemptId:int}/writing-marks")]
    public async Task<ActionResult<AttemptMarking>> Mark(int attemptId, WritingMarkRequest request, CancellationToken cancellationToken)
    {
        if (await WritingMarks.AttemptStatusForAsync(db, attemptId, User, cancellationToken) is null)
        {
            return NotFound();
        }

        var (status, error) = await WritingMarks.SaveAsync(db, attemptId, request, User.GetStaffId(), clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (status != StatusCodes.Status200OK)
        {
            return error is null ? StatusCode(status) : Problem(title: error, statusCode: status);
        }

        logger.LogInformation("Staff {UserId} marked question {Question} of attempt {AttemptId}", User.GetStaffId(), request.QuestionNumber, attemptId);
        return (await WritingMarks.MarkingAsync(db, attemptId, cancellationToken))!;
    }
}
