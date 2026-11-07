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

/// <param name="MarkingCode">VE-73: the code the examiner sees instead of the name; null until the Writing is dealt.</param>
/// <param name="Examiner">VE-73: the examiner the Writing was dealt to.</param>
/// <param name="ExaminerUnavailable">
/// That examiner can no longer mark it (switched off, no longer an examiner, or off the session). With nobody eligible
/// left on the session the deal stops, so the script keeps them until an active examiner is assigned (final review E).
/// </param>
public sealed record CandidateResult(int CandidateId, string CandidateNumber, string FullName, IReadOnlyList<SectionResult> Sections, string? MarkingCode = null, string? Examiner = null, bool ExaminerUnavailable = false);

/// <param name="Comment">Writing: the teacher's comment with the mark.</param>
/// <param name="Selection">Where the candidate chooses which questions to answer: Yes, No or Undecided; null elsewhere.</param>
/// <param name="Counts">False for a question the candidate did not choose: it is not marked.</param>
/// <param name="PromptHtml">The task the candidate answered, from the paper the attempt sat (sanitised on import).</param>
/// <param name="StimulusHtml">The question's own pictures or text.</param>
/// <param name="PassageHtml">The part's passage or instructions shown with the question.</param>
public sealed record QuestionMark(int Number, int PartNo, string InteractionType, string? Response, IReadOnlyList<string> AcceptedAnswers, bool? IsCorrect, decimal? Score, decimal Points, string? Comment, string? Selection = null, bool Counts = true,
    string? PromptHtml = null, string? StimulusHtml = null, string? PassageHtml = null);

/// <param name="PaperTitle">The paper the candidate sat: with mixed papers, the version of their seat (VE-67).</param>
/// <param name="RawScore">VE-73: the Writing marks so far (the examiner sees no other part of the test).</param>
/// <param name="MaxScore">The most the Writing questions that count can give.</param>
/// <param name="Code">VE-73: the script's code; the examiner never sees the candidate's name.</param>
public sealed record AttemptMarking(int AttemptId, string SectionTitle, string GradingStatus, decimal? RawScore, decimal? MaxScore, IReadOnlyList<QuestionMark> Questions, string? PaperTitle = null, int? PaperVersion = null, string? Code = null);

/// <summary>VE-73: a Writing script dealt to the signed-in examiner, known by its code.</summary>
public sealed record MarkingScript(int AttemptId, int ExamId, string ExamName, DateOnly ScheduledDate, string Section, string Code, string GradingStatus);

