using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

[ApiController]
[Route("api/staff/auth")]
public sealed class StaffAuthController(
    ExamPrepDbContext db,
    IPasswordHasher<User> passwordHasher,
    LoginThrottle throttle,
    ILogger<StaffAuthController> logger) : ControllerBase
{
    private const string InvalidCredentials = "Invalid username or password.";

    /// <summary>Hash used when the username does not exist, so both failure paths cost the same time.</summary>
    private static readonly Lazy<string> TimingDummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(new User { Username = "-", PasswordHash = "-", FullName = "-" }, Guid.NewGuid().ToString()));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<StaffProfile>> Login(StaffLoginRequest request, CancellationToken cancellationToken)
    {
        var throttleKey = LoginThrottle.StaffKey(request.Username);
        if (throttle.IsLocked(throttleKey))
        {
            logger.LogWarning("Staff sign-in refused for {Username}: too many failed attempts", request.Username);
            return LoginThrottle.Locked();
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(new User { Username = "-", PasswordHash = "-", FullName = "-" }, TimingDummyHash.Value, request.Password);
            return InvalidLogin(request.Username, "unknown username");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidLogin(request.Username, "wrong password");
        }

        if (!user.IsActive)
        {
            return InvalidLogin(request.Username, "account deactivated");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        throttle.Reset(throttleKey);
        await HttpContext.SignInAsync(StaffAuthDefaults.Scheme, StaffAuthenticationSetup.CreatePrincipal(user));
        logger.LogInformation("Staff {UserId} signed in", user.Id);
        return StaffProfile.From(user);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(StaffAuthDefaults.Scheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = StaffAuthDefaults.StaffPolicy)]
    public async Task<ActionResult<StaffProfile>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetStaffId();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, cancellationToken);
        return StaffProfile.From(user);
    }

    private UnauthorizedObjectResult InvalidLogin(string username, string reason)
    {
        throttle.RecordFailure(LoginThrottle.StaffKey(username));
        logger.LogWarning("Staff sign-in failed for {Username}: {Reason}", username, reason);
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = InvalidCredentials,
        });
    }
}
