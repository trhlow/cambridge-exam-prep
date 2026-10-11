using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExamPrep.Api.Auth;

/// <summary>Cookie settings shared by the staff and candidate schemes.</summary>
internal static class ApiCookieOptions
{
    /// <summary>
    /// HttpOnly, SameSite=Strict, Secure in production, sliding expiry, and 401/403 status codes instead of
    /// login redirects, because every caller is the single-page app, not a browser navigation.
    /// </summary>
    public static void Apply(CookieAuthenticationOptions options, string cookieName, TimeSpan lifetime, IWebHostEnvironment environment)
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = lifetime;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context => WriteStatus(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => WriteStatus(context, StatusCodes.Status403Forbidden);
    }

    private static Task WriteStatus(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
