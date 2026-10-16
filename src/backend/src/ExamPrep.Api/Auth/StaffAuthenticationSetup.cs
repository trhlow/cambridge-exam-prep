using System.Security.Claims;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

public static class StaffAuthenticationSetup
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(10);

    /// <summary>
    /// Cookie sign-in for staff (administrators, managers, examiners, invigilators). API calls get 401/403 instead of login redirects, and
    /// every request re-checks that the account is still active and its security stamp unchanged.
    /// </summary>
    public static IServiceCollection AddStaffAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddAuthentication(StaffAuthDefaults.Scheme)
            .AddCookie(StaffAuthDefaults.Scheme, options =>
            {
                ApiCookieOptions.Apply(options, StaffAuthDefaults.CookieName, SessionLifetime, environment);
                options.Events.OnValidatePrincipal = ValidateAccountAsync;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(StaffAuthDefaults.StaffPolicy, policy => policy
                .AddAuthenticationSchemes(StaffAuthDefaults.Scheme)
                .RequireRole(Enum.GetNames<UserRole>()))
            .AddPolicy(StaffAuthDefaults.AdminPolicy, policy => policy
                .AddAuthenticationSchemes(StaffAuthDefaults.Scheme)
                .RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(StaffAuthDefaults.PapersPolicy, policy => AreaPolicy(policy, ManagerArea.Papers))
            .AddPolicy(StaffAuthDefaults.ExamsPolicy, policy => AreaPolicy(policy, ManagerArea.Exams))
            .AddPolicy(StaffAuthDefaults.CandidatesPolicy, policy => AreaPolicy(policy, ManagerArea.Candidates))
            .AddPolicy(StaffAuthDefaults.CentrePolicy, policy => AreaPolicy(policy, ManagerArea.Centre))
            .AddPolicy(StaffAuthDefaults.CandidateListPolicy, policy => AreaPolicy(policy, ManagerArea.Exams, ManagerArea.Candidates))
            .AddPolicy(StaffAuthDefaults.ResultsPolicy, policy => policy
                .AddAuthenticationSchemes(StaffAuthDefaults.Scheme)
                .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Examiner)))
            .AddPolicy(StaffAuthDefaults.ExaminerPolicy, policy => policy
                .AddAuthenticationSchemes(StaffAuthDefaults.Scheme)
                .RequireRole(nameof(UserRole.Examiner)));

        return services;
    }

    /// <param name="user">With <see cref="User.Areas"/> loaded: a manager's areas become claims.</param>
    public static ClaimsPrincipal CreatePrincipal(User user)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(StaffAuthDefaults.SecurityStampClaim, user.SecurityStamp.ToString()),
            .. user.Areas.Select(a => new Claim(StaffAuthDefaults.AreaClaim, a.Area.ToString())),
        ], StaffAuthDefaults.Scheme);
        return new ClaimsPrincipal(identity);
    }

    public static int GetStaffId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Administrators work in every area; managers in the areas they were given.</summary>
    public static bool WorksIn(this ClaimsPrincipal principal, ManagerArea area) =>
        principal.IsInRole(nameof(UserRole.Admin))
        || (principal.IsInRole(nameof(UserRole.Manager)) && principal.HasClaim(StaffAuthDefaults.AreaClaim, area.ToString()));

    private static void AreaPolicy(AuthorizationPolicyBuilder policy, params ManagerArea[] areas) => policy
        .AddAuthenticationSchemes(StaffAuthDefaults.Scheme)
        .RequireAssertion(context => areas.Any(context.User.WorksIn));

    private static async Task ValidateAccountAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var idClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stampClaim = principal?.FindFirstValue(StaffAuthDefaults.SecurityStampClaim);
        if (!int.TryParse(idClaim, out var userId) || !Guid.TryParse(stampClaim, out var stamp))
        {
            await RejectAsync(context);
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ExamPrepDbContext>();
        var account = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.SecurityStamp })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (account is null || !account.IsActive || account.SecurityStamp != stamp)
        {
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(StaffAuthDefaults.Scheme);
    }
}
