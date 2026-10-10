using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Tests.Api;

/// <summary>
/// Hosts the real API in memory on a throw-away MySQL database, with a bootstrap admin configured.
/// Skipped (via <see cref="SkipIfUnavailable"/>) when no test MySQL server is configured.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Victory#Admin2026";

    private readonly MySqlDatabaseFixture database = new();
    private WebApplicationFactory<Program>? factory;

    public WebApplicationFactory<Program> Factory =>
        factory ?? throw new InvalidOperationException("The API fixture is not initialised.");

    public void SkipIfUnavailable() => database.SkipIfUnavailable();

    public WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:ExamPrep", database.ConnectionString);
            builder.UseSetting("Bootstrap:AdminUsername", AdminUsername);
            builder.UseSetting("Bootstrap:AdminPassword", AdminPassword);
        });

    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Creates a staff account directly in the database with a properly hashed password.</summary>
    public async Task<User> CreateStaffAsync(UserRole role, string password, bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = new User
        {
            Username = $"{role.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}"[..24],
            PasswordHash = string.Empty,
            FullName = $"Test {role}",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task RotateSecurityStampAsync(int userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var user = await db.Users.FindAsync(userId) ?? throw new InvalidOperationException($"User {userId} not found.");
        user.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await database.InitializeAsync();
        if (database.IsAvailable)
        {
            factory = CreateFactory();
            _ = factory.Server;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await database.DisposeAsync();
    }
}
