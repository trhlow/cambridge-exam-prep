using System.ComponentModel.DataAnnotations;

namespace ExamPrep.Api.Auth;

/// <summary>The username and password printed on the candidate's login slip.</summary>
public sealed record CandidateLoginRequest(
    [Required, StringLength(64)] string Username,
    [Required, StringLength(64)] string Password);

/// <summary>The session PIN the invigilator reads out to the room.</summary>
public sealed record CandidatePinRequest([Required, StringLength(16)] string Pin);

/// <summary>The Options screen: text size (Regular, Large, ExtraLarge) and contrast (BlackOnWhite, WhiteOnBlack, YellowOnBlack).</summary>
public sealed record CandidatePreferencesRequest([Required] string TextSize, [Required] string Contrast);
