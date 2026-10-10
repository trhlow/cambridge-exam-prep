using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>One section (Inspera "child test") of an exam type, in delivery order.</summary>
public class SectionTemplate
{
    public int Id { get; set; }

    public int ExamTypeId { get; set; }

    public ExamType? ExamType { get; set; }

    public int OrderNo { get; set; }

    public Skill Skill { get; set; }

    /// <summary>Title shown to the candidate, e.g. "Reading &amp; Writing".</summary>
    public required string Title { get; set; }

    public int DurationMinutes { get; set; }

    public int PartCount { get; set; }
}
