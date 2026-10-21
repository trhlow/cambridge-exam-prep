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

    /// <summary>Computers per row in the demo room; with a second source paper the section gives its mix by seat.</summary>
    public int? SeatsPerRow { get; init; }

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

    /// <summary>VE-68: a second demo exam, B1 Preliminary for Schools (section <c>Demo:B1</c>).</summary>
    public DemoB1Options B1 { get; init; } = new();

    /// <summary>Every folder setting resolved against <paramref name="root"/> (relative paths in appsettings).</summary>
    public DemoOptions WithFoldersUnder(string root) => this with
    {
        SamplesRoot = Path.GetFullPath(Path.Combine(root, SamplesRoot)),
        ReadingWritingFolder = Resolve(root, ReadingWritingFolder),
        ListeningFolder = Resolve(root, ListeningFolder),
        SecondReadingWritingFolder = Resolve(root, SecondReadingWritingFolder),
        SecondListeningFolder = Resolve(root, SecondListeningFolder),
        B1 = B1 with
        {
            Reading = B1.Reading.Under(root),
            Writing = B1.Writing.Under(root),
            Listening = B1.Listening.Under(root),
        },
    };

    internal static string? Resolve(string root, string? folder) =>
        string.IsNullOrWhiteSpace(folder) ? null : Path.GetFullPath(Path.Combine(root, folder));
}

/// <summary>Where a demo section's paper comes from: a package folder and, for a mix by seat, a second source.</summary>
public sealed record DemoSectionSource
{
    /// <summary>Used when it holds a paper.json (e.g. a paper converted from PDF); otherwise the self-written sample.</summary>
    public string? Folder { get; init; }

    /// <summary>Another paper of the same layout: the section then delivers the mix of both by seat.</summary>
    public string? SecondFolder { get; init; }

    public DemoSectionSource Under(string root) => new()
    {
        Folder = DemoOptions.Resolve(root, Folder),
        SecondFolder = DemoOptions.Resolve(root, SecondFolder),
    };
}

/// <summary>
/// The B1 demo exam: Reading, Writing and Listening, with its own candidate (a candidate belongs to one session) and
/// the demo PIN, password, invigilator and examiner of the A2 exam.
/// </summary>
public sealed record DemoB1Options
{
    public bool Enabled { get; init; }
    public string ExamName { get; init; } = string.Empty;
    public string CandidateUsername { get; init; } = string.Empty;
    public DemoSectionSource Reading { get; init; } = new();
    public DemoSectionSource Writing { get; init; } = new();
    public DemoSectionSource Listening { get; init; } = new();
}

/// <summary>
/// Development only: imports the sample papers and opens an A2 Key for Schools exam (and, when enabled, a B1
/// Preliminary for Schools exam) with one candidate each, so every screen can be tried at once. Runs once per exam name.
/// </summary>
public static class DemoSeeder
{
    /// <summary>A section of a demo exam: its self-written sample, and where a converted paper and a second source may be.</summary>
    private sealed record SectionPlan(string Sample, string Title, DemoSectionSource Source);

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

        await SeedExamAsync(provider, db, options, options.ExamName, options.CandidateUsername,
        [
            new("a2-key-fs-reading-writing", "Reading & Writing", new() { Folder = options.ReadingWritingFolder, SecondFolder = options.SecondReadingWritingFolder }),
            new("a2-key-fs-listening", "Listening", new() { Folder = options.ListeningFolder, SecondFolder = options.SecondListeningFolder }),
        ], logger, cancellationToken);

