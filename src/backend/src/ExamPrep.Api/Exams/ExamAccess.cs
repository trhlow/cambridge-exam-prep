using System.Security.Claims;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Api.Exams;

/// <summary>
/// Which sessions a staff member reaches, by what they do with them. Administrators reach every session; managers
/// of exams or candidates organise every session; invigilators and examiners reach only the sessions they are
/// assigned to. Everything else answers 404, as if the session did not exist.
/// </summary>
public static class ExamAccess
{
    /// <summary>The session list and a session's page.</summary>
    public static IQueryable<Exam> VisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (user.WorksIn(ManagerArea.Exams) || user.WorksIn(ManagerArea.Candidates))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        return exams.Where(e => e.Invigilators.Any(i => i.UserId == userId) || e.Examiners.Any(x => x.UserId == userId));
    }

    /// <summary>Candidates' login slips, with their passwords: the people who prepare or run the room.</summary>
    public static IQueryable<Exam> SlipsVisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (user.WorksIn(ManagerArea.Exams) || user.WorksIn(ManagerArea.Candidates))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        return exams.Where(e => e.Invigilators.Any(i => i.UserId == userId));
    }

    /// <summary>The live monitoring page, invigilator actions and messages.</summary>
    public static IQueryable<Exam> MonitorableBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (user.WorksIn(ManagerArea.Exams))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        return exams.Where(e => e.Invigilators.Any(i => i.UserId == userId));
    }

    /// <summary>Scores: administrators see every session's table, examiners their own sessions.</summary>
    public static IQueryable<Exam> ResultsVisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        return exams.Where(e => e.Examiners.Any(x => x.UserId == userId));
    }

    /// <summary>Candidates' answers and Writing marks: examiners assigned to the session only.</summary>
    public static IQueryable<Exam> MarkableBy(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        var userId = user.GetStaffId();
        return exams.Where(e => e.Examiners.Any(x => x.UserId == userId));
    }
}
