namespace ExamPrep.Domain.Entities;

/// <summary>
/// Papers mixed part by part from 2–6 source papers of the same layout (Victory 20/10, widened 29/10): each version
/// takes every part whole from one of the sources, so neighbouring computers can be given different papers.
/// </summary>
public class PaperMix
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int SectionTemplateId { get; set; }

    public SectionTemplate? SectionTemplate { get; set; }

    /// <summary>The source papers in letter order: A, B, C, …</summary>
    public ICollection<PaperMixSource> Sources { get; set; } = [];

    /// <summary>How many papers were made (4 to 3 × the number of sources).</summary>
    public int VersionCount { get; set; }

    /// <summary>Seed of the random choice of parts: the same sources, count and seed give the same papers.</summary>
    public int Seed { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>The mixed papers (<see cref="TestPaper.MixVersion"/> 1–<see cref="VersionCount"/>).</summary>
    public ICollection<TestPaper> Versions { get; set; } = [];
}
