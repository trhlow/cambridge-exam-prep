using ExamPrep.Domain.Entities;

namespace ExamPrep.Api.Auth;

/// <summary>What the API exposes about a staff account — never the password hash or security stamp.</summary>
public sealed record StaffProfile(int Id, string Username, string FullName, string? Email, string Role, bool IsActive)
{
    public static StaffProfile From(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, user.Role.ToString(), user.IsActive);
}
