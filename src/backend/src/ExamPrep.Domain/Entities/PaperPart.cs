using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>A part of a paper (Part 1..7). Holds the passage/prompt and, for Listening, its audio track.</summary>
public class PaperPart
{
    public int Id { get; set; }

    public int TestPaperId { get; set; }

    public TestPaper? TestPaper { get; set; }

    public int PartNo { get; set; }

    /// <summary>Heading above the content, e.g. "School gardens competition".</summary>
    public string? Title { get; set; }

    /// <summary>Passage, task prompt or pictures shown with the questions (sanitised HTML).</summary>
    public string? StimulusHtml { get; set; }

    public PartLayout Layout { get; set; } = PartLayout.Single;

    /// <summary>
    /// Inspera shows this part one question per page (B1 Listening Part 2, B1 Writing Part 2) although the questions
    /// carry no pictures of their own. Parts whose every question has its pictures are paged anyway.
    /// </summary>
    public bool QuestionPerPage { get; set; }

    /// <summary>Listening only: this part's track. Tracks play back to back as one continuous recording.</summary>
    public int? AudioAssetId { get; set; }

    public Asset? AudioAsset { get; set; }

    public ICollection<QuestionGroup> Groups { get; set; } = [];
}
