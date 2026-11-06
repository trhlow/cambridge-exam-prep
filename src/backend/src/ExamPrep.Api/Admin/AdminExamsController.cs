using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ExamPrep.Api.Approvals;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Api.Content;
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

/// <param name="PaperMixId">Optional (VE-66): give the four papers of this mix by seat instead of one paper to everyone; TestPaperId is then ignored.</param>
public sealed record SectionPaper(int SectionTemplateId, int TestPaperId, int? PaperMixId = null);

/// <param name="SessionPin">Left empty: a random 4-digit PIN.</param>
/// <param name="InvigilatorPassword">Left empty: a random password.</param>
/// <param name="SeatsPerRow">Computers per row (1–50) for giving mixed papers by seat; 0 means one row; omitted keeps the current value.</param>
public sealed record SaveExamRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    int ExamTypeId,
    DateOnly ScheduledDate,
    [StringLength(64)] string? Room,
    [Required] List<SectionPaper> Papers,
    string? SessionPin = null,
    string? InvigilatorPassword = null,
    string? SebConfigKey = null,
    int? SeatsPerRow = null);

public sealed record SetExamStatusRequest([Required] string Status);

public sealed record AssignInvigilatorsRequest([Required] List<int> UserIds);

/// <summary>An invigilator or examiner account that can be assigned to a session.</summary>
public sealed record AssignableStaffView(int Id, string Username, string FullName, string Role, bool IsActive);

/// <summary>
/// VE-21 / VE-23: scheduling exam sessions. Each section of the exam type gets one published paper; the session PIN
/// and invigilator password are generated unless the administrator types their own. Once any candidate has started,
/// the papers are fixed and the session cannot be deleted.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = StaffAuthDefaults.ExamsPolicy)]
public sealed class AdminExamsController(ExamPrepDbContext db, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, ILogger<AdminExamsController> logger) : ControllerBase
{
    private const string StartedTitle = "Candidates have already started this exam.";
    private const int MinPinLength = 4;
    private const int MaxPinLength = 8;
    private const int MinInvigilatorPasswordLength = 6;
    private const int MaxInvigilatorPasswordLength = 64;
    private const string InvigilatorPasswordAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int MaxSeatsPerRow = 50;

    [HttpGet("exam-types")]
    public async Task<IReadOnlyList<ExamTypeView>> ExamTypes(CancellationToken cancellationToken)
    {
        var types = await db.ExamTypes.AsNoTracking().OrderBy(t => t.Id).ToListAsync(cancellationToken);
        var templates = await db.SectionTemplates.AsNoTracking().OrderBy(t => t.OrderNo).ToListAsync(cancellationToken);
        return types
            .Select(t => new ExamTypeView(t.Id, t.Code, t.Name, templates.Where(s => s.ExamTypeId == t.Id).Select(s => new SectionTemplateView(s.Id, s.OrderNo, s.Title, s.DurationMinutes)).ToList()))
            .ToList();
    }

    /// <summary>
    /// The papers a session can use: published ones, and any a session already uses. Managers of exams choose from
    /// them without reaching the paper bank itself.
    /// </summary>
    [HttpGet("exam-papers")]
    public async Task<IReadOnlyList<PaperListItem>> Papers(CancellationToken cancellationToken)
    {
        var used = await db.ExamSections.Select(s => s.TestPaperId).Distinct().ToListAsync(cancellationToken);
        var papers = await AdminPapersController.PaperRows(db, db.TestPapers.Where(p => p.Status == PaperStatus.Published || used.Contains(p.Id)))
            .ToListAsync(cancellationToken);
        return papers.OrderBy(p => p.Title).ToList();
    }

    /// <summary>Mixes of papers a section can give by seat (VE-67), for managers of exams as well as of papers.</summary>
    [HttpGet("exam-paper-mixes")]
    public async Task<IReadOnlyList<PaperMixView>> PaperMixes(CancellationToken cancellationToken) =>
        (await AdminPaperMixesController.ViewsAsync(db, null, cancellationToken)).OrderBy(m => m.Title).ToList();

