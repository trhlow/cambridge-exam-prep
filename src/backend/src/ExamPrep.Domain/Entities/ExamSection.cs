namespace ExamPrep.Domain.Entities;

/// <summary>A section of an exam session (Inspera "child test"), bound to the paper it delivers.</summary>
public class ExamSection
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int SectionTemplateId { get; set; }

    public SectionTemplate? SectionTemplate { get; set; }

    public int TestPaperId { get; set; }

    public TestPaper? TestPaper { get; set; }

    public int OrderNo { get; set; }

    /// <summary>Copied from the template so later template edits do not change a scheduled exam.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// When the invigilator told the room to start this section. Candidates who start earlier get a
    /// "started early" warning; it never blocks them, matching Inspera.
    /// </summary>
    public DateTime? StartAnnouncedAt { get; set; }

    public ICollection<Attempt> Attempts { get; set; } = [];
}
