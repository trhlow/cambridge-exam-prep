using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>
/// An action waiting for an administrator other than the requester. Kept after the decision: the table is the log
/// both administrators check each other with.
/// </summary>
public class ApprovalRequest
{
    public int Id { get; set; }

    public ApprovalKind Kind { get; set; }

    /// <summary>The paper, exam, candidate or staff account the action is about (none for a new administrator).</summary>
    public int? TargetId { get; set; }

    /// <summary>What will happen, in a sentence, written when the request is made.</summary>
    public required string Summary { get; set; }

    /// <summary>Details of an account change or a new administrator (JSON; passwords only as hashes).</summary>
    public string? PayloadJson { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public int RequestedByUserId { get; set; }

    public User? RequestedBy { get; set; }

    public DateTime RequestedAt { get; set; }

    public int? DecidedByUserId { get; set; }

    public User? DecidedBy { get; set; }

    public DateTime? DecidedAt { get; set; }

    /// <summary>Why the request was rejected.</summary>
    public string? DecisionNote { get; set; }

    /// <summary>Why an approved request could not be carried out.</summary>
    public string? Outcome { get; set; }
}
