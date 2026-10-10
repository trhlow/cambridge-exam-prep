using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>A private highlight or note the candidate made on the text during the test.</summary>
public class Annotation
{
    public long Id { get; set; }

    public int AttemptId { get; set; }

    public Attempt? Attempt { get; set; }

    public int PaperPartId { get; set; }

    public PaperPart? PaperPart { get; set; }

    public int? QuestionId { get; set; }

    public AnnotationKind Kind { get; set; }

    /// <summary>JSON describing the selected text range inside the rendered part.</summary>
    public required string AnchorJson { get; set; }

    public string? NoteText { get; set; }

    public DateTime CreatedAt { get; set; }
}
