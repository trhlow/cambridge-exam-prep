namespace ExamPrep.Api.Auth;

/// <summary>Names shared by the candidate cookie scheme, its claims and authorization policies.</summary>
public static class CandidateAuthDefaults
{
    public const string Scheme = "Candidate";
    public const string CookieName = "victory.candidate";

    public const string ExamIdClaim = "exam_id";
    public const string SecurityStampClaim = "security_stamp";

    /// <summary>"true" once the candidate has entered the session PIN the invigilator read out.</summary>
    public const string PinVerifiedClaim = "pin_verified";

    /// <summary>Signed in with username and password; the PIN may still be missing.</summary>
    public const string SignedInPolicy = "CandidateSignedIn";

    /// <summary>Signed in and past the session PIN: may see and take the exam.</summary>
    public const string ExamPolicy = "CandidateExam";
}
