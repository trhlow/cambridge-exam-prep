using System.Security.Claims;
using ExamPrep.Domain.Entities;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

public static class CandidateAuthenticationSetup
{
    /// <summary>Long enough for the longest exam day (B1: three sections plus breaks and extra time).</summary>
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(6);

    /// <summary>
    /// Cookie sign-in for candidates, separate from the staff cookie. Every request re-checks the candidate's
    /// security stamp, which changes on each login, so signing in on another computer signs this one out.
    /// </summary>
    public static IServiceCollection AddCandidateAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddSingleton<IPasswordHasher<Candidate>, PasswordHasher<Candidate>>();

        services.AddAuthentication()
            .AddCookie(CandidateAuthDefaults.Scheme, options =>
            {
                ApiCookieOptions.Apply(options, CandidateAuthDefaults.CookieName, SessionLifetime, environment);
                options.Events.OnValidatePrincipal = ValidateCandidateAsync;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(CandidateAuthDefaults.SignedInPolicy, policy => policy
                .AddAuthenticationSchemes(CandidateAuthDefaults.Scheme)
                .RequireAuthenticatedUser())
            .AddPolicy(CandidateAuthDefaults.ExamPolicy, policy => policy
                .AddAuthenticationSchemes(CandidateAuthDefaults.Scheme)
                .RequireAuthenticatedUser()
                .RequireClaim(CandidateAuthDefaults.PinVerifiedClaim, bool.TrueString));

        return services;
    }

    public static ClaimsPrincipal CreatePrincipal(Candidate candidate, bool pinVerified)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, candidate.Id.ToString()),
            new Claim(ClaimTypes.Name, candidate.Username),
            new Claim(CandidateAuthDefaults.ExamIdClaim, candidate.ExamId.ToString()),
            new Claim(CandidateAuthDefaults.SecurityStampClaim, candidate.SecurityStamp.ToString()),
            new Claim(CandidateAuthDefaults.PinVerifiedClaim, pinVerified ? bool.TrueString : bool.FalseString),
        ], CandidateAuthDefaults.Scheme);
        return new ClaimsPrincipal(identity);
    }

    public static int GetCandidateId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static int GetExamId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(CandidateAuthDefaults.ExamIdClaim)!);

    public static bool IsPinVerified(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(CandidateAuthDefaults.PinVerifiedClaim) == bool.TrueString;

    private static async Task ValidateCandidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var idClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stampClaim = principal?.FindFirstValue(CandidateAuthDefaults.SecurityStampClaim);
        if (!int.TryParse(idClaim, out var candidateId) || !Guid.TryParse(stampClaim, out var stamp))
        {
            await RejectAsync(context);
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ExamPrepDbContext>();
        var currentStamp = await db.Candidates
            .AsNoTracking()
            .Where(c => c.Id == candidateId)
            .Select(c => (Guid?)c.SecurityStamp)
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (currentStamp != stamp)
        {
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CandidateAuthDefaults.Scheme);
    }
}
