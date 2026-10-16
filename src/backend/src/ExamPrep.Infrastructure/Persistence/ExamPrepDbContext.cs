using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Infrastructure.Persistence;

public class ExamPrepDbContext(DbContextOptions<ExamPrepDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<ExamType> ExamTypes => Set<ExamType>();

    public DbSet<SectionTemplate> SectionTemplates => Set<SectionTemplate>();

    public DbSet<TestPaper> TestPapers => Set<TestPaper>();

    public DbSet<PaperPart> PaperParts => Set<PaperPart>();

    public DbSet<QuestionGroup> QuestionGroups => Set<QuestionGroup>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerKey> AnswerKeys => Set<AnswerKey>();

    public DbSet<Exam> Exams => Set<Exam>();

    public DbSet<ExamSection> ExamSections => Set<ExamSection>();

    public DbSet<ExamInvigilator> ExamInvigilators => Set<ExamInvigilator>();

    public DbSet<ExamExaminer> ExamExaminers => Set<ExamExaminer>();

    public DbSet<UserArea> UserAreas => Set<UserArea>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<Candidate> Candidates => Set<Candidate>();

    public DbSet<Attempt> Attempts => Set<Attempt>();

    public DbSet<Response> Responses => Set<Response>();

    public DbSet<Annotation> Annotations => Set<Annotation>();

    public DbSet<ManualMark> ManualMarks => Set<ManualMark>();

    public DbSet<InvigilatorAction> InvigilatorActions => Set<InvigilatorAction>();

    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    public DbSet<CandidateMessage> CandidateMessages => Set<CandidateMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExamPrepDbContext).Assembly);
        SnakeCaseNaming.Apply(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RotateAttemptConcurrencyStamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RotateAttemptConcurrencyStamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Every attempt update gets a new stamp, so a write based on a stale read (e.g. a candidate heartbeat
    /// racing an invigilator pause) fails with DbUpdateConcurrencyException instead of overwriting.
    /// </summary>
    private void RotateAttemptConcurrencyStamps()
    {
        foreach (var entry in ChangeTracker.Entries<Attempt>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid();
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ModelConventions.Apply(configurationBuilder);
    }
}
