using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Auth;
using ExamPrep.Api.Common;
using ExamPrep.Api.Exams;
using ExamPrep.Domain.Candidates;
using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Admin;

/// <summary>One row of the entry list (typed in, or pasted from a spreadsheet by the portal). Checked row by row by the controller.</summary>
public sealed record NewCandidate(string? CandidateNumber, string? FirstName, string? LastName, DateOnly? DateOfBirth = null, int PlannedExtraPercent = 0, string? SeatNo = null);

public sealed record AddCandidatesRequest([Required, MinLength(1), MaxLength(500)] List<NewCandidate> Candidates);

public sealed record UpdateCandidateRequest(
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    DateOnly? DateOfBirth,
    [Range(0, 100)] int PlannedExtraPercent,
    [StringLength(16)] string? SeatNo);

public sealed record CandidateView(int Id, string CandidateNumber, string FirstName, string LastName, string Username, DateOnly? DateOfBirth, int PlannedExtraPercent, string? SeatNo, bool HasStarted)
{
    public static CandidateView From(Candidate c, bool hasStarted) =>
        new(c.Id, c.CandidateNumber, c.FirstName, c.LastName, c.Username, c.DateOfBirth, c.PlannedExtraPercent, c.SeatNo, hasStarted);
}

/// <summary>
/// VE-22: entering candidates for a session. Usernames follow Cambridge (5 letters of the family name, first initial,
/// candidate number); passwords are random and can be printed again on the login slips. A list with any invalid row
/// is refused as a whole, with one sentence per problem.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
public sealed class AdminCandidatesController(ExamPrepDbContext db, CandidateCredentials credentials, ILogger<AdminCandidatesController> logger) : ControllerBase
{
    private const int MaxNumberLength = 16;
    private const int MaxNameLength = 100;
    private const int MaxUsernameLength = 64;

    [HttpGet("exams/{examId:int}/candidates")]
    [Authorize(Policy = StaffAuthDefaults.CandidateListPolicy)]
    public async Task<ActionResult<IReadOnlyList<CandidateView>>> List(int examId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return NotFound();
        }

