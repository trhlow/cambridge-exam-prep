using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>Audit log of every invigilator operation. A multi-candidate action shares one <see cref="BatchId"/>.</summary>
public class InvigilatorAction
{
    public long Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int? AttemptId { get; set; }

    public Attempt? Attempt { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public InvigilatorActionType ActionType { get; set; }

    /// <summary>Action parameters as JSON, e.g. minutes added or the new end time.</summary>
    public string? PayloadJson { get; set; }

    /// <summary>Incident reason selected by the invigilator.</summary>
    public string? Reason { get; set; }

    public Guid BatchId { get; set; }

    public DateTime CreatedAt { get; set; }
}