        if (options.B1.Enabled)
        {
            await SeedExamAsync(provider, db, options, options.B1.ExamName, options.B1.CandidateUsername,
            [
                new("b1-pfs-reading", "Reading", options.B1.Reading),
                new("b1-pfs-writing", "Writing", options.B1.Writing),
                new("b1-pfs-listening", "Listening", options.B1.Listening),
            ], logger, cancellationToken);
        }
    }

    /// <summary>One demo exam: its sections in order (each one paper, or the mix of two by seat) and one candidate.</summary>
    private static async Task SeedExamAsync(
        IServiceProvider provider, ExamPrepDbContext db, DemoOptions options, string examName, string candidateUsername,
        IReadOnlyList<SectionPlan> plans, ILogger logger, CancellationToken cancellationToken)
    {
        if (options.ResetOnStart)
        {
            await DeleteDemoExamAsync(db, examName, cancellationToken);
        }

        if (await db.Exams.AnyAsync(e => e.Name == examName, cancellationToken))
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
        var papers = new List<(TestPaper First, TestPaper? Second)>();
        foreach (var plan in plans)
        {
            var folder = ChooseFolder(plan.Source.Folder, Path.Combine(options.SamplesRoot, plan.Sample), plan.Title, logger);
            var first = await ImportAsync(importer, folder, adminId.Value, cancellationToken);
            var second = await ImportIfPresentAsync(importer, plan.Source.SecondFolder, plan.Title, adminId.Value, logger, cancellationToken);
            papers.Add((first, second));
        }

        var templateIds = papers.Select(p => p.First.SectionTemplateId).ToList();
        var templates = await db.SectionTemplates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        // With a second source paper, the section gives four mixed papers by seat (Victory 20/10).
        await db.SaveChangesAsync(cancellationToken);
        var mixer = provider.GetRequiredService<PaperMixer>();
        var sections = new List<ExamSection>();
        foreach (var (first, second) in papers)
        {
            sections.Add(await SectionAsync(mixer, templates[first.SectionTemplateId], first, second, adminId.Value, cancellationToken));
        }

        var candidate = CreateCandidate(provider, options, candidateUsername);
        var invigilatorId = await EnsureStaffAsync(provider, db, options.InvigilatorUsername, options.InvigilatorPassword, "Demo Invigilator", UserRole.Invigilator, cancellationToken);
        var examinerId = await EnsureStaffAsync(provider, db, options.ExaminerUsername, options.ExaminerPassword, "Demo Examiner", UserRole.Examiner, cancellationToken);
        var exam = new Exam
        {
            Name = examName,
            ExamTypeId = templates[papers[0].First.SectionTemplateId].ExamTypeId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Room = "Demo room",
            Status = ExamStatus.Active,
            SessionPin = options.SessionPin,
            InvigilatorPassword = Guid.NewGuid().ToString("N")[..10],
            SeatsPerRow = options.SeatsPerRow,
            CreatedByUserId = adminId.Value,
            CreatedAt = DateTime.UtcNow,
            Sections = sections,
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
        var mixIds = exam.Sections.Where(s => s.PaperMixId != null).Select(s => s.PaperMixId!.Value).ToList();
        db.Exams.Remove(exam);
        await db.SaveChangesAsync(cancellationToken);

        // Mixes no other session uses go with their four papers; their sources are then removed below if unused.
        foreach (var mixId in mixIds)
        {
            if (await db.ExamSections.AnyAsync(s => s.PaperMixId == mixId, cancellationToken))
            {
                continue;
            }

            var mix = await db.PaperMixes.Include(m => m.Versions).SingleAsync(m => m.Id == mixId, cancellationToken);
            paperIds.AddRange([mix.SourceAPaperId, mix.SourceBPaperId]);
            db.TestPapers.RemoveRange(mix.Versions);
            await db.SaveChangesAsync(cancellationToken);
            db.PaperMixes.Remove(mix);
            await db.SaveChangesAsync(cancellationToken);
        }

        var stillMixed = await db.PaperMixes.Select(m => m.SourceAPaperId).Concat(db.PaperMixes.Select(m => m.SourceBPaperId)).ToListAsync(cancellationToken);
        paperIds = paperIds.Except(stillMixed).Distinct().ToList();
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

    /// <summary>The section with one paper, or with the mix of both papers when there is a second one.</summary>
    private static async Task<ExamSection> SectionAsync(
        PaperMixer mixer, SectionTemplate template, TestPaper paper, TestPaper? second, int adminId, CancellationToken cancellationToken)
    {
        if (second is null)
        {
            return Section(template, paper);
        }

        var result = await mixer.CreateAsync(paper.Id, second.Id, $"Demo mix – {template.Title}", seed: 2026, adminId, cancellationToken);
        var mix = result.Mix ?? throw new InvalidOperationException($"Demo papers cannot be mixed:{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");
        var section = Section(template, mix.Versions.Single(v => v.MixVersion == 1));
        section.PaperMixId = mix.Id;
        return section;
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

    private static Candidate CreateCandidate(IServiceProvider provider, DemoOptions options, string username)
    {
        var candidate = new Candidate
        {
            CandidateNumber = "0001",
            FirstName = options.CandidateFirstName,
            LastName = options.CandidateLastName,
            DateOfBirth = options.CandidateDateOfBirth,
            Username = username,
            PasswordHash = string.Empty,
            PasswordProtected = string.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        provider.GetRequiredService<CandidateCredentials>().SetPassword(candidate, options.CandidatePassword);
        return candidate;
    }
}
