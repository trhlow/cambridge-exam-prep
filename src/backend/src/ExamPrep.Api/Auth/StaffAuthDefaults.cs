namespace ExamPrep.Api.Auth;

/// <summary>Names shared by the staff cookie scheme, its claims and authorization policies.</summary>
public static class StaffAuthDefaults
{
    public const string Scheme = "Staff";
    public const string CookieName = "victory.staff";
    public const string SecurityStampClaim = "security_stamp";

    /// <summary>One claim per <see cref="Domain.Enums.ManagerArea"/> a manager works in.</summary>
    public const string AreaClaim = "area";

    /// <summary>Any signed-in staff member.</summary>
    public const string StaffPolicy = "Staff";

    /// <summary>Administrators only.</summary>
    public const string AdminPolicy = "Admin";

    /// <summary>Administrators, and managers of that area (create and edit; never delete).</summary>
    public const string PapersPolicy = "Papers";
    public const string ExamsPolicy = "Exams";
    public const string CandidatesPolicy = "Candidates";
    public const string CentrePolicy = "Centre";

    /// <summary>Reading a session's candidate list: administrators and managers of exams or candidates.</summary>
    public const string CandidateListPolicy = "CandidateList";

    /// <summary>The score table: administrators (all sessions) and examiners (their sessions).</summary>
    public const string ResultsPolicy = "Results";

    /// <summary>Candidates' work and Writing marks: examiners only.</summary>
    public const string ExaminerPolicy = "Examiner";
}
