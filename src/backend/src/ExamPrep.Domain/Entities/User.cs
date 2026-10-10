using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>Staff account: administrator or invigilator. Candidates are separate per-exam accounts.</summary>
public class User
{
    public int Id { get; set; }

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public required string FullName { get; set; }

    public string? Email { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
