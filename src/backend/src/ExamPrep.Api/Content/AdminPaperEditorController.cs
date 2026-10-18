using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

public sealed record EditPaperRequest([Required, StringLength(256, MinimumLength = 1)] string Title);

public sealed record EditPartRequest([StringLength(256)] string? Title, string? StimulusHtml);

/// <summary>An option keeps its key and picture; only its text changes.</summary>
public sealed record EditOption([Required] string Key, [StringLength(500)] string? Text);

public sealed record EditGroupRequest([Required, StringLength(1000, MinimumLength = 1)] string Instructions, string? BodyHtml, List<EditOption>? OptionPool);

public sealed record EditQuestionRequest(
    string? PromptHtml,
    string? StimulusHtml,
    List<EditOption>? Options,
    [Required] List<string> Answers,
    [Range(0, 100)] decimal Points,
    int? MinWords);

/// <summary>
/// VE-25: fixing a draft paper on the web after the PDF converter, without building a new package: the title, a
/// part's title and passage, a group's instructions, text with gaps and card texts, and each question's wording,
/// option texts, accepted answers, points and word count; a Listening part's recording can be replaced. The same
/// rules as the importer apply (gaps, one answer per choice question, no answer for Writing…), every HTML is
/// sanitised, and only drafts change — a published paper may already be in an exam.
/// </summary>
[ApiController]
[Route("api/admin/papers/{paperId:int}")]
[Authorize(Policy = StaffAuthDefaults.PapersPolicy)]
public sealed class AdminPaperEditorController(ExamPrepDbContext db, IAssetStorage storage, TimeProvider clock, ILogger<AdminPaperEditorController> logger) : ControllerBase
{
    public const long MaxAudioBytes = 60L * 1024 * 1024;
    private const int MaxAnswerLength = 255;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record StoredOption(string Key, string? Text, int? ImageAssetId);

    [HttpPut]
    public Task<ActionResult<PaperDetail>> EditTitle(int paperId, EditPaperRequest request, CancellationToken cancellationToken) =>
        EditAsync(paperId, paper =>
        {
            paper.Title = request.Title.Trim();
            return [];
        }, cancellationToken);

    [HttpPut("parts/{partNo:int}")]
    public Task<ActionResult<PaperDetail>> EditPart(int paperId, int partNo, EditPartRequest request, CancellationToken cancellationToken) =>
        EditAsync(paperId, paper =>
        {
            if (paper.Parts.SingleOrDefault(p => p.PartNo == partNo) is not { } part)
            {
                return null;
            }

            part.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
            part.StimulusHtml = PaperHtml.Sanitize(request.StimulusHtml);
            return [];
        }, cancellationToken);

    [HttpPut("parts/{partNo:int}/groups/{orderNo:int}")]
    public Task<ActionResult<PaperDetail>> EditGroup(int paperId, int partNo, int orderNo, EditGroupRequest request, CancellationToken cancellationToken) =>
        EditAsync(paperId, paper =>
        {
            var group = paper.Parts.SingleOrDefault(p => p.PartNo == partNo)?.Groups.SingleOrDefault(g => g.OrderNo == orderNo);
            if (group is null)
            {
                return null;
            }

            var label = $"Part {partNo}";
            var errors = new List<string>();
            var body = PaperHtml.Sanitize(request.BodyHtml);
            if (group.InteractionType is InteractionType.InlineChoice or InteractionType.InlineText)
            {
                errors.AddRange(PaperHtml.GapProblems(body, group.Questions.Select(q => q.Number).ToList(), label));
            }

            var pool = request.OptionPool is null ? group.OptionPoolJson : WithTexts(group.OptionPoolJson, request.OptionPool, label, "cards", errors);
            if (errors.Count == 0)
            {
                group.Instructions = request.Instructions.Trim();
                group.BodyHtml = body;
                group.OptionPoolJson = pool;
            }

            return errors;
        }, cancellationToken);

    [HttpPut("questions/{number:int}")]
    public Task<ActionResult<PaperDetail>> EditQuestion(int paperId, int number, EditQuestionRequest request, CancellationToken cancellationToken) =>
        EditAsync(paperId, paper =>
        {
            var question = paper.Parts.SelectMany(p => p.Groups).SelectMany(g => g.Questions).SingleOrDefault(q => q.Number == number);
            if (question is null)
            {
                return null;
            }

            var group = question.QuestionGroup!;
            var label = $"Question {number}";
            var errors = new List<string>();
            var options = request.Options is null ? question.OptionsJson : WithTexts(question.OptionsJson, request.Options, label, "options", errors);
            var answers = request.Answers.Select(a => a?.Trim() ?? string.Empty).ToList();
            var prompt = PaperHtml.Sanitize(request.PromptHtml);
            errors.AddRange(AnswerProblems(group, options, answers, prompt, request.MinWords, label));
            if (errors.Count > 0)
            {
                return errors;
            }

            question.PromptHtml = prompt;
            question.StimulusHtml = PaperHtml.Sanitize(request.StimulusHtml);
            question.OptionsJson = options;
            question.Points = request.Points;
            question.MinWords = group.InteractionType == InteractionType.Essay ? request.MinWords : null;
            db.AnswerKeys.RemoveRange(question.AnswerKeys);
            question.AnswerKeys = answers.Select(a => new AnswerKey { Value = a }).ToList();
            return [];
        }, cancellationToken);

