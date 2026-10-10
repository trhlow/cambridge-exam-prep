using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExamPrep.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c>. Reads the connection string from the <c>ConnectionStrings__ExamPrep</c>
/// environment variable; adding a migration does not open a connection, so a password-less placeholder is enough there.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ExamPrepDbContext>
{
    private const string PlaceholderConnection = "server=localhost;database=examprep";

    public ExamPrepDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__ExamPrep") ?? PlaceholderConnection;
        var options = new DbContextOptionsBuilder<ExamPrepDbContext>().UseMySQL(connection).Options;
        return new ExamPrepDbContext(options);
    }
}
