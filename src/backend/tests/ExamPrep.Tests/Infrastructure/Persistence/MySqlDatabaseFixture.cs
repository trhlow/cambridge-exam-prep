using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Tests.Infrastructure.Persistence;

/// <summary>
/// Creates a throw-away database on the MySQL server named by <c>EXAMPREP_TEST_MYSQL</c>
/// (a server-level connection string without a database), applies all migrations, and drops it afterwards.
/// Tests are skipped when the variable is not set.
/// </summary>
public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    public const string ServerConnectionVariable = "EXAMPREP_TEST_MYSQL";

    private readonly string? serverConnection = Environment.GetEnvironmentVariable(ServerConnectionVariable);
    private readonly string databaseName = $"examprep_test_{Guid.NewGuid():N}";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(serverConnection);

    private string ConnectionString => $"{serverConnection};database={databaseName}";

    public ExamPrepDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ExamPrepDbContext>().UseMySQL(ConnectionString).Options);

    public void SkipIfUnavailable() =>
        Assert.SkipUnless(IsAvailable, $"Set {ServerConnectionVariable} to a MySQL server connection string to run database tests.");

    public async ValueTask InitializeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
