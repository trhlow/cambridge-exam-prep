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

    /// <summary>Optional Listening package folder (paper.json + images/ + audio/), used like <see cref="ReadingWritingFolder"/>.</summary>
    public string? ListeningFolder { get; init; }

    /// <summary>
    /// Optional second source papers (Victory 20/10, paper shuffling): another paper of the same layout per section,
    /// imported and published next to the first when the folder holds a paper.json.
    /// </summary>
    public string? SecondReadingWritingFolder { get; init; }

    /// <summary>Optional second Listening source paper, used like <see cref="SecondReadingWritingFolder"/>.</summary>
    public string? SecondListeningFolder { get; init; }

    public string ExamName { get; init; } = string.Empty;
    public string CandidateUsername { get; init; } = string.Empty;
    public string CandidatePassword { get; init; } = string.Empty;
    public string CandidateFirstName { get; init; } = "An";
    public string CandidateLastName { get; init; } = "Nguyen";
    public DateOnly? CandidateDateOfBirth { get; init; }
    public string SessionPin { get; init; } = string.Empty;

    /// <summary>
    /// Optional administrator created (if missing) for the demo, so a fresh machine has a known admin login.
    /// Unlike Bootstrap:AdminPassword it has no minimum length: it is a development-only, report-friendly account.
    /// </summary>
    public string? AdminUsername { get; init; }
    public string? AdminPassword { get; init; }

    /// <summary>Optional second administrator (Victory: deletions and account changes need two administrators).</summary>
    public string? SecondAdminUsername { get; init; }
    public string? SecondAdminPassword { get; init; }

    /// <summary>Optional manager working in every area.</summary>
    public string? ManagerUsername { get; init; }
    public string? ManagerPassword { get; init; }

    /// <summary>Optional examiner, assigned to the demo exam.</summary>
    public string? ExaminerUsername { get; init; }
    public string? ExaminerPassword { get; init; }

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

        await EnsureStaffAsync(provider, db, options.AdminUsername, options.AdminPassword, "Demo Administrator", UserRole.Admin, cancellationToken);
        await EnsureStaffAsync(provider, db, options.SecondAdminUsername, options.SecondAdminPassword, "Demo Administrator 2", UserRole.Admin, cancellationToken);
        await EnsureStaffAsync(provider, db, options.ManagerUsername, options.ManagerPassword, "Demo Manager", UserRole.Manager, cancellationToken, Enum.GetValues<ManagerArea>());

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
        var readingWritingFolder = ChooseFolder(options.ReadingWritingFolder, Path.Combine(options.SamplesRoot, ReadingWritingSample), "Reading & Writing", logger);
        var listeningFolder = ChooseFolder(options.ListeningFolder, Path.Combine(options.SamplesRoot, ListeningSample), "Listening", logger);
        var readingWriting = await ImportAsync(importer, readingWritingFolder, adminId.Value, cancellationToken);
        var listening = await ImportAsync(importer, listeningFolder, adminId.Value, cancellationToken);
        var secondReadingWriting = await ImportIfPresentAsync(importer, options.SecondReadingWritingFolder, "Reading & Writing", adminId.Value, logger, cancellationToken);
        var secondListening = await ImportIfPresentAsync(importer, options.SecondListeningFolder, "Listening", adminId.Value, logger, cancellationToken);

        var templates = await db.SectionTemplates.AsNoTracking()
            .Where(t => t.ExamType!.Code == A2ExamTypeCode)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var candidate = CreateCandidate(provider, options);
        var invigilatorId = await EnsureStaffAsync(provider, db, options.InvigilatorUsername, options.InvigilatorPassword, "Demo Invigilator", UserRole.Invigilator, cancellationToken);
        var examinerId = await EnsureStaffAsync(provider, db, options.ExaminerUsername, options.ExaminerPassword, "Demo Examiner", UserRole.Examiner, cancellationToken);
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
            Examiners = examinerId is { } examiner ? [new ExamExaminer { UserId = examiner, AssignedAt = DateTime.UtcNow }] : [],
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

    /// <summary>The configured package folder when it holds a paper.json (e.g. a converted paper), else the sample.</summary>
    private static string ChooseFolder(string? folder, string sample, string section, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "paper.json")))
        {
            logger.LogInformation("Demo {Section} paper comes from {Folder}.", section, folder);
            return folder;
        }

        return sample;
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

    /// <summary>A second source paper when its folder holds a paper.json; null otherwise.</summary>
    private static async Task<TestPaper?> ImportIfPresentAsync(
        PaperPackageImporter importer, string? folder, string section, int adminId, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folder) || !File.Exists(Path.Combine(folder, "paper.json")))
        {
            return null;
        }

        logger.LogInformation("Second demo {Section} source paper comes from {Folder}.", section, folder);
        return await ImportAsync(importer, folder, adminId, cancellationToken);
    }

    private static ExamSection Section(SectionTemplate template, TestPaper paper) => new()
    {
        SectionTemplateId = template.Id,
        TestPaperId = paper.Id,
        OrderNo = template.OrderNo,
        DurationMinutes = template.DurationMinutes,
    };

    /// <summary>A demo staff account's id, creating the account the first time; null when none is configured.</summary>
    private static async Task<int?> EnsureStaffAsync(
        IServiceProvider provider, ExamPrepDbContext db, string? username, string? password, string fullName, UserRole role, CancellationToken cancellationToken, IReadOnlyList<ManagerArea>? areas = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var existing = await db.Users.Where(u => u.Username == username).Select(u => (int?)u.Id).SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User
        {
            Username = username,
            FullName = fullName,
            Role = role,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Areas = (areas ?? []).Select(area => new UserArea { Area = area }).ToList(),
        };
        user.PasswordHash = provider.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    private static Candidate CreateCandidate(IServiceProvider provider, DemoOptions options)
    {
        var candidate = new Candidate
        {
            CandidateNumber = "0001",
            FirstName = options.CandidateFirstName,
            LastName = options.CandidateLastName,
            DateOfBirth = options.CandidateDateOfBirth,
            Username = options.CandidateUsername,
            PasswordHash = string.Empty,
            PasswordProtected = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        provider.GetRequiredService<CandidateCredentials>().SetPassword(candidate, options.CandidatePassword);
        return candidate;
    }
}
