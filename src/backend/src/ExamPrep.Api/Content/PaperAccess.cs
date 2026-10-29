using System.Security.Claims;
using ExamPrep.Api.Auth;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Content;

/// <summary>
/// VE-72 (Victory 29/10): a papers manager works only on the papers and mixes they made — imported, converted, started
/// blank or mixed; administrators reach every paper. Managers of exams pick published papers by title
/// (<c>/api/admin/exam-papers</c>) but never open them.
/// </summary>
public static class PaperAccess
{
    public static IQueryable<TestPaper> VisibleTo(this IQueryable<TestPaper> papers, ClaimsPrincipal user)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
        {
            return papers;
        }

        var userId = user.GetStaffId();
        return papers.Where(p => p.CreatedByUserId == userId);
    }

    public static IQueryable<PaperMix> VisibleTo(this IQueryable<PaperMix> mixes, ClaimsPrincipal user)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
        {
            return mixes;
        }

        var userId = user.GetStaffId();
        return mixes.Where(m => m.CreatedByUserId == userId);
    }

    /// <summary>Whether the person may use this paper (as a mix source, for example).</summary>
    public static bool Owns(this ClaimsPrincipal user, TestPaper paper) =>
        user.IsInRole(nameof(UserRole.Admin)) || paper.CreatedByUserId == user.GetStaffId();
}

/// <summary>Answers 404 unless the paper named by the route's <c>paperId</c> is the signed-in person's (<see cref="PaperAccess"/>).</summary>
public sealed class OwnPaperFilter(ExamPrepDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.RouteData.Values.TryGetValue("paperId", out var value)
            && int.TryParse(value?.ToString(), out var paperId)
            && !await db.TestPapers.VisibleTo(context.HttpContext.User).AnyAsync(p => p.Id == paperId, context.HttpContext.RequestAborted))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
