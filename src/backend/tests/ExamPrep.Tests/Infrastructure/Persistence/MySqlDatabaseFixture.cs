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

    /// <summary>
    /// EF Core's MySQL migration lock (GET_LOCK('__EFMigrationsLock')) is server-wide, not per database, and a
    /// pooled connection can keep holding it; fixtures running in parallel then wait for ever. Migrations therefore
    /// run one at a time, on a connection that is really closed (no pooling), which always releases the lock.
    /// </summary>
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);

    private readonly string? serverConnection = Environment.GetEnvironmentVariable(ServerConnectionVariable);
    private readonly string databaseName = $"examprep_test_{Guid.NewGuid():N}";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(serverConnection);

    public string ConnectionString => $"{serverConnection};database={databaseName}";

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

        await MigrationGate.WaitAsync();
        try
        {
            await using var db = new ExamPrepDbContext(new DbContextOptionsBuilder<ExamPrepDbContext>().UseMySQL($"{ConnectionString};pooling=false").Options);
            await db.Database.MigrateAsync();
        }
        finally
        {
            MigrationGate.Release();
        }
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
