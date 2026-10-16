using ExamPrep.Domain.Entities;

namespace ExamPrep.Api.Auth;

/// <summary>What the API exposes about a staff account — never the password hash or security stamp.</summary>
/// <param name="Areas">Managers only: the areas they work in (load <see cref="User.Areas"/> first).</param>
public sealed record StaffProfile(int Id, string Username, string FullName, string? Email, string Role, bool IsActive, IReadOnlyList<string> Areas)
{
    public static StaffProfile From(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, user.Role.ToString(), user.IsActive,
            user.Areas.OrderBy(a => a.Area).Select(a => a.Area.ToString()).ToList());
}