    /// <summary>Invigilator, examiner and candidates-manager accounts, for the assignment panels of a session.</summary>
    [HttpGet("assignable-staff")]
    public async Task<IReadOnlyList<AssignableStaffView>> AssignableStaff(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Invigilator || u.Role == UserRole.Examiner
                || (u.Role == UserRole.Manager && db.UserAreas.Any(a => a.UserId == u.Id && a.Area == ManagerArea.Candidates)))
            .OrderBy(u => u.FullName)
            .Select(u => new AssignableStaffView(u.Id, u.Username, u.FullName, u.Role.ToString(), u.IsActive))
            .ToListAsync(cancellationToken);

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
            SebConfigKeyHash = Blank(request.SebConfigKey)?.ToLowerInvariant(),
            SeatsPerRow = request.SeatsPerRow is > 0 ? request.SeatsPerRow : null,
            CreatedByUserId = User.GetStaffId(),
            CreatedAt = DateTime.UtcNow,
            Sections = sections,
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} created exam {ExamId}", exam.CreatedByUserId, exam.Id);

        var detail = await ExamViews.WithRightsAsync(db, (await ExamViews.DetailAsync(db, db.Exams, exam.Id, cancellationToken))!, User, cancellationToken);
        return Created($"/api/staff/exams/{exam.Id}", detail);
    }

    [HttpPut("exams/{examId:int}")]
    public async Task<ActionResult<ExamDetail>> Update(int examId, SaveExamRequest request, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.OrganisedBy(User).Include(e => e.Sections).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
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
            || !exam.Sections.Select(s => (s.SectionTemplateId, s.TestPaperId, s.PaperMixId)).OrderBy(p => p).SequenceEqual(sections.Select(s => (s.SectionTemplateId, s.TestPaperId, s.PaperMixId)).OrderBy(p => p));
        if (papersChanged && await ExamViews.HasStartedAsync(db, examId, cancellationToken))
        {
            return Problems.Conflict($"{StartedTitle} Its papers can no longer be changed.");
        }

        // The row length decides every seat's paper, so it freezes with the first attempt (A12).
        if (request.SeatsPerRow is { } newRow && (newRow > 0 ? newRow : null) != exam.SeatsPerRow && await ExamViews.HasStartedAsync(db, examId, cancellationToken))
        {
            return Problems.Conflict($"{StartedTitle} The number of computers per row can no longer change.");
        }

        exam.Name = request.Name.Trim();
        exam.ScheduledDate = request.ScheduledDate;
        exam.Room = Blank(request.Room);
        exam.SessionPin = Blank(request.SessionPin) ?? exam.SessionPin;
        exam.InvigilatorPassword = Blank(request.InvigilatorPassword) ?? exam.InvigilatorPassword;
        if (request.SebConfigKey is not null)
        {
            // Empty switches Safe Exam Browser off; omitted (older clients) keeps it as it is.
            exam.SebConfigKeyHash = Blank(request.SebConfigKey)?.ToLowerInvariant();
            Security.SebActionFilter.Forget(cache, examId);
        }

        if (request.SeatsPerRow is { } seatsPerRow)
        {
            // Attempts already started keep their papers; later sections follow the new layout.
            exam.SeatsPerRow = seatsPerRow > 0 ? seatsPerRow : null;
        }

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
        return await ExamViews.WithRightsAsync(db, (await ExamViews.DetailAsync(db, db.Exams, examId, cancellationToken))!, User, cancellationToken);
    }

    /// <summary>
    /// Draft ↔ Scheduled → Active (candidates can sign in) → Closed, and back: Active → Scheduled only while nobody has
    /// started, Closed → Active for administrators. Any other jump is refused with a sentence (review B1, B2).
    /// </summary>
    [HttpPost("exams/{examId:int}/status")]
    public async Task<ActionResult<ExamDetail>> SetStatus(int examId, SetExamStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.GetNames<ExamStatus>().Contains(request.Status))
        {
            return Problems.Invalid($"Status must be one of {string.Join(", ", Enum.GetNames<ExamStatus>())}.");
        }

        var exam = await db.Exams.OrganisedBy(User).Include(e => e.Candidates).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var target = Enum.Parse<ExamStatus>(request.Status);
        if (await StatusProblemAsync(exam, target, cancellationToken) is { } refusal)
        {
            return refusal;
        }

        if (target == ExamStatus.Active)
        {
            // Seats stop following the list: a candidate added later can no longer push anyone to another paper (A12).
            foreach (var (candidateId, seat) in Delivery.PaperAssignment.SeatsOf(exam.Candidates))
            {
                var candidate = exam.Candidates.Single(c => c.Id == candidateId);
                if (Domain.Papers.SeatPattern.Parse(candidate.SeatNo) is null)
                {
                    candidate.SeatNo = seat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        exam.Status = target;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} set exam {ExamId} to {Status}", User.GetStaffId(), examId, exam.Status);
        return await ExamViews.WithRightsAsync(db, (await ExamViews.DetailAsync(db, db.Exams, examId, cancellationToken))!, User, cancellationToken);
    }

    /// <summary>The refusal for moving the exam to <paramref name="target"/>: 409 for a jump the lifecycle does not allow, 400 for what Active still needs.</summary>
    private async Task<ActionResult?> StatusProblemAsync(Exam exam, ExamStatus target, CancellationToken cancellationToken)
    {
        var from = exam.Status;
        var allowed = (from, target) switch
        {
            (ExamStatus.Draft, ExamStatus.Scheduled) => true,
            (ExamStatus.Scheduled, ExamStatus.Draft) => true,
            (ExamStatus.Scheduled, ExamStatus.Active) => true,
            (ExamStatus.Active, ExamStatus.Scheduled) => true,
            (ExamStatus.Active, ExamStatus.Closed) => true,
            (ExamStatus.Closed, ExamStatus.Active) => true,
            _ => false,
        };
        if (!allowed)
        {
            return Problems.Conflict(from == target
                ? $"The exam is already {from}."
                : $"An exam cannot go from {from} to {target}.{(target == ExamStatus.Active && from == ExamStatus.Draft ? " Schedule it first." : string.Empty)}");
        }

        if (from == ExamStatus.Closed && !User.IsInRole(nameof(UserRole.Admin)))
        {
            return Problems.Conflict("Only an administrator can reopen a closed exam.");
        }

        if (from == ExamStatus.Active && target == ExamStatus.Scheduled && await ExamViews.HasStartedAsync(db, exam.Id, cancellationToken))
        {
            return Problems.Conflict($"{StartedTitle} It cannot go back to Scheduled.");
        }

        if (target == ExamStatus.Closed)
        {
            var running = await db.Attempts.CountAsync(a => a.ExamSection!.ExamId == exam.Id && (a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Paused), cancellationToken);
            if (running > 0)
            {
                return Problems.Conflict($"{running} {(running == 1 ? "candidate is" : "candidates are")} still taking a section (running or paused). End it on the monitoring page first.");
            }
        }

        if (target == ExamStatus.Active)
        {
            var missing = new List<string>();
            if (exam.Candidates.Count == 0)
            {
                missing.Add("Enter at least one candidate before opening the exam.");
            }

            if (!await db.ExamInvigilators.AnyAsync(i => i.ExamId == exam.Id, cancellationToken))
            {
                missing.Add("Assign at least one invigilator before opening the exam.");
            }

            if (missing.Count > 0)
            {
                return Problems.Invalid("The exam cannot be opened yet.", missing);
            }
        }

        return null;
    }

    /// <summary>VE-60: asks for an unused session to be deleted; an administrator other than the requester approves it.</summary>
    [HttpDelete("exams/{examId:int}")]
    public async Task<ActionResult<ApprovalView>> Delete(int examId, [FromServices] ApprovalService approvals, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.OrganisedBy(User).AsNoTracking().SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        if (await ApprovalActions.ExamProblemAsync(db, examId, cancellationToken) is { } problem)
        {
            return Problems.Conflict(problem);
        }

        var (request, conflict) = await approvals.RequestAsync(ApprovalKind.DeleteExam, examId, $"Delete exam “{exam.Name}” ({exam.ScheduledDate:dd/MM/yyyy})", null, User.GetStaffId(), cancellationToken);
        return request is null ? Problems.Conflict(conflict!) : Accepted(request);
    }

    /// <summary>Replaces the session's invigilators; only active invigilator accounts can be assigned.</summary>
    [HttpPut("exams/{examId:int}/invigilators")]
    public async Task<ActionResult<IReadOnlyList<AssignedStaffView>>> AssignInvigilators(int examId, AssignInvigilatorsRequest request, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.OrganisedBy(User).Include(e => e.Invigilators).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        return exam is null
            ? NotFound()
            : await AssignAsync(exam, exam.Invigilators, UserRole.Invigilator, "invigilator", request.UserIds, id => new ExamInvigilator { UserId = id, AssignedAt = DateTime.UtcNow }, i => i.UserId, cancellationToken);
    }

    /// <summary>
    /// VE-61: replaces the session's examiners, who review its finished work, mark Writing and see its results; only
    /// active examiner accounts can be assigned.
    /// </summary>
    [HttpPut("exams/{examId:int}/examiners")]
    public async Task<ActionResult<IReadOnlyList<AssignedStaffView>>> AssignExaminers(
        int examId, AssignInvigilatorsRequest request, [FromServices] Infrastructure.Marking.MarkingAllocator allocator, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.OrganisedBy(User).Include(e => e.Examiners).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var before = exam.Examiners.Select(x => x.UserId).ToHashSet();
        var result = await AssignAsync(exam, exam.Examiners, UserRole.Examiner, "examiner", request.UserIds, id => new ExamExaminer { UserId = id, AssignedAt = DateTime.UtcNow }, x => x.UserId, cancellationToken);
        if (result.Value is not null && !before.SetEquals(request.UserIds))
        {
            // VE-73: the scripts nobody has started marking are dealt again among the examiners now on the session.
            await allocator.RebalanceAsync(examId, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// VE-72: replaces the session's managers of candidates, who enter and change its candidates; only active managers
    /// working on candidates can be assigned.
    /// </summary>
    [HttpPut("exams/{examId:int}/candidate-managers")]
    public async Task<ActionResult<IReadOnlyList<AssignedStaffView>>> AssignCandidateManagers(int examId, AssignInvigilatorsRequest request, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.OrganisedBy(User).Include(e => e.CandidateManagers).SingleOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound();
        }

        var ids = request.UserIds.Distinct().ToList();
        var managers = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.Role == UserRole.Manager && u.IsActive && db.UserAreas.Any(a => a.UserId == u.Id && a.Area == ManagerArea.Candidates))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        return await AssignAsync(
            exam, exam.CandidateManagers, UserRole.Manager, "manager of candidates", ids.Where(managers.Contains).ToList(),
            id => new ExamCandidateManager { UserId = id, AssignedAt = DateTime.UtcNow }, m => m.UserId, cancellationToken, refused: ids.Except(managers).ToList());
    }

    /// <param name="refused">Accounts already found unfit by the caller (managers working in another area).</param>
    private async Task<ActionResult<IReadOnlyList<AssignedStaffView>>> AssignAsync<T>(
        Exam exam, ICollection<T> assigned, UserRole role, string roleName, IEnumerable<int> userIds, Func<int, T> create, Func<T, int> userIdOf, CancellationToken cancellationToken, IReadOnlyList<int>? refused = null)
    {
        var ids = userIds.Distinct().ToList();
        var all = ids.Concat(refused ?? []).ToList();
        var users = await db.Users.AsNoTracking().Where(u => all.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        var errors = all
            .Where(id => (refused?.Contains(id) ?? false) || !users.TryGetValue(id, out var user) || user.Role != role || !user.IsActive)
            .Select(id => users.TryGetValue(id, out var user)
                ? $"{user.FullName} is not an active {roleName} account."
                : $"Staff account {id} does not exist.")
            .ToList();
        if (errors.Count > 0)
        {
            return Problems.Invalid($"The {roleName}s could not be assigned.", errors);
        }

        assigned.Where(a => !ids.Contains(userIdOf(a))).ToList().ForEach(a => assigned.Remove(a));
        foreach (var id in ids.Where(id => assigned.All(a => userIdOf(a) != id)))
        {
            assigned.Add(create(id));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} assigned {Count} {Role}s to exam {ExamId}", User.GetStaffId(), ids.Count, roleName, exam.Id);
        return ids.Select(id => new AssignedStaffView(id, users[id].Username, users[id].FullName)).OrderBy(i => i.FullName).ToList();
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

        if (Blank(request.SebConfigKey) is { } sebKey && !Security.SebGuard.IsConfigKey(sebKey))
        {
            errors.Add("The Safe Exam Browser config key must be 64 hexadecimal characters (SEB Config Tool → Config Key).");
        }

        if (request.SeatsPerRow is < 0 or > MaxSeatsPerRow)
        {
            errors.Add($"Computers per row must be between 1 and {MaxSeatsPerRow} (0 for one row).");
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
        var mixIds = request.Papers.Where(p => p.PaperMixId != null).Select(p => p.PaperMixId!.Value).ToList();
        var mixes = await db.PaperMixes.AsNoTracking().Where(m => mixIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
        var firstVersions = await db.TestPapers.AsNoTracking()
            .Where(p => p.PaperMixId != null && mixIds.Contains(p.PaperMixId.Value) && p.MixVersion == 1)
            .ToDictionaryAsync(p => p.PaperMixId!.Value, p => p.Id, cancellationToken);
        var sections = new List<ExamSection>();
        foreach (var template in templates)
        {
            var choice = request.Papers.FirstOrDefault(p => p.SectionTemplateId == template.Id);
            if (choice?.PaperMixId is { } mixId)
            {
                if (!mixes.TryGetValue(mixId, out var mix) || !firstVersions.TryGetValue(mixId, out var firstVersion))
                {
                    errors.Add($"Choose a mix of papers that exists for {template.Title}.");
                }
                else if (mix.SectionTemplateId != template.Id)
                {
                    errors.Add($"“{mix.Title}” is not a mix of {template.Title} papers for this exam type.");
                }
                else
                {
                    sections.Add(new ExamSection { SectionTemplateId = template.Id, TestPaperId = firstVersion, PaperMixId = mixId, OrderNo = template.OrderNo, DurationMinutes = template.DurationMinutes });
                }

                continue;
            }

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
