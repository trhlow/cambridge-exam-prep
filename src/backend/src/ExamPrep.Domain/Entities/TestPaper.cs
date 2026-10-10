using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>Authored content for one section template (e.g. "A2 Key fS Reading &amp; Writing, Test 1").</summary>
public class TestPaper
{
    public int Id { get; set; }

    public int SectionTemplateId { get; set; }

    public SectionTemplate? SectionTemplate { get; set; }

    public required string Title { get; set; }

    public PaperStatus Status { get; set; } = PaperStatus.Draft;

    public int Version { get; set; } = 1;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public ICollection<PaperPart> Parts { get; set; } = [];
}