/// <summary>
/// Scores for staff. Candidates never see marks in the exam room, as in Cambridge exams; results go through the
/// centre afterwards.
/// </summary>
[ApiController]
[Route("api/staff")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class StaffResultsController(ExamPrepDbContext db) : ControllerBase
{
    /// <summary>
    /// One row per candidate with every section's status and score, and who marks their Writing under which code:
    /// administrators only (VE-73). Invigilators, managers and examiners do not see scores with names.
    /// </summary>
    [HttpGet("exams/{examId:int}/results")]
    [Authorize(Policy = StaffAuthDefaults.ResultsPolicy)]
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
            .Select(c => new { c.Id, c.CandidateNumber, c.FirstName, c.LastName, c.MarkingCode, c.ExaminerUserId })
            .ToListAsync(cancellationToken);
        var examinerIds = candidates.Select(c => c.ExaminerUserId).OfType<int>().Distinct().ToList();
        var examiners = await db.Users.AsNoTracking().Where(u => examinerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
        // The same rule as MarkingAllocator: active, role Examiner, on this session.
        var eligible = (await db.ExamExaminers.AsNoTracking()
            .Where(x => x.ExamId == examId && x.User!.IsActive && x.User.Role == UserRole.Examiner)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();
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
                : new SectionResult(s.Id, s.Title, AttemptStatus.NotStarted.ToString(), GradingStatus.NotGraded.ToString(), null, null, null)).ToList(),
            c.MarkingCode,
            c.ExaminerUserId is { } examinerId ? examiners.GetValueOrDefault(examinerId) : null,
            c.ExaminerUserId is { } dealtTo && !eligible.Contains(dealtTo)))
            .ToList();
    }

    /// <summary>
    /// VE-73: the Writing scripts dealt to the signed-in examiner across their sessions, by code; the deal is brought up
    /// to date first (candidates who finished since are dealt now).
    /// </summary>
    [HttpGet("marking")]
    [Authorize(Policy = StaffAuthDefaults.ExaminerPolicy)]
    public async Task<IReadOnlyList<MarkingScript>> Scripts([FromServices] Infrastructure.Marking.MarkingAllocator allocator, CancellationToken cancellationToken)
    {
        var userId = User.GetStaffId();
        foreach (var examId in await db.Exams.MarkableBy(User).Select(e => e.Id).ToListAsync(cancellationToken))
        {
            await allocator.EnsureAsync(examId, cancellationToken);
        }

        return await db.Attempts.AsNoTracking()
            .Where(a => a.Candidate!.ExaminerUserId == userId && a.Status == AttemptStatus.Submitted && a.GradingStatus != GradingStatus.NotGraded
                && a.ExamSection!.Exam!.Examiners.Any(x => x.UserId == userId)
                && db.QuestionGroups.Any(g => g.PaperPart!.TestPaperId == a.TestPaperId && g.InteractionType == InteractionType.Essay))
            .OrderByDescending(a => a.ExamSection!.Exam!.ScheduledDate).ThenBy(a => a.Candidate!.MarkingCode)
            .Select(a => new MarkingScript(a.Id, a.ExamSection!.ExamId, a.ExamSection.Exam!.Name, a.ExamSection.Exam.ScheduledDate, a.ExamSection.SectionTemplate!.Title, a.Candidate!.MarkingCode!, a.GradingStatus.ToString()))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The Writing of a finished attempt with the marks so far: only for the examiner the script was dealt to (VE-73),
    /// by code, without the candidate's name or the other parts of the test; only once the section has ended.
    /// </summary>
    [HttpGet("attempts/{attemptId:int}/marking")]
    [Authorize(Policy = StaffAuthDefaults.ExaminerPolicy)]
    public async Task<ActionResult<AttemptMarking>> Marking(int attemptId, [FromServices] Infrastructure.Marking.MarkingAllocator allocator, CancellationToken cancellationToken)
    {
        await WritingMarks.DealAsync(db, allocator, attemptId, User, cancellationToken);
        var status = await WritingMarks.AttemptStatusForAsync(db, attemptId, User, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        if (status != AttemptStatus.Submitted)
        {
            return Problem(title: "The candidate's work can be reviewed once the section has ended.", statusCode: StatusCodes.Status409Conflict);
        }

        // A section without Writing (Reading, Listening) has nothing to mark.
        var marking = await WritingMarks.MarkingAsync(db, attemptId, cancellationToken);
        return marking is null || marking.Questions.Count == 0 ? NotFound() : marking;
    }
}

/// <summary>
/// VE-26: the marking view of an attempt, and Writing marks entered by a teacher. Once every Writing question has
/// a mark, the attempt's score is the automatic marks plus the Writing marks and its grading status is Final.
/// </summary>
public static class WritingMarks
{
    /// <summary>
    /// Brings the deal of the attempt's session up to date (VE-73) when the examiner is on that session, so a script
    /// opened straight from its address has an examiner.
    /// </summary>
    public static async Task DealAsync(ExamPrepDbContext db, Infrastructure.Marking.MarkingAllocator allocator, int attemptId, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var markable = db.Exams.MarkableBy(user).Select(e => e.Id);
        var examId = await db.Attempts.AsNoTracking()
            .Where(a => a.Id == attemptId && markable.Contains(a.ExamSection!.ExamId) && a.Candidate!.ExaminerUserId == null)
            .Select(a => (int?)a.ExamSection!.ExamId)
            .SingleOrDefaultAsync(cancellationToken);
        if (examId is { } id)
        {
            await allocator.EnsureAsync(id, cancellationToken);
        }
    }

    /// <summary>
    /// The attempt's status when its candidate's Writing was dealt to this examiner, on a session they are still
    /// assigned to (VE-73); null otherwise.
    /// </summary>
    public static async Task<AttemptStatus?> AttemptStatusForAsync(ExamPrepDbContext db, int attemptId, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var markable = db.Exams.MarkableBy(user).Select(e => e.Id);
        var userId = user.GetStaffId();
        return await db.Attempts.AsNoTracking()
            .Where(a => a.Id == attemptId && markable.Contains(a.ExamSection!.ExamId) && a.Candidate!.ExaminerUserId == userId)
            .Select(a => (AttemptStatus?)a.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<AttemptMarking?> MarkingAsync(ExamPrepDbContext db, int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .Include(a => a.ExamSection!).ThenInclude(s => s.SectionTemplate)
            .Include(a => a.TestPaper)
            .Include(a => a.Candidate)
            .SingleOrDefaultAsync(a => a.Id == attemptId, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        // VE-73: examiners mark Writing only, so the other parts of the test are left out.
        var questions = await db.Questions.AsNoTracking()
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup!).ThenInclude(g => g.PaperPart)
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId && q.QuestionGroup.InteractionType == InteractionType.Essay)
            .OrderBy(q => q.Number)
            .ToListAsync(cancellationToken);
        var responses = await db.Responses.AsNoTracking()
            .Include(r => r.ManualMarks)
            .Where(r => r.AttemptId == attemptId)
            .ToDictionaryAsync(r => r.QuestionId, cancellationToken);

        var counted = Counted(questions, responses);
        var marks = questions.Select(q =>
        {
            var type = q.QuestionGroup!.InteractionType;
            var response = responses.GetValueOrDefault(q.Id);
            var manual = Latest(response?.ManualMarks);
            bool? isCorrect = AnswerMatcher.CanAutoMark(type) ? AnswerMatcher.IsCorrect(type, response?.Value, q.AnswerKeys.ToList()) : null;
            decimal? score = isCorrect is null ? manual?.Score : isCorrect.Value ? q.Points : 0;
            var selection = q.QuestionGroup.ChooseCount is null ? null : (response?.Selection ?? QuestionSelection.Undecided).ToString();
            return new QuestionMark(q.Number, q.QuestionGroup.PaperPart!.PartNo, type.ToString(), response?.Value, q.AnswerKeys.Select(k => k.Value).ToList(), isCorrect, score, q.Points, manual?.Comment, selection, counted.Contains(q.Id), q.PromptHtml, q.StimulusHtml, q.QuestionGroup.PaperPart.StimulusHtml);
        }).ToList();

        var counting = marks.Where(m => m.Counts).ToList();
        var writingScore = counting.All(m => m.Score is null) ? (decimal?)null : counting.Sum(m => m.Score ?? 0);
        var writingMax = counting.Sum(m => m.Points);
        return new AttemptMarking(attempt.Id, attempt.ExamSection!.SectionTemplate!.Title, attempt.GradingStatus.ToString(), writingScore, writingMax, marks, attempt.TestPaper!.Title, attempt.TestPaper.MixVersion, attempt.Candidate!.MarkingCode);
    }

    /// <summary>
    /// The ids of the questions that are marked: all of them, except in a group where the candidate answers only some
    /// (B1 Writing Part 2), where <see cref="ChosenQuestions"/> decides. Needs each question's group loaded.
    /// </summary>
    private static HashSet<int> Counted(IReadOnlyCollection<Question> questions, IReadOnlyDictionary<int, Response> responses) =>
        questions.GroupBy(q => q.QuestionGroupId).SelectMany(group =>
        {
            var numbers = ChosenQuestions.Counted(group.First().QuestionGroup!.ChooseCount, group.Select(q =>
            {
                var response = responses.GetValueOrDefault(q.Id);
                return new ChosenQuestions.Answer(q.Number, response?.Selection ?? QuestionSelection.Undecided, !string.IsNullOrWhiteSpace(response?.Value));
            }));
            return group.Where(q => numbers.Contains(q.Number)).Select(q => q.Id);
        }).ToHashSet();

    /// <summary>The mark that counts: the latest one (marks are never deleted, so a re-mark is a new row).</summary>
    private static ManualMark? Latest(IEnumerable<ManualMark>? marks) =>
        marks?.OrderByDescending(m => m.MarkedAt).ThenByDescending(m => m.Id).FirstOrDefault();

    /// <summary>
    /// Records a mark of one Writing question (a new row; earlier marks stay as history) and recomputes the attempt's
    /// score, in one transaction. Returns the status code, with a sentence for the teacher when the mark is refused.
    /// A concurrent change of the attempt is reloaded and retried once.
    /// </summary>
    public static async Task<(int Status, string? Error)> SaveAsync(ExamPrepDbContext db, int attemptId, WritingMarkRequest request, int markerId, DateTime now, CancellationToken cancellationToken)
    {
        for (var attemptNo = 1; ; attemptNo++)
        {
            try
            {
                return await SaveOnceAsync(db, attemptId, request, markerId, now, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attemptNo == 1)
            {
                db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                return (StatusCodes.Status409Conflict, "The script changed while the mark was being saved. Try again.");
            }
        }
    }

    private static async Task<(int Status, string? Error)> SaveOnceAsync(ExamPrepDbContext db, int attemptId, WritingMarkRequest request, int markerId, DateTime now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Locking the attempt first makes two marks of one script wait for each other instead of deadlocking.
        var attempt = await db.Attempts.FromSql($"SELECT * FROM attempts WHERE id = {attemptId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        if (attempt is null)
        {
            return (StatusCodes.Status404NotFound, null);
        }

        if (attempt.Status != AttemptStatus.Submitted || attempt.GradingStatus == GradingStatus.NotGraded)
        {
            return (StatusCodes.Status409Conflict, "Writing can be marked once the section has ended and been marked automatically.");
        }

        var essays = await db.Questions
            .Include(q => q.QuestionGroup)
            .Where(q => q.QuestionGroup!.PaperPart!.TestPaperId == attempt.TestPaperId && q.QuestionGroup.InteractionType == InteractionType.Essay)
            .ToDictionaryAsync(q => q.Number, cancellationToken);
        if (!essays.TryGetValue(request.QuestionNumber, out var question))
        {
            return (StatusCodes.Status400BadRequest, $"Question {request.QuestionNumber} is not a Writing question of this paper.");
        }

        var essayResponses = await db.Responses.AsNoTracking()
            .Where(r => r.AttemptId == attemptId && r.Question!.QuestionGroup!.InteractionType == InteractionType.Essay)
            .ToDictionaryAsync(r => r.QuestionId, cancellationToken);
        var counted = Counted(essays.Values, essayResponses);
        if (!counted.Contains(question.Id))
        {
            return (StatusCodes.Status400BadRequest, $"The candidate did not choose question {request.QuestionNumber}, so it is not marked.");
        }

        if (request.Score < 0 || request.Score > question.Points)
        {
            return (StatusCodes.Status400BadRequest, $"The mark must be between 0 and {question.Points:0.##}.");
        }

        var response = await db.Responses.SingleOrDefaultAsync(r => r.AttemptId == attemptId && r.QuestionId == question.Id, cancellationToken);
        if (response is null)
        {
            // Nothing was written: the mark (usually 0) still needs a response to belong to.
            response = new Response { AttemptId = attemptId, QuestionId = question.Id, UpdatedAt = now };
            db.Responses.Add(response);
        }

        response.ManualMarks.Add(new ManualMark
        {
            MarkerUserId = markerId,
            Score = request.Score,
            MaxScore = question.Points,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            MarkedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);

        await RecomputeAsync(db, attempt, counted.ToList(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (StatusCodes.Status200OK, null);
    }

    /// <summary>Automatic marks plus the latest marks of the Writing questions that count; Final once each of them has a mark.</summary>
    private static async Task RecomputeAsync(ExamPrepDbContext db, Attempt attempt, List<int> essayIds, CancellationToken cancellationToken)
    {
        var responses = await db.Responses.AsNoTracking().Include(r => r.ManualMarks).Where(r => r.AttemptId == attempt.Id).ToListAsync(cancellationToken);
        var automatic = responses.Sum(r => r.AutoScore ?? 0);
        var writing = responses.Where(r => essayIds.Contains(r.QuestionId)).Select(r => Latest(r.ManualMarks)).OfType<ManualMark>().ToList();
        attempt.RawScore = automatic + writing.Sum(m => m.Score);
        attempt.GradingStatus = writing.Count == essayIds.Count ? GradingStatus.Final : GradingStatus.NeedsManualMarking;
    }
}

public sealed record WritingMarkRequest([Range(1, 1000)] int QuestionNumber, decimal Score, [StringLength(2000)] string? Comment = null);

/// <summary>VE-26, VE-61, VE-73: Writing marks, entered by the examiner the script was dealt to.</summary>
[ApiController]
[Route("api/staff/attempts")]
[Authorize(Policy = StaffAuthDefaults.ExaminerPolicy)]
public sealed class WritingMarksController(ExamPrepDbContext db, TimeProvider clock, ILogger<WritingMarksController> logger) : ControllerBase
{
    [HttpPost("{attemptId:int}/writing-marks")]
    public async Task<ActionResult<AttemptMarking>> Mark(int attemptId, WritingMarkRequest request, [FromServices] Infrastructure.Marking.MarkingAllocator allocator, CancellationToken cancellationToken)
    {
        await WritingMarks.DealAsync(db, allocator, attemptId, User, cancellationToken);
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
