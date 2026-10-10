using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Infrastructure.Persistence;

namespace ExamPrep.Tests.Infrastructure.Persistence;

/// <summary>Inserts a minimal A2 Key for Schools exam: one paper question, one exam session, one candidate attempt.</summary>
internal static class A2ExamBuilder
{
    private const int A2ReadingWritingTemplateId = 1;

    public sealed record SeededExam(int UserId, int PaperId, int QuestionId, int ExamId, int ExamSectionId, int CandidateId, int AttemptId);

    public static async Task<SeededExam> SeedAsync(ExamPrepDbContext db, CancellationToken cancellationToken = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        var admin = new User
        {
            Username = $"admin_{suffix}",
            PasswordHash = "hash",
            FullName = "Test Admin",
            Role = UserRole.Admin,
            CreatedAt = now,
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        var question = new Question
        {
            Number = 1,
            OrderNo = 1,
            PromptHtml = "Go upstairs if you want to",
            OptionsJson = """[{"key":"A","text":"buy a dress."},{"key":"B","text":"pay less for a book."},{"key":"C","text":"find a game."}]""",
            AnswerKeys = [new AnswerKey { Value = "B" }],
        };
        var paper = new TestPaper
        {
            SectionTemplateId = A2ReadingWritingTemplateId,
            Title = $"A2 RW test {suffix}",
            CreatedByUserId = admin.Id,
            CreatedAt = now,
            Parts =
            [
                new PaperPart
                {
                    PartNo = 1,
                    Groups =
                    [
                        new QuestionGroup
                        {
                            OrderNo = 1,
                            InteractionType = InteractionType.SingleChoice,
                            Instructions = "For each question, choose the correct answer.",
                            Questions = [question],
                        },
                    ],
                },
            ],
        };
        db.TestPapers.Add(paper);
        await db.SaveChangesAsync(cancellationToken);

        var section = new ExamSection
        {
            SectionTemplateId = A2ReadingWritingTemplateId,
            TestPaperId = paper.Id,
            OrderNo = 1,
            DurationMinutes = 60,
        };
        var candidate = new Candidate
        {
            CandidateNumber = "0001",
            FirstName = "An",
            LastName = "Nguyen",
            Username = $"nguyea0001_{suffix}",
            PasswordHash = "hash",
            PasswordProtected = "protected",
            CreatedAt = now,
        };
        var exam = new Exam
        {
            Name = $"A2 Key fS mock {suffix}",
            ExamTypeId = 1,
            ScheduledDate = new DateOnly(2026, 11, 20),
            SessionPin = "4821",
            InvigilatorPassword = "invig-pass",
            CreatedByUserId = admin.Id,
            CreatedAt = now,
            Sections = [section],
            Candidates = [candidate],
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);

        var attempt = new Attempt
        {
            CandidateId = candidate.Id,
            ExamSectionId = section.Id,
            Status = AttemptStatus.InProgress,
            StartedAt = now,
            EndsAt = now.AddMinutes(60),
        };
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        return new SeededExam(admin.Id, paper.Id, question.Id, exam.Id, section.Id, candidate.Id, attempt.Id);
    }
}
