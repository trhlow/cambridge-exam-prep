using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

public sealed record CorrectAnswersRequest([Required] List<string> Answers);

/// <param name="AttemptsRemarked">Marked attempts on the paper that were marked again for this question.</param>
/// <param name="ScoresChanged">How many of them got a different score.</param>
/// <param name="Skipped">Attempts left as they were because they kept changing; send the correction again to mark them.</param>
public sealed record AnswersCorrected(int Number, IReadOnlyList<string> Answers, int AttemptsRemarked, int ScoresChanged, IReadOnlyList<int> Skipped);

/// <summary>
/// A6 repair (review 02/11): a published paper is read-only for the editor, since sessions may already use it, yet an
/// answer key can be wrong. An administrator corrects one question's key here; the correction is logged and every
/// attempt already marked on the paper is marked again for that question (<see cref="AnswerRemarker"/>).
/// </summary>
[ApiController]
[Route("api/admin/papers/{paperId:int}/questions/{number:int}/answers")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminPaperAnswersController(ExamPrepDbContext db, AnswerRemarker remarker, ILogger<AdminPaperAnswersController> logger) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<AnswersCorrected>> Correct(int paperId, int number, CorrectAnswersRequest request, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers.AsNoTracking().SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        var question = await db.Questions
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup)
            .SingleOrDefaultAsync(q => q.Number == number && q.QuestionGroup!.PaperPart!.TestPaperId == paperId, cancellationToken);
        if (paper is null || question is null)
        {
            return NotFound();
        }

        if (paper.Status != PaperStatus.Published)
        {
            return Problems.Conflict("Only a published paper is corrected here. Edit a draft in the paper editor.");
        }

        var group = question.QuestionGroup!;
        if (group.InteractionType == InteractionType.Essay)
        {
            return Problems.Invalid("Writing has no answer key: an examiner marks it.");
        }

        var answers = request.Answers.Select(a => a?.Trim() ?? string.Empty).ToList();
        var errors = AdminPaperEditorController.AnswerProblems(group, question.OptionsJson, answers, question.PromptHtml, question.MinWords, $"Question {number}").ToList();
        if (errors.Count > 0)
        {
            return Problems.Invalid("The answers could not be saved.", errors);
        }

        var before = question.AnswerKeys.OrderBy(k => k.Id).Select(k => k.Value).ToList();
        var caseSensitive = question.AnswerKeys.Count > 0 && question.AnswerKeys.All(k => k.IsCaseSensitive);
        db.AnswerKeys.RemoveRange(question.AnswerKeys);
        question.AnswerKeys = answers.Select(a => new AnswerKey { Value = a, IsCaseSensitive = caseSensitive }).ToList();
        await db.SaveChangesAsync(cancellationToken);

        var result = await remarker.RemarkAsync(question.Id, cancellationToken);
        logger.LogWarning(
            "Staff {UserId} corrected the answers of question {Number} of published paper {PaperId} from [{Before}] to [{After}]: {Attempts} attempts marked again, {Changed} scores changed, {Skipped} skipped",
            User.GetStaffId(), number, paperId, string.Join(" | ", before), string.Join(" | ", answers), result.Attempts, result.Changed, result.Skipped.Count);
        return new AnswersCorrected(number, answers, result.Attempts, result.Changed, result.Skipped);
    }
}
