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

    /// <summary>Set on the papers of a mix (Victory 20/10); null for an authored or imported paper.</summary>
    public int? PaperMixId { get; set; }

    public PaperMix? PaperMix { get; set; }

    /// <summary>Version 1–K within the mix; two versions of one family take different sources in every part.</summary>
    public int? MixVersion { get; set; }

    /// <summary>Which source each part came from, in part order, e.g. "ACBBAAB" (A = the first source).</summary>
    public string? MixPattern { get; set; }

    public ICollection<PaperPart> Parts { get; set; } = [];
}
