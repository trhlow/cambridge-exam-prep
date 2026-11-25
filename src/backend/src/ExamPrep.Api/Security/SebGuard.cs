using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExamPrep.Api.Security;

/// <summary>
/// VE-28: Safe Exam Browser. An exam may require SEB by storing the Config Key of its SEB settings file. SEB then
/// sends, on every request, <c>X-SafeExamBrowser-ConfigKeyHash</c> = hex SHA-256 of the absolute request URL followed
/// by that Config Key; the server recomputes it, so a normal browser (or SEB with other settings) is refused.
/// </summary>
public static class SebGuard
{
    public const string ConfigKeyHashHeader = "X-SafeExamBrowser-ConfigKeyHash";
    public const string RequiredTitle = "Open this test in Safe Exam Browser.";

    /// <summary>A Config Key is itself a SHA-256: 64 hexadecimal characters.</summary>
    public static bool IsConfigKey(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);

    /// <summary>The URL as the browser requested it (behind nginx the scheme comes from X-Forwarded-Proto).</summary>
    public static string RequestUrl(HttpRequest request)
    {
        var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault() is { Length: > 0 } forwarded ? forwarded : request.Scheme;
        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }

    public static string ExpectedHash(string url, string configKey) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(url + configKey.ToLowerInvariant())));

    public static bool Passes(HttpRequest request, string configKey)
    {
        var sent = request.Headers[ConfigKeyHashHeader].FirstOrDefault();
        if (sent is null || sent.Length != 64)
        {
            return false;
        }

        var url = RequestUrl(request);
        // A WebSocket handshake is an http(s) request to the server but a ws(s) URL to the browser: either form counts.
        var urls = request.HttpContext.WebSockets.IsWebSocketRequest ? [url, "ws" + url[4..]] : new[] { url };
        var sentBytes = Encoding.ASCII.GetBytes(sent.ToLowerInvariant());
        return urls.Any(u => CryptographicOperations.FixedTimeEquals(sentBytes, Encoding.ASCII.GetBytes(ExpectedHash(u, configKey))));
    }
}

/// <summary>Marks a candidate action that runs without SEB: signing in checks it itself, signing out always works.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SkipSebCheckAttribute : Attribute;

/// <summary>
/// Refuses every candidate request (API and exam files) of an exam that requires SEB unless it comes from SEB with
/// the right settings, and raises an <see cref="SecurityEventType.InvalidSebKey"/> warning for a running attempt.
/// A browser that also holds a staff cookie (only the asset endpoint reads both) keeps the staff member's own rights:
/// the candidate identity is dropped from the request instead of refusing it.
/// </summary>
public sealed class SebActionFilter(ExamPrepDbContext db, IMemoryCache cache, SecurityEventRecorder events) : IAsyncActionFilter
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(10);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var candidate = user.Identities.FirstOrDefault(i => i.IsAuthenticated && i.AuthenticationType == CandidateAuthDefaults.Scheme);
        var skip = context.ActionDescriptor.EndpointMetadata.OfType<SkipSebCheckAttribute>().Any();
        if (candidate is null || skip || await PassesAsync(db, cache, context.HttpContext.Request, new ClaimsPrincipal(candidate).GetExamId()))
        {
            await next();
            return;
        }

        await RecordAsync(db, events, new ClaimsPrincipal(candidate).GetCandidateId());
        var staff = user.Identities.Where(i => i != candidate && i.IsAuthenticated && i.AuthenticationType == StaffAuthDefaults.Scheme).ToList();
        if (staff.Count > 0)
        {
            context.HttpContext.User = new ClaimsPrincipal(staff);
            await next();
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = SebGuard.RequiredTitle })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }

    /// <summary>True when the exam does not require SEB or the request carries the right Config Key hash.</summary>
    public static async Task<bool> PassesAsync(ExamPrepDbContext db, IMemoryCache cache, HttpRequest request, int examId)
    {
        var configKey = await cache.GetOrCreateAsync($"seb:{examId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            return db.Exams.AsNoTracking().Where(e => e.Id == examId).Select(e => e.SebConfigKeyHash).SingleOrDefaultAsync();
        });
        return configKey is null || SebGuard.Passes(request, configKey);
    }

    /// <summary>The InvalidSebKey warning, while the candidate has a running attempt.</summary>
    public static async Task RecordAsync(ExamPrepDbContext db, SecurityEventRecorder events, int candidateId)
    {
        var running = await db.Attempts.AsNoTracking()
            .Where(a => a.CandidateId == candidateId && (a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Paused))
            .Select(a => new { a.Id, a.ExamSectionId })
            .FirstOrDefaultAsync();
        if (running is not null)
        {
            await events.RecordAsync(running.Id, running.ExamSectionId, SecurityEventType.InvalidSebKey,
                "A request came from a browser other than Safe Exam Browser with this test's settings.", TimeSpan.FromMinutes(5), CancellationToken.None);
        }
    }

    /// <summary>Forgets an exam's key after an administrator changes it.</summary>
    public static void Forget(IMemoryCache cache, int examId) => cache.Remove($"seb:{examId}");
}