    /// <summary>A Listening part's recording, replaced by a new file (mp3, m4a, ogg or wav; at most 60 MB).</summary>
    [HttpPost("parts/{partNo:int}/audio")]
    [RequestSizeLimit(MaxAudioBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAudioBytes + 64 * 1024)]
    public async Task<ActionResult<PaperDetail>> ReplaceAudio(int paperId, int partNo, IFormFile? audio, CancellationToken cancellationToken)
    {
        if (audio is null || audio.Length == 0 || !PackageFileTypes.Audio.ContainsKey(Path.GetExtension(audio.FileName)))
        {
            return Problems.Invalid("Choose an audio file (mp3, m4a, ogg or wav).");
        }

        if (audio.Length > MaxAudioBytes)
        {
            return Problems.Invalid("The audio file must be at most 60 MB.");
        }

        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();
        var sha = Convert.ToHexStringLower(SHA256.HashData(content));
        var extension = Path.GetExtension(audio.FileName).ToLowerInvariant();
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Sha256 == sha, cancellationToken) ?? new Asset
        {
            Kind = AssetKind.Audio,
            OriginalFileName = Path.GetFileName(audio.FileName),
            StoredPath = await storage.SaveAsync(content, sha, extension, cancellationToken),
            ContentType = PackageFileTypes.Audio[extension],
            SizeBytes = content.Length,
            Sha256 = sha,
            UploadedByUserId = User.GetStaffId(),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };

        return await EditAsync(paperId, paper =>
        {
            if (paper.Parts.SingleOrDefault(p => p.PartNo == partNo) is not { } part)
            {
                return null;
            }

            part.AudioAsset = asset;
            return [];
        }, cancellationToken);
    }

    /// <summary>
    /// Loads the paper, applies one change and saves it. The change returns null when its target does not exist
    /// (404) or the sentences explaining why it cannot be saved (400).
    /// </summary>
    private async Task<ActionResult<PaperDetail>> EditAsync(int paperId, Func<TestPaper, IReadOnlyList<string>?> change, CancellationToken cancellationToken)
    {
        var paper = await db.TestPapers
            .Include(p => p.SectionTemplate!).ThenInclude(t => t.ExamType)
            .Include(p => p.Parts).ThenInclude(p => p.Groups).ThenInclude(g => g.Questions).ThenInclude(q => q.AnswerKeys)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper is null)
        {
            return NotFound();
        }

        if (paper.Status != PaperStatus.Draft)
        {
            return Problems.Conflict("Only draft papers can be edited: a published paper may already be in an exam. Import a corrected copy instead.");
        }

        var errors = change(paper);
        if (errors is null)
        {
            return NotFound();
        }

        if (errors.Count > 0)
        {
            return Problems.Invalid("The change could not be saved.", errors);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} edited draft paper {PaperId}", User.GetStaffId(), paperId);
        return PaperDetailMapper.From(paper);
    }

    /// <summary>New texts for the same options: keys and pictures stay, so the answer keys still point at them.</summary>
    private static string? WithTexts(string? json, List<EditOption> edits, string label, string name, List<string> errors)
    {
        var stored = json is null ? [] : JsonSerializer.Deserialize<List<StoredOption>>(json, Json)!;
        if (!stored.Select(o => o.Key).SequenceEqual(edits.Select(e => e.Key)))
        {
            errors.Add($"{label}: the {name} must keep their keys ({string.Join(", ", stored.Select(o => o.Key))}); only their texts can change.");
            return json;
        }

        if (stored.Zip(edits).Any(p => p.First.ImageAssetId is null && string.IsNullOrWhiteSpace(p.Second.Text)))
        {
            errors.Add($"{label}: an option without a picture needs a text.");
        }

        var updated = stored.Zip(edits, (o, e) => o with { Text = string.IsNullOrWhiteSpace(e.Text) ? null : e.Text.Trim() }).ToList();
        return JsonSerializer.Serialize(updated, Json);
    }

    /// <summary>The importer's answer rules for one question (PaperPackageValidator).</summary>
    private static IEnumerable<string> AnswerProblems(QuestionGroup group, string? optionsJson, List<string> answers, string? prompt, int? minWords, string label)
    {
        if (answers.Any(a => a.Length > MaxAnswerLength))
        {
            yield return $"{label}: answers can be at most {MaxAnswerLength} characters.";
        }

        switch (group.InteractionType)
        {
            case InteractionType.SingleChoice or InteractionType.ImageChoice or InteractionType.InlineChoice:
                foreach (var problem in SingleAnswer(answers, optionsJson, "options", label))
                {
                    yield return problem;
                }

                break;
            case InteractionType.DragDropMatch:
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    yield return $"{label}: drag-and-drop questions need a label.";
                }

                foreach (var problem in SingleAnswer(answers, group.OptionPoolJson, "cards", label))
                {
                    yield return problem;
                }

                break;
            case InteractionType.InlineText:
                if (answers.Count == 0 || answers.Any(string.IsNullOrWhiteSpace))
                {
                    yield return $"{label}: at least one accepted answer is required, and none may be empty.";
                }

                break;
            case InteractionType.Essay:
                if (answers.Count > 0)
                {
                    yield return $"{label}: Writing is marked by an examiner and cannot have answers.";
                }

                if (minWords is null or <= 0)
                {
                    yield return $"{label}: Writing questions need a minimum number of words.";
                }

                break;
        }
    }

    private static IEnumerable<string> SingleAnswer(List<string> answers, string? optionsJson, string name, string label)
    {
        if (answers.Count != 1)
        {
            yield return $"{label}: choice questions need exactly one answer.";
            yield break;
        }

        var keys = optionsJson is null ? [] : JsonSerializer.Deserialize<List<StoredOption>>(optionsJson, Json)!.Select(o => o.Key).ToList();
        if (keys.Count > 0 && !keys.Contains(answers[0]))
        {
            yield return $"{label}: answer '{answers[0]}' is not one of the {name} ({string.Join(", ", keys)}).";
        }
    }
}
