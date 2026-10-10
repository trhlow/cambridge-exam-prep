namespace ExamPrep.Domain.Entities;

/// <summary>An accepted answer. Several rows mean alternatives (e.g. "your" / "the").</summary>
public class AnswerKey
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    public required string Value { get; set; }

    public bool IsCaseSensitive { get; set; }
}
