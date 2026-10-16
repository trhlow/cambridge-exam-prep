using System.ComponentModel.DataAnnotations;
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

/// <param name="NewPassword">Set to reset the password; signs the account out everywhere.</param>
public sealed record UpdateStaffRequest(
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    bool IsActive,
    [StringLength(128, MinimumLength = AdminUsersController.MinPasswordLength)] string? NewPassword = null,
    [EmailAddress, StringLength(256)] string? Email = null);

/// <summary>
/// VE-23, VE-59: staff accounts. Every staff account is created by an administrator: administrators, managers (with
/// the areas they work in), examiners and invigilators. A password reset or deactivation rotates the security stamp,
/// so open sessions end at once.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = StaffAuthDefaults.AdminPolicy)]
public sealed class AdminUsersController(ExamPrepDbContext db, IPasswordHasher<User> passwordHasher, ILogger<AdminUsersController> logger) : ControllerBase
{
    public const int MinPasswordLength = 8;

    [HttpGet]
    public async Task<IReadOnlyList<StaffProfile>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().Include(u => u.Areas).OrderBy(u => u.Username).ToListAsync(cancellationToken);
        return users.Select(StaffProfile.From).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<StaffProfile>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
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

        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
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

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<StaffProfile>> Update(int userId, UpdateStaffRequest request, CancellationToken cancellationToken)
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

        var signOut = user.IsActive != request.IsActive || request.NewPassword is not null;
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? user.Email : request.Email.Trim();
        user.IsActive = request.IsActive;
        if (request.NewPassword is not null)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        }

        if (signOut)
        {
            user.SecurityStamp = Guid.NewGuid();
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Staff {UserId} updated account {TargetId} (active {IsActive}, password reset {Reset})", User.GetStaffId(), userId, user.IsActive, request.NewPassword is not null);
        return StaffProfile.From(user);
    }

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
