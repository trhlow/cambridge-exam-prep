using System.Reflection;
using ExamPrep.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ExamPrep.Tests.Api.Common;

/// <summary>Review of Task 5: a lock wait past MySQL's timeout, or a deadlock, is a 409 with a sentence, not a 500.</summary>
public sealed class LockContentionFilterTests
{
    /// <summary>MySqlException has no public constructor that sets the error number; this is the only way to build one.</summary>
    private static MySqlException MySqlError(int number)
    {
        var exception = (MySqlException)Activator.CreateInstance(typeof(MySqlException), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ["boom", number], null)!;
        Assert.Equal(number, exception.Number);
        return exception;
    }

    private static ExceptionContext ContextFor(Exception exception) =>
        new(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), []) { Exception = exception };

    [Theory]
    [InlineData(1205)] // lock wait timeout
    [InlineData(1213)] // deadlock
    public void ALockTimeoutOrDeadlock_IsAConflictWithASentence(int number)
    {
        var context = ContextFor(new DbUpdateException("save", MySqlError(number)));

        new LockContentionFilter().OnException(context);

        var result = Assert.IsType<ConflictObjectResult>(context.Result);
        Assert.Equal("The session is being changed; try again.", Assert.IsType<ProblemDetails>(result.Value).Title);
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void AnyOtherError_IsLeftAlone()
    {
        var context = ContextFor(new DbUpdateException("save", MySqlError(1062)));

        new LockContentionFilter().OnException(context);

        Assert.Null(context.Result);
        Assert.False(context.ExceptionHandled);
    }
}