        var candidates = await db.Candidates.AsNoTracking().Where(c => c.ExamId == examId).OrderBy(c => c.CandidateNumber).ToListAsync(cancellationToken);
        var started = await StartedCandidateIdsAsync(examId, cancellationToken);
        return candidates.Select(c => CandidateView.From(c, started.Contains(c.Id))).ToList();
    }

    [HttpPost("exams/{examId:int}/candidates")]
    [Authorize(Policy = StaffAuthDefaults.CandidatesPolicy)]
    public async Task<ActionResult<IReadOnlyList<CandidateView>>> Add(int examId, AddCandidatesRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return NotFound();
        }

        var existingNumbers = await db.Candidates.Where(c => c.ExamId == examId).Select(c => c.CandidateNumber).ToListAsync(cancellationToken);
        var errors = Validate(request.Candidates, existingNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (errors.Count > 0)
        {
            return Problems.Invalid("No candidates were added.", errors);
        }

        var baseNames = request.Candidates.Select(c => CandidateUsername.For(c.LastName!, c.FirstName!, c.CandidateNumber!)).ToList();
        var taken = await TakenUsernamesAsync(baseNames, cancellationToken);
        var now = DateTime.UtcNow;
        var created = new List<Candidate>();
        for (var i = 0; i < request.Candidates.Count; i++)
        {
            var row = request.Candidates[i];
            var candidate = new Candidate
            {
                ExamId = examId,
                CandidateNumber = row.CandidateNumber!.Trim(),
                FirstName = row.FirstName!.Trim(),
                LastName = row.LastName!.Trim(),
                DateOfBirth = row.DateOfBirth,
                PlannedExtraPercent = row.PlannedExtraPercent,
                SeatNo = string.IsNullOrWhiteSpace(row.SeatNo) ? null : row.SeatNo.Trim(),
                Username = FreeUsername(baseNames[i], taken),
                PasswordHash = string.Empty,
                PasswordProtected = string.Empty,
                CreatedAt = now,
            };
            credentials.SetPassword(candidate, CandidateCredentials.NewPassword());
            created.Add(candidate);
        }

        db.Candidates.AddRange(created);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another administrator added the same number or username at the same moment.
            return Problems.Conflict("The list changed while it was being saved. Please add the candidates again.");
        }

        logger.LogInformation("Staff {UserId} added {Count} candidates to exam {ExamId}", User.GetStaffId(), created.Count, examId);
        return Created($"/api/admin/exams/{examId}/candidates", created.Select(c => CandidateView.From(c, hasStarted: false)).ToList());
    }

    [HttpPut("candidates/{candidateId:int}")]
    [Authorize(Policy = StaffAuthDefaults.CandidatesPolicy)]
    public async Task<ActionResult<CandidateView>> Update(int candidateId, UpdateCandidateRequest request, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates.SingleOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        // The username stays as printed on the slip even when a misspelt name is corrected.
        candidate.FirstName = request.FirstName.Trim();
        candidate.LastName = request.LastName.Trim();
        candidate.DateOfBirth = request.DateOfBirth;
        candidate.PlannedExtraPercent = request.PlannedExtraPercent;
        candidate.SeatNo = string.IsNullOrWhiteSpace(request.SeatNo) ? null : request.SeatNo.Trim();
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} updated candidate {CandidateId}", User.GetStaffId(), candidateId);

        var hasStarted = await db.Attempts.AnyAsync(a => a.CandidateId == candidateId, cancellationToken);
        return CandidateView.From(candidate, hasStarted);
    }

    /// <summary>
    /// A new password for a lost slip (or one that can no longer be printed). Any computer the candidate is signed in
    /// on is signed out; the username does not change.
    /// </summary>
    [HttpPost("candidates/{candidateId:int}/reset-password")]
    [Authorize(Policy = StaffAuthDefaults.CandidatesPolicy)]
    public async Task<ActionResult<LoginSlip>> ResetPassword(int candidateId, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates.Include(c => c.Exam).SingleOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        var password = CandidateCredentials.NewPassword();
        credentials.SetPassword(candidate, password);
        candidate.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} reset the password of candidate {CandidateId}", User.GetStaffId(), candidateId);

        var exam = candidate.Exam!;
        return new LoginSlip(candidate.Id, candidate.CandidateNumber, $"{candidate.FirstName} {candidate.LastName}", candidate.Username, password, exam.Name, exam.ScheduledDate, exam.Room);
    }

    [HttpDelete("candidates/{candidateId:int}")]
    [Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
    public async Task<IActionResult> Delete(int candidateId, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates.SingleOrDefaultAsync(c => c.Id == candidateId, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        if (await db.Attempts.AnyAsync(a => a.CandidateId == candidateId, cancellationToken))
        {
            return Problems.Conflict("This candidate has already started the exam and cannot be removed.");
        }

        db.Candidates.Remove(candidate);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} removed candidate {CandidateId}", User.GetStaffId(), candidateId);
        return NoContent();
    }

    private static List<string> Validate(List<NewCandidate> rows, HashSet<string> existingNumbers)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var label = $"Row {i + 1}";
            var number = row.CandidateNumber?.Trim() ?? string.Empty;
            if (number.Length is 0 or > MaxNumberLength || !number.All(char.IsAsciiLetterOrDigit))
            {
                errors.Add($"{label}: the candidate number must be 1–{MaxNumberLength} letters or digits.");
            }
            else if (existingNumbers.Contains(number))
            {
                errors.Add($"{label}: candidate number {number} is already entered for this exam.");
            }
            else if (!seen.Add(number))
            {
                errors.Add($"{label}: candidate number {number} is already in the list.");
            }

            if (!HasLetters(row.FirstName) || !HasLetters(row.LastName))
            {
                errors.Add($"{label}: enter the family name and the given name.");
            }
            else if (row.FirstName!.Trim().Length > MaxNameLength || row.LastName!.Trim().Length > MaxNameLength)
            {
                errors.Add($"{label}: names can be at most {MaxNameLength} characters.");
            }

            if (row.PlannedExtraPercent is < 0 or > 100)
            {
                errors.Add($"{label}: extra time must be between 0 and 100 %.");
            }

            if (row.SeatNo?.Trim().Length > MaxNumberLength)
            {
                errors.Add($"{label}: the seat number can be at most {MaxNumberLength} characters.");
            }
        }

        return errors;
    }

    /// <summary>A username needs ASCII letters in both names (accents are dropped).</summary>
    private static bool HasLetters(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        try
        {
            CandidateUsername.For(name, "x", "0");
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private async Task<HashSet<string>> TakenUsernamesAsync(List<string> baseNames, CancellationToken cancellationToken)
    {
        var prefixes = baseNames.Distinct().ToList();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prefix in prefixes)
        {
            var matches = await db.Candidates.Where(c => c.Username.StartsWith(prefix)).Select(c => c.Username).ToListAsync(cancellationToken);
            taken.UnionWith(matches);
        }

        return taken;
    }

    /// <summary>The Cambridge username, or with -2, -3… when another session already uses it.</summary>
    private static string FreeUsername(string baseName, HashSet<string> taken)
    {
        var name = baseName.Length > MaxUsernameLength ? baseName[..MaxUsernameLength] : baseName;
        for (var n = 2; taken.Contains(name); n++)
        {
            var suffix = $"-{n}";
            name = (baseName.Length + suffix.Length > MaxUsernameLength ? baseName[..(MaxUsernameLength - suffix.Length)] : baseName) + suffix;
        }

        taken.Add(name);
        return name;
    }

    private async Task<HashSet<int>> StartedCandidateIdsAsync(int examId, CancellationToken cancellationToken) =>
        (await db.Attempts.Where(a => a.Candidate!.ExamId == examId).Select(a => a.CandidateId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
}
