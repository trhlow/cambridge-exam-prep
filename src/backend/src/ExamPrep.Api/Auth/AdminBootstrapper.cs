using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Auth;

/// <summary>
/// Creates the first administrator from configuration (<c>Bootstrap:AdminUsername</c> / <c>Bootstrap:AdminPassword</c>,
/// e.g. user-secrets in development) when the database has no administrator yet. Never runs twice.
/// </summary>
public static class AdminBootstrapper
{
    private const int MinimumPasswordLength = 12;

    public static async Task EnsureAdminAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminBootstrapper));

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var username = configuration["Bootstrap:AdminUsername"];
        var password = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("No administrator exists and Bootstrap:AdminUsername / Bootstrap:AdminPassword are not set; nobody can sign in to the admin portal.");
            return;
        }

        if (password.Length < MinimumPasswordLength)
        {
            throw new InvalidOperationException($"Bootstrap:AdminPassword must be at least {MinimumPasswordLength} characters.");
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User
        {
            Username = username,
            PasswordHash = string.Empty,
            FullName = "Administrator",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created bootstrap administrator {Username}", username);
    }
}
