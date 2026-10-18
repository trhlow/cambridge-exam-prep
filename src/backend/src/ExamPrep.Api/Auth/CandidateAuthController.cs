using System.Security.Cryptography;
using System.Text;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

/// <summary>
/// Candidate sign-in in two steps, as in the Cambridge exam room: the printed username and password,
/// then the session PIN the invigilator reads out once everyone is logged in.
/// </summary>
[ApiController]
[Route("api/candidate/auth")]
public sealed class CandidateAuthController(
    ExamPrepDbContext db,
    IPasswordHasher<Candidate> passwordHasher,
    LoginThrottle throttle,
    ILogger<CandidateAuthController> logger) : ControllerBase
{
    private const string InvalidCredentials = "Invalid username or password.";
    private const string ExamNotOpen = "This exam is not open.";
    private const string IncorrectPin = "Incorrect PIN.";

    /// <summary>Used when the username does not exist, so both failure paths cost the same hashing time.</summary>
    private static readonly Candidate TimingDummy = new()
    {
        CandidateNumber = "-",
        FirstName = "-",
        LastName = "-",
        Username = "-",
        PasswordHash = "-",
        PasswordProtected = "-",
    };

    private static readonly Lazy<string> TimingDummyHash = new(() =>
        new PasswordHasher<Candidate>().HashPassword(TimingDummy, Guid.NewGuid().ToString()));

    [HttpPost("login")]
    [AllowAnonymous]
    [Security.SkipSebCheck]
    public async Task<ActionResult<CandidateProfile>> Login(CandidateLoginRequest request, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates
            .Include(c => c.Exam)
            .SingleOrDefaultAsync(c => c.Username == request.Username, cancellationToken);
        var throttleKey = candidate is null ? LoginThrottle.UnknownKey("candidate", request.Username) : LoginThrottle.CandidateKey(candidate.Id);
        if (throttle.IsLocked(throttleKey))
        {
            logger.LogWarning("Candidate sign-in refused for {Username}: too many failed attempts", request.Username);
            return LoginThrottle.Locked();
        }

        if (candidate is null)
        {
            passwordHasher.VerifyHashedPassword(TimingDummy, TimingDummyHash.Value, request.Password);
            return InvalidLogin(throttleKey, request.Username, "unknown username");
        }

        var verification = passwordHasher.VerifyHashedPassword(candidate, candidate.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidLogin(throttleKey, request.Username, "wrong password");
        }

        var exam = candidate.Exam!;
        if (exam.Status != ExamStatus.Active)
        {
            logger.LogWarning("Candidate {CandidateId} tried to sign in to exam {ExamId} with status {Status}", candidate.Id, exam.Id, exam.Status);
            return Problem(title: ExamNotOpen, statusCode: StatusCodes.Status403Forbidden);
        }

        // VE-28: an exam that requires Safe Exam Browser cannot even be signed in to from another browser.
        if (exam.SebConfigKeyHash is { } sebKey && !Security.SebGuard.Passes(Request, sebKey))
        {
            logger.LogWarning("Candidate {CandidateId} tried to sign in to exam {ExamId} outside Safe Exam Browser", candidate.Id, exam.Id);
            return Problem(title: Security.SebGuard.RequiredTitle, statusCode: StatusCodes.Status403Forbidden);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            candidate.PasswordHash = passwordHasher.HashPassword(candidate, request.Password);
        }

        throttle.Reset(throttleKey);

        // A new stamp signs out any other computer this candidate was using.
        candidate.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync(cancellationToken);

        await HttpContext.SignInAsync(CandidateAuthDefaults.Scheme, CandidateAuthenticationSetup.CreatePrincipal(candidate, pinVerified: false));
        logger.LogInformation("Candidate {CandidateId} signed in to exam {ExamId}", candidate.Id, exam.Id);
        return CandidateProfile.From(candidate, exam, pinVerified: false);
    }

    [HttpPost("pin")]
    [Authorize(Policy = CandidateAuthDefaults.SignedInPolicy)]
    public async Task<ActionResult<CandidateProfile>> EnterPin(CandidatePinRequest request, CancellationToken cancellationToken)
    {
        var candidateId = User.GetCandidateId();
        var candidate = await db.Candidates
            .AsNoTracking()
            .Include(c => c.Exam)
            .SingleAsync(c => c.Id == candidateId, cancellationToken);
        var exam = candidate.Exam!;

        if (exam.Status != ExamStatus.Active)
        {
            return Problem(title: ExamNotOpen, statusCode: StatusCodes.Status403Forbidden);
        }

        var pinKey = LoginThrottle.PinKey(candidate.Id);
        if (throttle.IsLocked(pinKey))
        {
            logger.LogWarning("Candidate {CandidateId} PIN entry refused: too many wrong PINs", candidate.Id);
            return LoginThrottle.Locked();
        }

        if (!PinMatches(request.Pin, exam.SessionPin))
        {
            throttle.RecordFailure(pinKey);
            logger.LogWarning("Candidate {CandidateId} entered an incorrect PIN for exam {ExamId}", candidate.Id, exam.Id);
            return Problem(title: IncorrectPin, statusCode: StatusCodes.Status400BadRequest);
        }

        throttle.Reset(pinKey);
        await HttpContext.SignInAsync(CandidateAuthDefaults.Scheme, CandidateAuthenticationSetup.CreatePrincipal(candidate, pinVerified: true));
        return CandidateProfile.From(candidate, exam, pinVerified: true);
    }

    [HttpPost("logout")]
    [Security.SkipSebCheck]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CandidateAuthDefaults.Scheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = CandidateAuthDefaults.SignedInPolicy)]
    public async Task<ActionResult<CandidateProfile>> Me(CancellationToken cancellationToken)
    {
        var candidateId = User.GetCandidateId();
        var candidate = await db.Candidates
            .AsNoTracking()
            .Include(c => c.Exam)
            .SingleAsync(c => c.Id == candidateId, cancellationToken);
        return CandidateProfile.From(candidate, candidate.Exam!, User.IsPinVerified());
    }

    /// <summary>VE-18: saves the Options screen's text size and contrast for this candidate.</summary>
    [HttpPut("preferences")]
    [Authorize(Policy = CandidateAuthDefaults.SignedInPolicy)]
    public async Task<ActionResult<CandidateProfile>> SavePreferences(CandidatePreferencesRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.GetNames<TextSize>().Contains(request.TextSize) || !Enum.GetNames<ContrastMode>().Contains(request.Contrast))
        {
            return Problem(title: "Unknown text size or contrast.", statusCode: StatusCodes.Status400BadRequest);
        }

        var candidateId = User.GetCandidateId();
        var candidate = await db.Candidates.Include(c => c.Exam).SingleAsync(c => c.Id == candidateId, cancellationToken);
        candidate.TextSize = Enum.Parse<TextSize>(request.TextSize);
        candidate.Contrast = Enum.Parse<ContrastMode>(request.Contrast);
        await db.SaveChangesAsync(cancellationToken);
        return CandidateProfile.From(candidate, candidate.Exam!, User.IsPinVerified());
    }

    private static bool PinMatches(string entered, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(entered.Trim()), Encoding.UTF8.GetBytes(expected));

    private UnauthorizedObjectResult InvalidLogin(string throttleKey, string username, string reason)
    {
        throttle.RecordFailure(throttleKey);
        logger.LogWarning("Candidate sign-in failed for {Username}: {Reason}", username, reason);
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = InvalidCredentials,
        });
    }
}
