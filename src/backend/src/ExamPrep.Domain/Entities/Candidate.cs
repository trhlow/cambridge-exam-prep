using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>A candidate entered for one exam session, with the login printed on their slip.</summary>
public class Candidate
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public required string CandidateNumber { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    /// <summary>First 5 letters of the family name + first initial + candidate number, as on Cambridge slips.</summary>
    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    /// <summary>Password encrypted with ASP.NET Data Protection so login slips can be re-printed.</summary>
    public required string PasswordProtected { get; set; }

    public string? SeatNo { get; set; }

    /// <summary>Changes on every login, so moving to another computer signs the previous one out.</summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    /// <summary>Approved special-arrangement extra time, as a percentage of each section's duration.</summary>
    public int PlannedExtraPercent { get; set; }

    /// <summary>Set when the exam was ended for this candidate at parent level; null means "use test end time".</summary>
    public DateTime? ParentEndOverrideAt { get; set; }

    public TextSize TextSize { get; set; } = TextSize.Regular;

    public ContrastMode Contrast { get; set; } = ContrastMode.BlackOnWhite;

    public DateTime CreatedAt { get; set; }

    public ICollection<Attempt> Attempts { get; set; } = [];
}
