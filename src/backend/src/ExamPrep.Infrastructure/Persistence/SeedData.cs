using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Infrastructure.Persistence;

/// <summary>Reference data inserted by migrations: exam types, their section templates, default branding.</summary>
internal static class SeedData
{
    public const int A2KeyForSchools = 1;
    public const int B1PreliminaryForSchools = 2;

    public static readonly DateTime SeededAt = new(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

    public static ExamType[] ExamTypes =>
    [
        new() { Id = A2KeyForSchools, Code = "A2_KEY_FS", Name = "A2 Key for Schools" },
        new() { Id = B1PreliminaryForSchools, Code = "B1_PRELIM_FS", Name = "B1 Preliminary for Schools" },
    ];

    /// <summary>Durations and part counts from the Exam Day Booklet for Digital exams (January 2026).</summary>
    public static SectionTemplate[] SectionTemplates =>
    [
        new() { Id = 1, ExamTypeId = A2KeyForSchools, OrderNo = 1, Skill = Skill.ReadingWriting, Title = "Reading & Writing", DurationMinutes = 60, PartCount = 7 },
        new() { Id = 2, ExamTypeId = A2KeyForSchools, OrderNo = 2, Skill = Skill.Listening, Title = "Listening", DurationMinutes = 30, PartCount = 5 },
        new() { Id = 3, ExamTypeId = B1PreliminaryForSchools, OrderNo = 1, Skill = Skill.Reading, Title = "Reading", DurationMinutes = 45, PartCount = 6 },
        new() { Id = 4, ExamTypeId = B1PreliminaryForSchools, OrderNo = 2, Skill = Skill.Writing, Title = "Writing", DurationMinutes = 45, PartCount = 2 },
        new() { Id = 5, ExamTypeId = B1PreliminaryForSchools, OrderNo = 3, Skill = Skill.Listening, Title = "Listening", DurationMinutes = 30, PartCount = 4 },
    ];

    public static SiteSetting DefaultSiteSetting => new() { Id = 1, CentreName = "Victory", UpdatedAt = SeededAt };
}
