namespace ExamPrep.Domain.Enums;

/// <summary>
/// Staff roles. Every staff account is created by an administrator; candidates are not users but per-exam accounts.
/// </summary>
public enum UserRole
{
    /// <summary>Full control; deletions and account changes need a second administrator's approval.</summary>
    Admin,

    /// <summary>Runs the exam room for the sessions they are assigned to.</summary>
    Invigilator,

    /// <summary>Works in the areas an administrator grants (<see cref="ManagerArea"/>): may create and edit, never delete.</summary>
    Manager,

    /// <summary>Reviews finished work, marks Writing and sees the results of the sessions they are assigned to.</summary>
    Examiner,
}
