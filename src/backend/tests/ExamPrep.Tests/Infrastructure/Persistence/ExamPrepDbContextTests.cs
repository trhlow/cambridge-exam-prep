using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExamPrep.Tests.Infrastructure.Persistence;

/// <summary>Integration tests against a real MySQL database built from the migrations.</summary>
public sealed class ExamPrepDbContextTests(MySqlDatabaseFixture database) : IClassFixture<MySqlDatabaseFixture>
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrations_SeedA2SectionsInDeliveryOrder()
    {
        database.SkipIfUnavailable();
        await using var db = database.CreateContext();

        var sections = await db.SectionTemplates
            .Where(s => s.ExamType!.Code == "A2_KEY_FS")
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Skill, s.DurationMinutes, s.PartCount })
            .ToListAsync(ct);

        Assert.Equal(
            [new { Skill = Skill.ReadingWriting, DurationMinutes = 60, PartCount = 7 }, new { Skill = Skill.Listening, DurationMinutes = 30, PartCount = 5 }],
            sections);
    }

    [Fact]
    public async Task Migrations_SeedVictoryAsDefaultCentreName()
    {
        database.SkipIfUnavailable();
        await using var db = database.CreateContext();

        var setting = await db.SiteSettings.SingleAsync(ct);

        Assert.Equal("Victory", setting.CentreName);
        Assert.Null(setting.LogoAssetId);
    }

    [Fact]
    public async Task SaveChanges_RoundTripsA2PaperExamAndAttempt()
    {
        database.SkipIfUnavailable();
        A2ExamBuilder.SeededExam seeded;
        await using (var db = database.CreateContext())
        {
            seeded = await A2ExamBuilder.SeedAsync(db, ct);
        }

        await using var read = database.CreateContext();
        var question = await read.Questions
            .Include(q => q.AnswerKeys)
            .Include(q => q.QuestionGroup!).ThenInclude(g => g.PaperPart)
            .SingleAsync(q => q.Id == seeded.QuestionId, ct);
        var attempt = await read.Attempts.SingleAsync(a => a.Id == seeded.AttemptId, ct);

        Assert.Equal("B", Assert.Single(question.AnswerKeys).Value);
        Assert.Equal(InteractionType.SingleChoice, question.QuestionGroup!.InteractionType);
        Assert.Equal(1, question.QuestionGroup.PaperPart!.PartNo);
        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(DateTimeKind.Utc, attempt.StartedAt!.Value.Kind);
    }

    [Fact]
    public async Task SaveChanges_StoresEnumsAsReadableText()
    {
        database.SkipIfUnavailable();
        await using var db = database.CreateContext();
        var seeded = await A2ExamBuilder.SeedAsync(db, ct);

        var stored = await db.Database
            .SqlQuery<string>($"SELECT status AS Value FROM attempts WHERE id = {seeded.AttemptId}")
            .SingleAsync(ct);

        Assert.Equal("InProgress", stored);
    }

    [Fact]
    public async Task SaveChanges_Throws_WhenCandidateUsernameIsTaken()
    {
        database.SkipIfUnavailable();
        await using var db = database.CreateContext();
        var seeded = await A2ExamBuilder.SeedAsync(db, ct);
        var existing = await db.Candidates.AsNoTracking().SingleAsync(c => c.Id == seeded.CandidateId, ct);

        db.Candidates.Add(new Candidate
        {
            ExamId = seeded.ExamId,
            CandidateNumber = "0002",
            FirstName = "Binh",
            LastName = "Tran",
            Username = existing.Username,
            PasswordHash = "hash",
            PasswordProtected = "protected",
            CreatedAt = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task SaveChanges_Throws_WhenSecondResponseIsAddedForSameQuestion()
    {
        database.SkipIfUnavailable();
        await using var db = database.CreateContext();
        var seeded = await A2ExamBuilder.SeedAsync(db, ct);

        db.Responses.Add(new Response { AttemptId = seeded.AttemptId, QuestionId = seeded.QuestionId, Value = "A", UpdatedAt = DateTime.UtcNow });
        db.Responses.Add(new Response { AttemptId = seeded.AttemptId, QuestionId = seeded.QuestionId, Value = "B", UpdatedAt = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task SaveChanges_Throws_WhenAttemptWasChangedByAnotherSession()
    {
        database.SkipIfUnavailable();
        int attemptId;
        await using (var seedDb = database.CreateContext())
        {
            attemptId = (await A2ExamBuilder.SeedAsync(seedDb, ct)).AttemptId;
        }

        await using var invigilator = database.CreateContext();
        await using var candidate = database.CreateContext();
        var pausedByInvigilator = await invigilator.Attempts.SingleAsync(a => a.Id == attemptId, ct);
        var heartbeatFromCandidate = await candidate.Attempts.SingleAsync(a => a.Id == attemptId, ct);

        pausedByInvigilator.Status = AttemptStatus.Paused;
        pausedByInvigilator.ConcurrencyStamp = Guid.NewGuid();
        await invigilator.SaveChangesAsync(ct);

        heartbeatFromCandidate.LastHeartbeatAt = DateTime.UtcNow;
        heartbeatFromCandidate.ConcurrencyStamp = Guid.NewGuid();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => candidate.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task SaveChanges_Throws_WhenDeletingPaperUsedByAnExam()
    {
        database.SkipIfUnavailable();
        int paperId;
        await using (var seedDb = database.CreateContext())
        {
            paperId = (await A2ExamBuilder.SeedAsync(seedDb, ct)).PaperId;
        }

        await using var db = database.CreateContext();
        db.TestPapers.Remove(await db.TestPapers.SingleAsync(p => p.Id == paperId, ct));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task DeletingExam_RemovesItsCandidatesAndAttempts()
    {
        database.SkipIfUnavailable();
        A2ExamBuilder.SeededExam seeded;
        await using (var db = database.CreateContext())
        {
            seeded = await A2ExamBuilder.SeedAsync(db, ct);
            db.Exams.Remove(await db.Exams.SingleAsync(e => e.Id == seeded.ExamId, ct));
            await db.SaveChangesAsync(ct);
        }

        await using var read = database.CreateContext();

        Assert.False(await read.Candidates.AnyAsync(c => c.Id == seeded.CandidateId, ct));
        Assert.False(await read.Attempts.AnyAsync(a => a.Id == seeded.AttemptId, ct));
        Assert.True(await read.TestPapers.AnyAsync(p => p.Id == seeded.PaperId, ct));
    }
}
