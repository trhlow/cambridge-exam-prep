using System.ComponentModel.DataAnnotations;
using System.Data;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Papers;
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
        var paper = await db.TestPapers.AsNoTracking().Include(p => p.Parts).SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        var question = await db.Questions
            .AsNoTracking()
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

        if (paper.PaperMixId is { } mixId)
        {
            // A version holds copies of its sources' questions: the one key lives on the source, so the copies cannot disagree.
            var source = await SourceOfAsync(paper, mixId, question, cancellationToken);
            return Problems.Invalid($"This paper is a mixed version; correct the answer on its source paper {source}.");
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

        var targets = await TargetsAsync(paperId, number, cancellationToken);
        var attempts = 0;
        var changed = 0;
        var skipped = new List<int>();
        // READ COMMITTED: the re-mark retries a lost race by reading the attempt again, and under REPEATABLE READ that
        // read would return the snapshot taken at the first SELECT, so a stamp changed by the sweep worker never shows.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        foreach (var target in targets)
        {
            var current = await db.Questions
                .Include(q => q.AnswerKeys)
                .SingleAsync(q => q.Number == number && q.QuestionGroup!.PaperPart!.TestPaperId == target, cancellationToken);
            var before = current.AnswerKeys.OrderBy(k => k.Id).Select(k => k.Value).ToList();
            var caseSensitive = current.AnswerKeys.Count > 0 && current.AnswerKeys.All(k => k.IsCaseSensitive);
            db.AnswerKeys.RemoveRange(current.AnswerKeys);
            current.AnswerKeys = answers.Select(a => new AnswerKey { Value = a, IsCaseSensitive = caseSensitive }).ToList();
            await db.SaveChangesAsync(cancellationToken);

            var result = await remarker.RemarkAsync(current.Id, cancellationToken);
            db.AnswerCorrections.Add(new AnswerCorrection
            {
                TestPaperId = target,
                QuestionNumber = number,
                OldAnswers = string.Join(" | ", before),
                NewAnswers = string.Join(" | ", answers),
                CorrectedByUserId = User.GetStaffId(),
                CorrectedAt = DateTime.UtcNow,
                AttemptsRemarked = result.Attempts,
                AttemptsSkipped = result.Skipped.Count,
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Staff {UserId} corrected the answers of question {Number} of published paper {PaperId} from [{Before}] to [{After}]: {Attempts} attempts marked again, {Changed} scores changed, {Skipped} skipped ({SkippedIds})",
                User.GetStaffId(), number, target, string.Join(" | ", before), string.Join(" | ", answers), result.Attempts, result.Changed, result.Skipped.Count, string.Join(", ", result.Skipped));
            attempts += result.Attempts;
            changed += result.Changed;
            skipped.AddRange(result.Skipped);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AnswersCorrected(number, answers, attempts, changed, skipped);
    }

    /// <summary>The source paper (its title) a mix version took the part holding <paramref name="question"/> from.</summary>
    private async Task<string> SourceOfAsync(TestPaper version, int mixId, Question question, CancellationToken cancellationToken)
    {
        var partNo = await db.PaperParts.Where(p => p.Id == question.QuestionGroup!.PaperPartId).Select(p => p.PartNo).SingleAsync(cancellationToken);
        var ordinal = version.Parts.OrderBy(p => p.PartNo).ToList().FindIndex(p => p.PartNo == partNo) + 1;
        var position = PaperMixPlan.SourceOf(version.MixPattern!, ordinal) + 1;
        return await db.PaperMixSources.Where(s => s.PaperMixId == mixId && s.Position == position).Select(s => s.TestPaper!.Title).SingleAsync(cancellationToken);
    }

    /// <summary>The paper itself, then every mix version that took the question's part from it (in version order).</summary>
    private async Task<List<int>> TargetsAsync(int paperId, int number, CancellationToken cancellationToken)
    {
        var targets = new List<int> { paperId };
        var mixes = await db.PaperMixSources.AsNoTracking().Where(s => s.TestPaperId == paperId).Select(s => new { s.PaperMixId, s.Position }).ToListAsync(cancellationToken);
        foreach (var mix in mixes)
        {
            var versions = await db.TestPapers.AsNoTracking().Include(p => p.Parts)
                .Where(p => p.PaperMixId == mix.PaperMixId).OrderBy(p => p.MixVersion).ToListAsync(cancellationToken);
            foreach (var version in versions)
            {
                var partNo = await db.Questions.Where(q => q.Number == number && q.QuestionGroup!.PaperPart!.TestPaperId == version.Id)
                    .Select(q => q.QuestionGroup!.PaperPart!.PartNo).SingleAsync(cancellationToken);
                var ordinal = version.Parts.OrderBy(p => p.PartNo).ToList().FindIndex(p => p.PartNo == partNo) + 1;
                if (PaperMixPlan.SourceOf(version.MixPattern!, ordinal) + 1 == mix.Position)
                {
                    targets.Add(version.Id);
                }
            }
        }

        return targets;
    }
}
