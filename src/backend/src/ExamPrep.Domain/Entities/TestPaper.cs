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

    /// <summary>Set on the four papers of a mix (Victory 20/10); null for an authored or imported paper.</summary>
    public int? PaperMixId { get; set; }

    public PaperMix? PaperMix { get; set; }

    /// <summary>Version 1–4 within the mix; versions 1/2 and 3/4 take opposite sources in every part.</summary>
    public int? MixVersion { get; set; }

    /// <summary>Which source each part came from, in part order, e.g. "ABBAABA".</summary>
    public string? MixPattern { get; set; }

    public ICollection<PaperPart> Parts { get; set; } = [];
}
