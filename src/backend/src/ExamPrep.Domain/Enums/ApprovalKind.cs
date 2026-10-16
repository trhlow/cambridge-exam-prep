namespace ExamPrep.Domain.Enums;

/// <summary>Actions that wait for a second person (Victory: no single administrator may delete or change accounts alone).</summary>
public enum ApprovalKind
{
    DeletePaper,
    DeleteExam,
    DeleteCandidate,

    /// <summary>Switch a staff account on or off, reset its password, or change its role or areas.</summary>
    ChangeStaffAccount,

    /// <summary>A third (or later) administrator; the second one is created directly.</summary>
    CreateAdmin,
}
