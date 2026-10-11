using ExamPrep.Api.Admin;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Api.Demo;

/// <summary>Settings of the development demo (section <c>Demo</c> in appsettings.Development.json).</summary>
public sealed record DemoOptions
{
    public bool Enabled { get; init; }

    /// <summary>Folder holding the sample packages; relative paths are resolved against the API's content root.</summary>
    public string SamplesRoot { get; init; } = string.Empty;

    /// <summary>
    /// Optional package folder for Reading &amp; Writing, e.g. a paper converted from PDF under the git-ignored
    /// setup/exam-package/sources. Used when it holds a paper.json; otherwise the synthetic sample is used.
    /// </summary>
    public string? ReadingWritingFolder { get; init; }

    public string ExamName { get; init; } = string.Empty;
    public string CandidateUsername { get; init; } = string.Empty;
    public string CandidatePassword { get; init; } = string.Empty;
    public string SessionPin { get; init; } = string.Empty;

    /// <summary>Optional invigilator account created (if missing) and assigned to the demo exam.</summary>
    public string? InvigilatorUsername { get; init; }
    public string? InvigilatorPassword { get; init; }

    /// <summary>Delete the demo exam (and its papers, attempts and answers) and create it again on every start.</summary>
    public bool ResetOnStart { get; init; }
}

/// <summary>
/// Development only: imports the synthetic A2 sample papers and opens an A2 Key for Schools exam with one
/// candidate, so the candidate screen can be tried before the admin portal exists. Runs once per exam name.
/// </summary>
public static class DemoSeeder
{
    private const string ReadingWritingSample = "a2-key-fs-reading-writing";
    private const string ListeningSample = "a2-key-fs-listening";
    private const string A2ExamTypeCode = "A2_KEY_FS";

    public static async Task SeedAsync(IServiceProvider services, DemoOptions options, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ExamPrepDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoSeeder));

        if (options.ResetOnStart)
        {
            await DeleteDemoExamAsync(db, options.ExamName, cancellationToken);
        }

        if (await db.Exams.AnyAsync(e => e.Name == options.ExamName, cancellationToken))
        {
            return;
        }

        var adminId = await db.Users.Where(u => u.Role == UserRole.Admin).OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync(cancellationToken);
        if (adminId is null)
        {
            logger.LogWarning("Demo exam not created: there is no administrator yet (set Bootstrap:AdminUsername / Bootstrap:AdminPassword).");
            return;
        }

        var importer = provider.GetRequiredService<PaperPackageImporter>();
        var readingWritingFolder = ReadingWritingFolder(options, logger);
        var readingWriting = await ImportAsync(importer, readingWritingFolder, adminId.Value, cancellationToken);
        var listening = await ImportAsync(importer, Path.Combine(options.SamplesRoot, ListeningSample), adminId.Value, cancellationToken);

        var templates = await db.SectionTemplates.AsNoTracking()
            .Where(t => t.ExamType!.Code == A2ExamTypeCode)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var candidate = CreateCandidate(provider, options);
        var invigilatorId = await EnsureInvigilatorAsync(provider, db, options, cancellationToken);
        var exam = new Exam
        {
            Name = options.ExamName,
            ExamTypeId = templates[readingWriting.SectionTemplateId].ExamTypeId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Room = "Demo room",
            Status = ExamStatus.Active,
            SessionPin = options.SessionPin,
            InvigilatorPassword = Guid.NewGuid().ToString("N")[..10],
            CreatedByUserId = adminId.Value,
            CreatedAt = DateTime.UtcNow,
            Sections = [Section(templates[readingWriting.SectionTemplateId], readingWriting), Section(templates[listening.SectionTemplateId], listening)],
            Candidates = [candidate],
            Invigilators = invigilatorId is { } id ? [new ExamInvigilator { UserId = id, AssignedAt = DateTime.UtcNow }] : [],
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo exam {ExamName} is open; candidate {Username} can sign in (see the Demo section of appsettings.Development.json).", exam.Name, candidate.Username);
    }

    /// <summary>Removes the demo exam with its candidates, attempts and answers, then the papers only it used.</summary>
    private static async Task DeleteDemoExamAsync(ExamPrepDbContext db, string examName, CancellationToken cancellationToken)
    {
        var exam = await db.Exams.Include(e => e.Sections).SingleOrDefaultAsync(e => e.Name == examName, cancellationToken);
        if (exam is null)
        {
            return;
        }

        var paperIds = exam.Sections.Select(s => s.TestPaperId).ToList();
        db.Exams.Remove(exam);
        await db.SaveChangesAsync(cancellationToken);

        var unusedPapers = await db.TestPapers
            .Where(p => paperIds.Contains(p.Id) && !db.ExamSections.Any(s => s.TestPaperId == p.Id))
            .ToListAsync(cancellationToken);
        db.TestPapers.RemoveRange(unusedPapers);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string ReadingWritingFolder(DemoOptions options, ILogger logger)
    {
        var folder = options.ReadingWritingFolder;
        if (!string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "paper.json")))
        {
            logger.LogInformation("Demo Reading & Writing paper comes from {Folder}.", folder);
            return folder;
        }

        return Path.Combine(options.SamplesRoot, ReadingWritingSample);
    }

    private static async Task<TestPaper> ImportAsync(PaperPackageImporter importer, string folder, int adminId, CancellationToken cancellationToken)
    {
        var result = await importer.ImportAsync(new MemoryStream(PackageFolder.Zip(folder, fillMissing: true)), adminId, cancellationToken);
        var paper = result.Paper ?? throw new InvalidOperationException($"Demo sample {folder} is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");

        // Published, so the admin portal can schedule further demo sessions with it.
        paper.Status = PaperStatus.Published;
        paper.PublishedAt = DateTime.UtcNow;
        return paper;
    }

    private static ExamSection Section(SectionTemplate template, TestPaper paper) => new()
    {
        SectionTemplateId = template.Id,
        TestPaperId = paper.Id,
        OrderNo = template.OrderNo,
        DurationMinutes = template.DurationMinutes,
    };

    /// <summary>The demo invigilator's id, creating the account the first time; null when none is configured.</summary>
    private static async Task<int?> EnsureInvigilatorAsync(IServiceProvider provider, ExamPrepDbContext db, DemoOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.InvigilatorUsername) || string.IsNullOrWhiteSpace(options.InvigilatorPassword))
        {
            return null;
        }

        var existing = await db.Users.Where(u => u.Username == options.InvigilatorUsername).Select(u => (int?)u.Id).SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User
        {
            Username = options.InvigilatorUsername,
            FullName = "Demo Invigilator",
            Role = UserRole.Invigilator,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = provider.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, options.InvigilatorPassword);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    private static Candidate CreateCandidate(IServiceProvider provider, DemoOptions options)
    {
        var candidate = new Candidate
        {
            CandidateNumber = "0001",
            FirstName = "An",
            LastName = "Nguyen",
            Username = options.CandidateUsername,
            PasswordHash = string.Empty,
            PasswordProtected = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        provider.GetRequiredService<CandidateCredentials>().SetPassword(candidate, options.CandidatePassword);
        return candidate;
    }
}
