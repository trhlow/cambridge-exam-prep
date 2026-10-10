namespace ExamPrep.Domain.Entities;

/// <summary>A numbered question. Numbers run through the whole paper (1..32 for A2 Reading &amp; Writing).</summary>
public class Question
{
    public int Id { get; set; }

    public int QuestionGroupId { get; set; }

    public QuestionGroup? QuestionGroup { get; set; }

    public int Number { get; set; }

    public int OrderNo { get; set; }

    /// <summary>Question text, or the row label for DragDropMatch ("Anton").</summary>
    public string? PromptHtml { get; set; }

    /// <summary>Material belonging to this question only, e.g. the notice image in A2 Reading Part 1.</summary>
    public string? StimulusHtml { get; set; }

    /// <summary>Choice interactions: JSON array of <c>{ key, text, imageAssetId }</c>.</summary>
    public string? OptionsJson { get; set; }

    public decimal Points { get; set; } = 1;

    /// <summary>Essay only: minimum word count shown in the instructions.</summary>
    public int? MinWords { get; set; }

    public ICollection<AnswerKey> AnswerKeys { get; set; } = [];
}
