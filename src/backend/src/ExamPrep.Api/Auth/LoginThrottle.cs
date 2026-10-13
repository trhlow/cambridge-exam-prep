using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ExamPrep.Api.Auth;

/// <summary>
/// Locks one account (staff account, candidate account, or a candidate's PIN entry) for a few minutes after too
/// many wrong attempts, so passwords and the 4-digit session PIN cannot be guessed by trying them all. Counted per
/// account rather than per address: a whole exam room may reach the server from one address.
/// </summary>
public sealed class LoginThrottle(IMemoryCache cache)
{
    public const int MaxFailures = 10;
    public static readonly TimeSpan LockTime = TimeSpan.FromMinutes(5);
    public const string LockedTitle = "Too many failed attempts. Please wait 5 minutes and try again, or ask the invigilator.";

    private sealed class Counter
    {
        public int Failures;
    }

    /// <summary>
    /// Counted per account id, not per typed name: the database compares names ignoring case and accents, so
    /// "admin", "ADMIN" and "àdmin" reach one account and must share one count.
    /// </summary>
    public static string StaffKey(int userId) => $"login:staff:{userId}";

    public static string CandidateKey(int candidateId) => $"login:candidate:{candidateId}";

    /// <summary>A name with no account, folded the way the database compares names (case and accents ignored).</summary>
    public static string UnknownKey(string kind, string username)
    {
        var folded = new StringBuilder(username.Length);
        foreach (var c in username.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                folded.Append(char.ToLowerInvariant(c));
            }
        }

        return $"login:{kind}:unknown:{folded}";
    }

    public static string PinKey(int candidateId) => $"login:pin:{candidateId}";

    public bool IsLocked(string key) => cache.TryGetValue(key, out Counter? counter) && Volatile.Read(ref counter!.Failures) >= MaxFailures;

    /// <summary>Counts a wrong attempt; the count (and any lock) lasts <see cref="LockTime"/> after the latest one.</summary>
    public void RecordFailure(string key)
    {
        var counter = cache.GetOrCreate(key, _ => new Counter())!;
        Interlocked.Increment(ref counter.Failures);
        cache.Set(key, counter, LockTime);
    }

    public void Reset(string key) => cache.Remove(key);

    public static ObjectResult Locked() => new(new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Title = LockedTitle })
    {
        StatusCode = StatusCodes.Status429TooManyRequests,
    };
}
