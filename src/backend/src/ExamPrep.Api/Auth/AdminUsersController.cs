using System.ComponentModel.DataAnnotations;
using ExamPrep.Api.Approvals;
using ExamPrep.Api.Common;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

public sealed record CreateStaffRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "The username may only contain letters, digits, dots, dashes and underscores.")] string Username,
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    [Required, StringLength(128, MinimumLength = AdminUsersController.MinPasswordLength)] string Password,
    [Required] string Role,
    [EmailAddress, StringLength(256)] string? Email = null,
    IReadOnlyList<string>? Areas = null);

/// <param name="NewPassword">Set to reset the password; signs the account out everywhere once approved.</param>
/// <param name="Role">Set to change the role; with <paramref name="Areas"/> for a manager.</param>
/// <param name="Areas">A manager's areas; omit to keep them.</param>
public sealed record UpdateStaffRequest(
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    bool IsActive,
    [StringLength(128, MinimumLength = AdminUsersController.MinPasswordLength)] string? NewPassword = null,
    [EmailAddress, StringLength(256)] string? Email = null,
    string? Role = null,
    IReadOnlyList<string>? Areas = null);

/// <param name="Request">The account change waiting for another administrator, if the update asked for one.</param>
public sealed record UpdateStaffResult(StaffProfile User, ApprovalView? Request);

/// <summary>
/// VE-23, VE-59: staff accounts. Every staff account is created by an administrator: administrators, managers (with
/// the areas they work in), examiners and invigilators. A password reset or deactivation rotates the security stamp,
/// so open sessions end at once.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminUsersController(ExamPrepDbContext db, IPasswordHasher<User> passwordHasher, ApprovalService approvals, ILogger<AdminUsersController> logger) : ControllerBase
{
    public const int MinPasswordLength = 8;

    [HttpGet]
    public async Task<IReadOnlyList<StaffProfile>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().Include(u => u.Areas).OrderBy(u => u.Username).ToListAsync(cancellationToken);
        return users.Select(StaffProfile.From).ToList();
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.GetNames<UserRole>().Contains(request.Role))
        {
            return Problems.Invalid($"Role must be one of {string.Join(", ", Enum.GetNames<UserRole>())}.");
        }

        var role = Enum.Parse<UserRole>(request.Role);
        var (areas, areaError) = ParseAreas(role, request.Areas);
        if (areaError is not null)
        {
            return Problems.Invalid(areaError);
        }

        var username = request.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            return Problems.Conflict($"The username {username} is already used.");
        }

        var fullName = request.FullName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (role == UserRole.Admin && await db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive, cancellationToken) >= 2)
        {
            // VE-60: the second administrator is created directly (nobody else could approve); later ones need approval.
            var payload = ApprovalActions.Serialize(new NewAdministrator(username, fullName, email, passwordHasher.HashPassword(new User { Username = username, FullName = fullName, PasswordHash = string.Empty }, request.Password)));
            var (pending, conflict) = await approvals.RequestAsync(ApprovalKind.CreateAdmin, null, $"Create administrator {username} ({fullName})", payload, User.GetStaffId(), cancellationToken);
            return pending is null ? Problems.Conflict(conflict!) : Accepted(pending);
        }

        var user = new User
        {
            Username = username,
            FullName = fullName,
            Email = email,
            Role = role,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Areas = areas.Select(area => new UserArea { Area = area }).ToList(),
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} created {Role} account {NewUserId}", User.GetStaffId(), user.Role, user.Id);
        return Created($"/api/admin/users/{user.Id}", StaffProfile.From(user));
    }

    /// <summary>
    /// The name and email change at once. Switching the account on or off, a new password, or a new role or areas
    /// wait for an administrator other than the requester (VE-60); once approved the account is signed out.
    /// </summary>
    [HttpPut("{userId:int}")]
    public async Task<ActionResult<UpdateStaffResult>> Update(int userId, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.Areas).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (userId == User.GetStaffId() && !request.IsActive)
        {
            return Problems.Invalid("You cannot switch off your own account.");
        }

        if (request.Role is not null && !Enum.GetNames<UserRole>().Contains(request.Role))
        {
            return Problems.Invalid($"Role must be one of {string.Join(", ", Enum.GetNames<UserRole>())}.");
        }

        var role = request.Role is null ? user.Role : Enum.Parse<UserRole>(request.Role);
        var currentAreas = user.Areas.Select(a => a.Area.ToString()).ToList();
        var (areas, areaError) = ParseAreas(role, request.Areas ?? (role == UserRole.Manager ? currentAreas : []));
        if (areaError is not null)
        {
            return Problems.Invalid(areaError);
        }

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? user.Email : request.Email.Trim();
        await db.SaveChangesAsync(cancellationToken);

        var areaNames = areas.Select(a => a.ToString()).ToList();
        var change = new StaffAccountChange(
            request.IsActive != user.IsActive ? request.IsActive : null,
            role != user.Role ? role.ToString() : null,
            role == UserRole.Manager && !areaNames.Order().SequenceEqual(currentAreas.Order()) ? areaNames : null,
            request.NewPassword is null ? null : passwordHasher.HashPassword(user, request.NewPassword));
        ApprovalView? pending = null;
        if (change.IsActive is not null || change.Role is not null || change.Areas is not null || change.PasswordHash is not null)
        {
            var (created, conflict) = await approvals.RequestAsync(
                ApprovalKind.ChangeStaffAccount, userId, $"Change account {user.Username} ({user.FullName}): {Describe(change)}", ApprovalActions.Serialize(change), User.GetStaffId(), cancellationToken);
            if (created is null)
            {
                return Problems.Conflict(conflict!);
            }

            pending = created;
        }

        logger.LogInformation("Staff {UserId} updated account {TargetId} (approval {RequestId})", User.GetStaffId(), userId, pending?.Id);
        return new UpdateStaffResult(StaffProfile.From(user), pending);
    }

    private static string Describe(StaffAccountChange change) => string.Join("; ", new[]
    {
        change.IsActive switch { true => "switch on", false => "switch off", null => null },
        change.PasswordHash is null ? null : "reset the password",
        change.Role is null ? null : $"role {change.Role}",
        change.Areas is null ? null : $"areas {(change.Areas.Count == 0 ? "none" : string.Join(", ", change.Areas))}",
    }.OfType<string>());

    /// <summary>Managers need at least one area; nobody else has any.</summary>
    private static (IReadOnlyList<ManagerArea> Areas, string? Error) ParseAreas(UserRole role, IReadOnlyList<string>? names)
    {
        names ??= [];
        var unknown = names.Where(n => !Enum.GetNames<ManagerArea>().Contains(n)).ToList();
        if (unknown.Count > 0)
        {
            return ([], $"Unknown area(s): {string.Join(", ", unknown)}. Areas are {string.Join(", ", Enum.GetNames<ManagerArea>())}.");
        }

        var areas = names.Distinct().Select(Enum.Parse<ManagerArea>).Order().ToList();
        return role switch
        {
            UserRole.Manager when areas.Count == 0 => ([], "A manager needs at least one area to work in."),
            not UserRole.Manager when areas.Count > 0 => ([], "Only managers have areas."),
            _ => (areas, null),
        };
    }
}
