using ExamPrep.Domain.Entities;

namespace ExamPrep.Api.Auth;

/// <summary>What the candidate screen shows after login — never the password, PIN or security stamp.</summary>
public sealed record CandidateProfile(string CandidateNumber, string FullName, string ExamName, bool PinVerified)
{
    public static CandidateProfile From(Candidate candidate, Exam exam, bool pinVerified) =>
        new(candidate.CandidateNumber, $"{candidate.FirstName} {candidate.LastName}", exam.Name, pinVerified);
}
