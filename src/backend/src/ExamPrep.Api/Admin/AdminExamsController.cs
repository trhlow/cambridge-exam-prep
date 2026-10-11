using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Admin;

public sealed record SectionTemplateView(int Id, int OrderNo, string Title, int DurationMinutes);

public sealed record ExamTypeView(int Id, string Code, string Name, IReadOnlyList<SectionTemplateView> Sections);

public sealed record SectionPaper(int SectionTemplateId, int TestPaperId);

/// <param name="SessionPin">Left empty: a random 4-digit PIN.</param>
/// <param name="InvigilatorPassword">Left empty: a random password.</param>
public sealed record SaveExamRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    int ExamTypeId,
    DateOnly ScheduledDate,
    [StringLength(64)] string? Room,
    [Required] List<SectionPaper> Papers,
    string? SessionPin = null,
    string? InvigilatorPassword = null);

public sealed record SetExamStatusRequest([Required] string Status);

public sealed record AssignInvigilatorsRequest([Required] List<int> UserIds);

/// <summary>
/// VE-21 / VE-23: scheduling exam sessions. Each section of the exam type gets one published paper; the session PIN
/// and invigilator password are generated unless the administrator types their own. Once any candidate has started,
/// the papers are fixed and the session cannot be deleted.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminExamsController(ExamPrepDbContext db, ILogger<AdminExamsController> logger) : ControllerBase
{
    private const string StartedTitle = "Candidates have already started this exam.";
    private const int MinPinLength = 4;
    private const int MaxPinLength = 8;
    private const int MinInvigilatorPasswordLength = 6;
    private const int MaxInvigilatorPasswordLength = 64;
    private const string InvigilatorPasswordAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    [HttpGet("exam-types")]
    public async Task<IReadOnlyList<ExamTypeView>> ExamTypes(CancellationToken cancellationToken)
    {
        var types = await db.ExamTypes.AsNoTracking().OrderBy(t => t.Id).ToListAsync(cancellationToken);
        var templates = await db.SectionTemplates.AsNoTracking().OrderBy(t => t.OrderNo).ToListAsync(cancellationToken);
        return types
            .Select(t => new ExamTypeView(t.Id, t.Code, t.Name, templates.Where(s => s.ExamTypeId == t.Id).Select(s => new SectionTemplateView(s.Id, s.OrderNo, s.Title, s.DurationMinutes)).ToList()))
            .ToList();
    }

    [HttpPost("exams")]
    public async Task<ActionResult<ExamDetail>> Create(SaveExamRequest request, CancellationToken cancellationToken)
    {
        var (sections, errors) = await ValidateAsync(request, new HashSet<int>(), cancellationToken);
        if (errors.Count > 0)
        {
            return Problems.Invalid("The exam could not be saved.", errors);
        }

        var exam = new Exam
        {
            Name = request.Name.Trim(),
            ExamTypeId = request.ExamTypeId,
            ScheduledDate = request.ScheduledDate,
            Room = Blank(request.Room),
            SessionPin = Blank(request.SessionPin) ?? RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4"),
            InvigilatorPassword = Blank(request.InvigilatorPassword) ?? RandomNumberGenerator.GetString(InvigilatorPasswordAlphabet, 10),
            CreatedByUserId = User.GetStaffId(),
            CreatedAt = DateTime.UtcNow,
            Sections = sections,
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} created exam {ExamId}", exam.CreatedByUserId, exam.Id);

        var detail = await ExamViews.DetailAsync(db, db.Exams, exam.Id, cancellationToken);
        return Created($"/api/staff/exams/{exam.Id}", detail);
    }

    [HttpPut("exams/{examId:int}")]
    public async Task<ActionResult<ExamDetail>> Update(int examId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.Include(e => e.Sections).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        // Papers already in the session stay allowed even if they were unpublished since.
        var (sections, errors) = await ValidateAsync(request, exam.Sections.Select(s => s.TestPaperId).ToHashSet(), cancellationToken);
        if (errors.Count > 0)
        {
            return Problems.Invalid("The exam could not be saved.", errors);
        }

        var papersChanged = exam.ExamTypeId != request.ExamTypeId
            || !exam.Sections.Select(s => (s.SectionTemplateId, s.TestPaperId)).OrderBy(p => p).SequenceEqual(sections.Select(s => (s.SectionTemplateId, s.TestPaperId)).OrderBy(p => p));
        if (papersChanged && await ExamViews.HasStartedAsync(db, examId, cancellationToken))
        {
            return Problems.Conflict($"{StartedTitle} Its papers can no longer be changed.");
        }

        exam.Name = request.Name.Trim();
        exam.ScheduledDate = request.ScheduledDate;
        exam.Room = Blank(request.Room);
        exam.SessionPin = Blank(request.SessionPin) ?? exam.SessionPin;
        exam.InvigilatorPassword = Blank(request.InvigilatorPassword) ?? exam.InvigilatorPassword;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (papersChanged)
        {
            // Removed first, in its own save: (exam_id, order_no) is unique.
            db.ExamSections.RemoveRange(exam.Sections);
            await db.SaveChangesAsync(cancellationToken);
            exam.ExamTypeId = request.ExamTypeId;
            exam.Sections = sections;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} updated exam {ExamId}", User.GetStaffId(), examId);
        return await ExamViews.DetailAsync(db, db.Exams, examId, cancellationToken);
    }

    /// <summary>Draft → Scheduled → Active (candidates can sign in) → Closed.</summary>
    [HttpPost("exams/{examId:int}/status")]
    public async Task<ActionResult<ExamDetail>> SetStatus(int examId, SetExamStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.GetNames<ExamStatus>().Contains(request.Status))
        {
            return Problems.Invalid($"Status must be one of {string.Join(", ", Enum.GetNames<ExamStatus>())}.");
        }

        var exam = await db.Exams.SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        exam.Status = Enum.Parse<ExamStatus>(request.Status);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} set exam {ExamId} to {Status}", User.GetStaffId(), examId, exam.Status);
        return await ExamViews.DetailAsync(db, db.Exams, examId, cancellationToken);
    }

    [HttpDelete("exams/{examId:int}")]
    public async Task<IActionResult> Delete(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        if (await ExamViews.HasStartedAsync(db, examId, cancellationToken))
        {
            return Problems.Conflict($"{StartedTitle} It cannot be deleted.");
        }

        db.Exams.Remove(exam);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} deleted exam {ExamId}", User.GetStaffId(), examId);
        return NoContent();
    }

    /// <summary>Replaces the session's invigilators; only active invigilator accounts can be assigned.</summary>
    [HttpPut("exams/{examId:int}/invigilators")]
    public async Task<ActionResult<IReadOnlyList<InvigilatorView>>> AssignInvigilators(int examId, AssignInvigilatorsRequest request, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.Include(e => e.Invigilators).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var ids = request.UserIds.Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        var errors = ids
            .Where(id => !users.TryGetValue(id, out var user) || user.Role != UserRole.Invigilator || !user.IsActive)
            .Select(id => users.TryGetValue(id, out var user)
                ? $"{user.FullName} is not an active invigilator account."
                : $"Staff account {id} does not exist.")
            .ToList();
        if (errors.Count > 0)
        {
            return Problems.Invalid("The invigilators could not be assigned.", errors);
        }

        var now = DateTime.UtcNow;
        exam.Invigilators.Where(i => !ids.Contains(i.UserId)).ToList().ForEach(i => exam.Invigilators.Remove(i));
        foreach (var id in ids.Where(id => exam.Invigilators.All(i => i.UserId != id)))
        {
            exam.Invigilators.Add(new ExamInvigilator { UserId = id, AssignedAt = now });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} assigned {Count} invigilators to exam {ExamId}", User.GetStaffId(), ids.Count, examId);
        return ids.Select(id => new InvigilatorView(id, users[id].Username, users[id].FullName)).OrderBy(i => i.FullName).ToList();
    }

    /// <summary>Checks the request and builds one section per section template of the exam type.</summary>
    private async Task<(List<ExamSection> Sections, List<string> Errors)> ValidateAsync(SaveExamRequest request, IReadOnlySet<int> keptPaperIds, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (Blank(request.SessionPin) is { } pin && (pin.Length is < MinPinLength or > MaxPinLength || !pin.All(char.IsAsciiDigit)))
        {
            errors.Add($"The session PIN must be {MinPinLength}–{MaxPinLength} digits.");
        }

        if (Blank(request.InvigilatorPassword) is { } password && password.Length is < MinInvigilatorPasswordLength or > MaxInvigilatorPasswordLength)
        {
            errors.Add($"The invigilator password must be {MinInvigilatorPasswordLength}–{MaxInvigilatorPasswordLength} characters.");
        }

        var templates = await db.SectionTemplates.AsNoTracking()
            .Where(t => t.ExamTypeId == request.ExamTypeId)
            .OrderBy(t => t.OrderNo)
            .ToListAsync(cancellationToken);
        if (templates.Count == 0)
        {
            errors.Add("Choose an exam type.");
            return ([], errors);
        }

        var paperIds = request.Papers.Select(p => p.TestPaperId).ToList();
        var papers = await db.TestPapers.AsNoTracking().Where(p => paperIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var sections = new List<ExamSection>();
        foreach (var template in templates)
        {
            var choice = request.Papers.FirstOrDefault(p => p.SectionTemplateId == template.Id);
            if (choice is null || !papers.TryGetValue(choice.TestPaperId, out var paper))
            {
                errors.Add($"Choose a paper for {template.Title}.");
                continue;
            }

            if (paper.SectionTemplateId != template.Id)
            {
                errors.Add($"“{paper.Title}” is not a {template.Title} paper for this exam type.");
            }
            else if (paper.Status != PaperStatus.Published && !keptPaperIds.Contains(paper.Id))
            {
                errors.Add($"“{paper.Title}” is not published yet.");
            }

            sections.Add(new ExamSection { SectionTemplateId = template.Id, TestPaperId = paper.Id, OrderNo = template.OrderNo, DurationMinutes = template.DurationMinutes });
        }

        if (request.Papers.Any(p => templates.All(t => t.Id != p.SectionTemplateId)))
        {
            errors.Add("A paper was given for a section this exam type does not have.");
        }

        return (sections, errors);
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
