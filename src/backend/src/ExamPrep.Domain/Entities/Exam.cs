using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>An exam session (Inspera "parent test"), e.g. A2 Key fS on 20/11/2026 in Room 1.</summary>
public class Exam
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int ExamTypeId { get; set; }

    public ExamType? ExamType { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public string? Room { get; set; }

    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    /// <summary>PIN the invigilator reads out after candidates log in.</summary>
    public required string SessionPin { get; set; }

    /// <summary>Shown to invigilators; needed to exit the lockdown browser or move a candidate to another computer.</summary>
    public required string InvigilatorPassword { get; set; }

    /// <summary>Hex SHA-256 of the Safe Exam Browser config key the server accepts.</summary>
    public string? SebConfigKeyHash { get; set; }

    /// <summary>
    /// Computers per row in the room (seat numbers run row by row). Decides which computers are neighbours when a section
    /// delivers a mix of papers; null means one row.
    /// </summary>
    public int? SeatsPerRow { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ExamSection> Sections { get; set; } = [];

    public ICollection<Candidate> Candidates { get; set; } = [];

    public ICollection<ExamInvigilator> Invigilators { get; set; } = [];

    public ICollection<ExamExaminer> Examiners { get; set; } = [];
}
