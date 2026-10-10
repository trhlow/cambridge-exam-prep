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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ModelConventions.Apply(configurationBuilder);
    }
}
