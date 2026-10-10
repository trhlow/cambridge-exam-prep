using System.ComponentModel.DataAnnotations;

namespace ExamPrep.Api.Auth;

public sealed record StaffLoginRequest(
    [Required, StringLength(64)] string Username,
    [Required, StringLength(128)] string Password);
