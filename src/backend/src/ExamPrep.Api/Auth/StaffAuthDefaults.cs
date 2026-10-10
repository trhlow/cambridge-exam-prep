namespace ExamPrep.Api.Auth;

/// <summary>Names shared by the staff cookie scheme, its claims and authorization policies.</summary>
public static class StaffAuthDefaults
{
    public const string Scheme = "Staff";
    public const string CookieName = "victory.staff";
    public const string SecurityStampClaim = "security_stamp";

    /// <summary>Any signed-in staff member (admin or invigilator).</summary>
    public const string StaffPolicy = "Staff";

    /// <summary>Administrators only.</summary>
    public const string AdminPolicy = "Admin";
}
