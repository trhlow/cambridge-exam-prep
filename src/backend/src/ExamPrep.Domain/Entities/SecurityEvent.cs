using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>A warning raised for an attempt and shown in the invigilator's Warnings column.</summary>
public class SecurityEvent
{
    public long Id { get; set; }

    public int AttemptId { get; set; }

    public Attempt? Attempt { get; set; }

    public SecurityEventType Type { get; set; }

    public string? Detail { get; set; }

    public DateTime DetectedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedByUserId { get; set; }
}
