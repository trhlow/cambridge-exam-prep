namespace ExamPrep.Domain.Enums;

/// <summary>What a manager (<see cref="UserRole.Manager"/>) looks after; one account may hold several areas.</summary>
public enum ManagerArea
{
    /// <summary>Import, review, edit and publish papers.</summary>
    Papers,

    /// <summary>Exam sessions: papers per section, PIN, status, invigilators and examiners.</summary>
    Exams,

    /// <summary>Candidates of a session, their login slips and passwords.</summary>
    Candidates,

    /// <summary>Centre name and logo.</summary>
    Centre,
}
