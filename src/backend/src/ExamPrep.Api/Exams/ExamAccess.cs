using System.Security.Claims;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Api.Exams;

/// <summary>
/// Which sessions a staff member reaches, by what they do with them (VE-72, Victory 29/10: several people work in each
/// area and each sees only their own work). Administrators reach every session. A manager of exams organises the
/// sessions they created; a manager of candidates works in the sessions the organiser assigned them to; invigilators
/// reach only the sessions they are assigned to. Examiners do not open sessions at all (VE-73): they mark the Writing
/// scripts dealt to them, by code. Everything else answers 404, as if the session did not exist.
/// </summary>
public static class ExamAccess
{
    private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));

    /// <summary>The session list and a session's page.</summary>
    public static IQueryable<Exam> VisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var organises = user.WorksIn(ManagerArea.Exams);
        var entersCandidates = user.WorksIn(ManagerArea.Candidates);
        return exams.Where(e =>
            (organises && e.CreatedByUserId == userId)
            || (entersCandidates && e.CandidateManagers.Any(m => m.UserId == userId))
            || e.Invigilators.Any(i => i.UserId == userId));
    }

    /// <summary>Changing the session itself: its papers, status, staff, deletion. Its creator (a manager of exams).</summary>
    public static IQueryable<Exam> OrganisedBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var organises = user.WorksIn(ManagerArea.Exams);
        return exams.Where(e => organises && e.CreatedByUserId == userId);
    }

    /// <summary>Entering and changing candidates: the managers of candidates assigned to the session.</summary>
    public static IQueryable<Exam> CandidatesManagedBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var entersCandidates = user.WorksIn(ManagerArea.Candidates);
        var organises = user.WorksIn(ManagerArea.Exams);
        return exams.Where(e => entersCandidates && (e.CandidateManagers.Any(m => m.UserId == userId) || (organises && e.CreatedByUserId == userId)));
    }

    /// <summary>The candidate list: the session's organiser and its managers of candidates.</summary>
    public static IQueryable<Exam> CandidatesListedFor(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var organises = user.WorksIn(ManagerArea.Exams);
        var entersCandidates = user.WorksIn(ManagerArea.Candidates);
        return exams.Where(e => (organises && e.CreatedByUserId == userId) || (entersCandidates && e.CandidateManagers.Any(m => m.UserId == userId)));
    }

    /// <summary>Candidates' login slips, with their passwords: the people who prepare or run the room.</summary>
    public static IQueryable<Exam> SlipsVisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var organises = user.WorksIn(ManagerArea.Exams);
        var entersCandidates = user.WorksIn(ManagerArea.Candidates);
        return exams.Where(e =>
            (organises && e.CreatedByUserId == userId)
            || (entersCandidates && e.CandidateManagers.Any(m => m.UserId == userId))
            || e.Invigilators.Any(i => i.UserId == userId));
    }

    /// <summary>The live monitoring page, invigilator actions and messages: the organiser and the invigilators.</summary>
    public static IQueryable<Exam> MonitorableBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (IsAdmin(user))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        var organises = user.WorksIn(ManagerArea.Exams);
        return exams.Where(e => (organises && e.CreatedByUserId == userId) || e.Invigilators.Any(i => i.UserId == userId));
    }

    /// <summary>Scores with candidates' names: administrators only (VE-73).</summary>
    public static IQueryable<Exam> ResultsVisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user) =>
        IsAdmin(user) ? exams : exams.Where(_ => false);

    /// <summary>Writing scripts to mark: examiners assigned to the session (each then sees only the scripts dealt to them).</summary>
    public static IQueryable<Exam> MarkableBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        var userId = user.GetStaffId();
        return exams.Where(e => e.Examiners.Any(x => x.UserId == userId));
    }
}
