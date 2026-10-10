namespace ExamPrep.Domain.Entities;

/// <summary>The candidate's current answer to one question (saved on every change).</summary>
public class Response
{
    public long Id { get; set; }

    public int AttemptId { get; set; }

    public Attempt? Attempt { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    /// <summary>Option key ("B"), typed word, card key, or essay text.</summary>
    public string? Value { get; set; }

    public bool IsFlagged { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Set by auto-marking; null for essays and unmarked responses.</summary>
    public bool? IsCorrect { get; set; }

    public decimal? AutoScore { get; set; }

    public ICollection<ManualMark> ManualMarks { get; set; } = [];
}
