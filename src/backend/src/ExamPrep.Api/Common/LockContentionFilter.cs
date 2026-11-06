using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MySql.Data.MySqlClient;

namespace ExamPrep.Api.Common;

/// <summary>
/// A request that waited for a row lock longer than MySQL allows (1205, 50 s) or lost a deadlock (1213) did nothing: tell
/// the person to try again instead of answering 500. Exam status changes and candidate Starts queue on the exam row lock
/// (<c>ExamRowLock</c>), so this is where a stuck one surfaces. The problem carries <c>code: busy</c> so the candidate's
/// autosave keeps its answers and tries again instead of treating the 409 as the end of the section.
/// </summary>
public sealed class LockContentionFilter : IExceptionFilter
{
    private const int LockWaitTimeout = 1205;
    private const int Deadlock = 1213;

    public void OnException(ExceptionContext context)
    {
        for (var e = context.Exception; e is not null; e = e.InnerException)
        {
            if (e is MySqlException { Number: LockWaitTimeout or Deadlock })
            {
                var problem = new ProblemDetails { Title = "The session is being changed; try again.", Status = StatusCodes.Status409Conflict };
                problem.Extensions["code"] = "busy";
                context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status409Conflict };
                context.ExceptionHandled = true;
                return;
            }
        }
    }
}
