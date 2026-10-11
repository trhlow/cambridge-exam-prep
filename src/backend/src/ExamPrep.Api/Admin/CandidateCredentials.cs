using System.Security.Cryptography;
using ExamPrep.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace ExamPrep.Api.Admin;

/// <summary>
/// Candidate passwords: hashed for sign-in, and also encrypted with ASP.NET Data Protection so the login slip can be
/// printed again (Cambridge centres re-print slips on the day).
/// </summary>
public sealed class CandidateCredentials(IPasswordHasher<Candidate> hasher, IDataProtectionProvider protection)
{
    public const string ProtectorPurpose = "ExamPrep.CandidatePassword";

    /// <summary>No 0/o, 1/l/i: candidates copy the password from paper.</summary>
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";
    private const int PasswordLength = 8;

    private readonly IDataProtector protector = protection.CreateProtector(ProtectorPurpose);

    public static string NewPassword() => RandomNumberGenerator.GetString(PasswordAlphabet, PasswordLength);

    public void SetPassword(Candidate candidate, string password)
    {
        candidate.PasswordProtected = protector.Protect(password);
        candidate.PasswordHash = hasher.HashPassword(candidate, password);
    }

    /// <summary>The password for the login slip; null when it can no longer be decrypted.</summary>
    public string? Reveal(Candidate candidate)
    {
        try
        {
            return protector.Unprotect(candidate.PasswordProtected);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
