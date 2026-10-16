namespace ExamPrep.Domain.Enums;

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,

    /// <summary>Approved, but the action could no longer be carried out (e.g. the paper is now used by a session).</summary>
    Failed,
}
