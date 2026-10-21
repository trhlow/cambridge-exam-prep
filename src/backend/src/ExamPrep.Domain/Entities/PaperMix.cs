namespace ExamPrep.Domain.Entities;

/// <summary>
/// Four papers mixed part by part from two source papers of the same layout (Victory 20/10): each version takes every
/// part whole from source A or source B, so neighbouring computers can be given different papers.
/// </summary>
public class PaperMix
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int SectionTemplateId { get; set; }

    public SectionTemplate? SectionTemplate { get; set; }

    public int SourceAPaperId { get; set; }

    public TestPaper? SourceAPaper { get; set; }

    public int SourceBPaperId { get; set; }

    public TestPaper? SourceBPaper { get; set; }

    /// <summary>Seed of the random choice of parts: the same sources and seed give the same four papers.</summary>
    public int Seed { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>The four papers (<see cref="TestPaper.MixVersion"/> 1–4).</summary>
    public ICollection<TestPaper> Versions { get; set; } = [];
}
