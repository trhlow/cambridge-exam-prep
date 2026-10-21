using System.Net.Http.Json;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using ExamPrep.Tests.Infrastructure.Content;
using ExamPrep.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>Throw-away folder for the Data Protection key ring, deleted with the fixture.</summary>
    public string KeysPath { get; } = Path.Combine(Path.GetTempPath(), $"examprep_keys_{Guid.NewGuid():N}");

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
            builder.UseSetting("DataProtection:KeysPath", KeysPath);
        });

    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>A client signed in as the given staff member.</summary>
    public async Task<HttpClient> StaffClientAsync(string username, string password)
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/staff/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public Task<HttpClient> AdminClientAsync() => StaffClientAsync(AdminUsername, AdminPassword);

    /// <summary>Two published A2 papers without content (Reading &amp; Writing, Listening), enough to schedule an exam.</summary>
    public async Task<(int ReadingWritingId, int ListeningId)> CreatePublishedPapersAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var adminId = db.Users.Where(u => u.Username == AdminUsername).Select(u => u.Id).Single();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;
        var readingWriting = new TestPaper { SectionTemplateId = 1, Title = $"RW {suffix}", Status = PaperStatus.Published, PublishedAt = now, CreatedByUserId = adminId, CreatedAt = now };
        var listening = new TestPaper { SectionTemplateId = 2, Title = $"L {suffix}", Status = PaperStatus.Published, PublishedAt = now, CreatedByUserId = adminId, CreatedAt = now };
        db.TestPapers.AddRange(readingWriting, listening);
        await db.SaveChangesAsync();
        return (readingWriting.Id, listening.Id);
    }

    /// <summary>
    /// Imports a sample package as a published paper with its own title; every part title is prefixed with the paper title,
    /// so a mixed paper shows which source each part came from.
    /// </summary>
    public async Task<int> ImportPublishedSampleAsync(string sample, string title)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var adminId = db.Users.Where(u => u.Username == AdminUsername).Select(u => u.Id).Single();
        var importer = scope.ServiceProvider.GetRequiredService<PaperPackageImporter>();
        var imported = (await importer.ImportAsync(new MemoryStream(SamplePackages.Zip(sample)), adminId, default)).Paper!;
        var paper = await db.TestPapers.Include(p => p.Parts).SingleAsync(p => p.Id == imported.Id);
        paper.Title = title;
        paper.Status = PaperStatus.Published;
        paper.PublishedAt = DateTime.UtcNow;
        foreach (var part in paper.Parts)
        {
            part.Title = $"{title} part {part.PartNo}";
        }

        await db.SaveChangesAsync();
        return paper.Id;
    }

    /// <summary>
    /// Gives the candidate a running attempt at the section, as if they had just started it; an offline candidate's
    /// last heartbeat was long ago.
    /// </summary>
    public async Task<int> StartAttemptAsync(int candidateId, int sectionId, bool isOnline = true, int durationMinutes = 60, int? audioPlayedMinutes = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var now = DateTime.UtcNow;
        var attempt = new Attempt
        {
            CandidateId = candidateId,
            ExamSectionId = sectionId,
            Status = AttemptStatus.InProgress,
            StartedAt = now,
            EndsAt = now.AddMinutes(durationMinutes),
            LastHeartbeatAt = isOnline ? now : now.AddMinutes(-5),
            ConnectionStatus = isOnline ? ConnectionStatus.Online : ConnectionStatus.Offline,
            AudioStartedAt = audioPlayedMinutes is { } played ? now.AddMinutes(-played) : null,
        };
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync();
        return attempt.Id;
    }

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

    /// <summary>A manager account working in the given areas.</summary>
    public async Task<User> CreateManagerAsync(string password, params ManagerArea[] areas)
    {
        var user = await CreateStaffAsync(UserRole.Manager, password);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        db.UserAreas.AddRange(areas.Select(area => new UserArea { UserId = user.Id, Area = area }));
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>A client signed in as a new administrator: the second pair of eyes for approvals.</summary>
    public async Task<HttpClient> SecondAdminClientAsync()
    {
        const string password = "Second#Admin2026";
        var admin = await CreateStaffAsync(UserRole.Admin, password);
        return await StaffClientAsync(admin.Username, password);
    }

    /// <summary>Reads the approval request a 202 response carries (a deletion or an account change waiting for approval).</summary>
    public static async Task<int> RequestIdOfAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        var request = body!["request"] ?? body;
        return (int)request["id"]!;
    }

    /// <summary>Approves a request as the given administrator and fails the test unless it went through.</summary>
    public static async Task ApproveAsync(HttpClient approver, int requestId)
    {
        var response = await approver.PostAsync($"/api/staff/approvals/{requestId}/approve", null);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        if ((string)body!["status"]! != "Approved")
        {
            throw new InvalidOperationException($"Request {requestId} ended as {(string)body["status"]!}: {(string?)body["outcome"]}");
        }
    }

    /// <summary>Assigns an examiner to a session, as an administrator would.</summary>
    public async Task AssignExaminerAsync(int examId, int userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        db.ExamExaminers.Add(new ExamExaminer { ExamId = examId, UserId = userId, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public sealed record SeededCandidate(int CandidateId, int ExamId, string Username, string SessionPin, int ReadingWritingSectionId, int ListeningSectionId);

    /// <summary>
    /// Creates an A2 Key for Schools exam session (Reading &amp; Writing, then Listening) in the given status,
    /// with one candidate whose password is hashed the way the API expects.
    /// </summary>
    public async Task<SeededCandidate> CreateCandidateAsync(string password, ExamStatus status = ExamStatus.Active, bool withSamplePapers = false)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>();
        var adminId = db.Users.Where(u => u.Username == AdminUsername).Select(u => u.Id).Single();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        TestPaper listening, readingWriting;
        if (withSamplePapers)
        {
            // The real sample packages from setup/exam-package, imported the way an administrator would.
            var importer = scope.ServiceProvider.GetRequiredService<PaperPackageImporter>();
            readingWriting = (await importer.ImportAsync(new MemoryStream(SamplePackages.Zip(SamplePackages.ReadingWriting)), adminId, default)).Paper!;
            listening = (await importer.ImportAsync(new MemoryStream(SamplePackages.Zip(SamplePackages.Listening)), adminId, default)).Paper!;
        }
        else
        {
            // Listening is added first on purpose: the API must order sections by OrderNo, not by insertion.
            listening = new TestPaper { SectionTemplateId = 2, Title = $"A2 L {suffix}", CreatedByUserId = adminId, CreatedAt = now };
            readingWriting = new TestPaper { SectionTemplateId = 1, Title = $"A2 RW {suffix}", CreatedByUserId = adminId, CreatedAt = now };
            db.TestPapers.AddRange(listening, readingWriting);
            await db.SaveChangesAsync();
        }

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
        var sectionIds = exam.Sections.ToDictionary(s => s.SectionTemplateId, s => s.Id);
        return new SeededCandidate(candidate.Id, exam.Id, candidate.Username, exam.SessionPin, sectionIds[1], sectionIds[2]);
    }

    /// <summary>Enters another candidate for an existing session; returns its id and username.</summary>
    public async Task<(int CandidateId, string Username)> AddCandidateAsync(int examId, string password, string firstName = "Binh", string lastName = "Tran")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Candidate>>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var candidate = new Candidate
        {
            ExamId = examId,
            CandidateNumber = suffix[..6],
            FirstName = firstName,
            LastName = lastName,
            Username = $"tranb{suffix}",
            PasswordHash = string.Empty,
            PasswordProtected = "not-used-in-tests",
            CreatedAt = DateTime.UtcNow,
        };
        candidate.PasswordHash = hasher.HashPassword(candidate, password);
        db.Candidates.Add(candidate);
        await db.SaveChangesAsync();
        return (candidate.Id, candidate.Username);
    }

    /// <summary>Marks a section as finished for the candidate (submitted, or its time ran out).</summary>
    public async Task SubmitSectionAsync(int candidateId, int sectionId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var now = DateTime.UtcNow;
        db.Attempts.Add(new Attempt
        {
            CandidateId = candidateId,
            ExamSectionId = sectionId,
            Status = AttemptStatus.Submitted,
            StartedAt = now.AddMinutes(-60),
            EndsAt = now,
            SubmittedAt = now,
            SubmitReason = SubmitReason.TimeExpired,
        });
        await db.SaveChangesAsync();
    }

    public async Task SetPlannedExtraPercentAsync(int candidateId, int percent)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var candidate = await db.Candidates.FindAsync(candidateId) ?? throw new InvalidOperationException($"Candidate {candidateId} not found.");
        candidate.PlannedExtraPercent = percent;
        await db.SaveChangesAsync();
    }

    /// <summary>Pretends the candidate started the section <paramref name="earlier"/> ago (moves start and end back).</summary>
    public async Task MoveAttemptStartBackAsync(int candidateId, int sectionId, TimeSpan earlier)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var attempt = db.Attempts.Single(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId);
        attempt.StartedAt -= earlier;
        attempt.EndsAt -= earlier;
        await db.SaveChangesAsync();
    }

    /// <summary>Pretends the candidate pressed Play <paramref name="earlier"/> ago.</summary>
    public async Task MoveAudioStartBackAsync(int candidateId, int sectionId, TimeSpan earlier)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        var attempt = db.Attempts.Single(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId);
        attempt.AudioStartedAt -= earlier;
        await db.SaveChangesAsync();
    }

    public async Task<Attempt> GetAttemptAsync(int candidateId, int sectionId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamPrepDbContext>();
        return db.Attempts.Single(a => a.CandidateId == candidateId && a.ExamSectionId == sectionId);
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

        foreach (var folder in new[] { AssetsRoot, KeysPath }.Where(Directory.Exists))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
