using System.Security.Claims;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Api.Exams;

/// <summary>
/// Which exam sessions a staff member may open: administrators see every session, invigilators only the ones they
/// are assigned to. Other sessions answer 404, so an invigilator cannot even learn that they exist.
/// </summary>
public static class ExamAccess
{
    public static IQueryable<Exam> VisibleTo(this IQueryable<Exam> exams, ClaimsPrincipal user)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
        {
            return exams;
        }

        var userId = user.GetStaffId();
        return exams.Where(e => e.Invigilators.Any(i => i.UserId == userId));
    }
}
