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

    /// <summary>Throw-away folder for uploaded images and audio, deleted with the fixture.</summary>
    public string AssetsRoot { get; } = Path.Combine(Path.GetTempPath(), $"examprep_assets_{Guid.NewGuid():N}");

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
            builder.UseSetting("Storage:AssetsRoot", AssetsRoot);
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

    public sealed record SeededCandidate(int CandidateId, int ExamId, string Username, string SessionPin);

    /// <summary>
    /// Creates an A2 Key for Schools exam session (Reading &amp; Writing, then Listening) in the given status,
    /// with one candidate whose password is hashed the way the API expects.
    /// </summary>
    public async Task<SeededCandidate> CreateCandidateAsync(string password, ExamStatus status = ExamStatus.Active)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>();
        var adminId = db.Users.Where(u => u.Username == AdminUsername).Select(u => u.Id).Single();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        // Listening is added first on purpose: the API must order sections by OrderNo, not by insertion.
        var listening = new TestPaper { SectionTemplateId = 2, Title = $"A2 L {suffix}", CreatedByUserId = adminId, CreatedAt = now };
        var readingWriting = new TestPaper { SectionTemplateId = 1, Title = $"A2 RW {suffix}", CreatedByUserId = adminId, CreatedAt = now };
        db.TestPapers.AddRange(listening, readingWriting);
        await db.SaveChangesAsync();

        var candidate = new Candidate
        {
            CandidateNumber = "0001",
            FirstName = "An",
            LastName = "Nguyen",
            Username = $"nguyea{suffix}",
            PasswordHash = string.Empty,
            PasswordProtected = "not-used-in-tests",
            CreatedAt = now,
        };
        candidate.PasswordHash = hasher.HashPassword(candidate, password);
        var exam = new Exam
        {
            Name = $"A2 Key fS {suffix}",
            ExamTypeId = 1,
            ScheduledDate = DateOnly.FromDateTime(now),
            Status = status,
            SessionPin = "4821",
            InvigilatorPassword = "invig-pass",
            CreatedByUserId = adminId,
            CreatedAt = now,
            Sections =
            [
                new ExamSection { SectionTemplateId = 2, TestPaperId = listening.Id, OrderNo = 2, DurationMinutes = 30 },
                new ExamSection { SectionTemplateId = 1, TestPaperId = readingWriting.Id, OrderNo = 1, DurationMinutes = 60 },
            ],
            Candidates = [candidate],
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        return new SeededCandidate(candidate.Id, exam.Id, candidate.Username, exam.SessionPin);
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

        if (Directory.Exists(AssetsRoot))
        {
            Directory.Delete(AssetsRoot, recursive: true);
        }
    }
}
