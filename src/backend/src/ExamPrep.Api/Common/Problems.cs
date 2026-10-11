using Microsoft.AspNetCore.Mvc;

namespace ExamPrep.Api.Common;

/// <summary>Error responses the staff portal shows as they are: a title and, for invalid input, every problem found.</summary>
public static class Problems
{
    /// <summary>400 with a title and one admin-readable sentence per problem in <c>errors</c>.</summary>
    public static BadRequestObjectResult Invalid(string title, IReadOnlyList<string> errors)
    {
        var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = title };
        problem.Extensions["errors"] = errors;
        return new BadRequestObjectResult(problem);
    }

    public static BadRequestObjectResult Invalid(string error) => Invalid(error, [error]);

    /// <summary>409: the request is valid but the current state does not allow it (e.g. candidates have started).</summary>
    public static ConflictObjectResult Conflict(string title) =>
        new(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = title });
}
