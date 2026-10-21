using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>
/// Questions answered the same way under one instruction box ("Questions 1–6 / For each question, choose the correct answer.").
/// </summary>
public class QuestionGroup
{
    public int Id { get; set; }

    public int PaperPartId { get; set; }

    public PaperPart? PaperPart { get; set; }

    public int OrderNo { get; set; }

    public InteractionType InteractionType { get; set; }

    /// <summary>Instruction line shown under the "Questions x–y" heading.</summary>
    public required string Instructions { get; set; }

    /// <summary>Text containing the gaps for inline interactions; a gap is written as <c>{{q:19}}</c>.</summary>
    public string? BodyHtml { get; set; }

    /// <summary>DragDropMatch only: JSON array of word cards shared by the group's questions.</summary>
    public string? OptionPoolJson { get; set; }

    /// <summary>
    /// DragDropMatch only: heading over the questions' column (A2 Listening 5 "People", B1 Reading 2 "Young People");
    /// null shows "People" when the questions are rows, nothing when they are gaps in a text.
    /// </summary>
    public string? ItemsHeading { get; set; }

    /// <summary>DragDropMatch only: heading over the cards (B1 Reading 2 "Cycling Courses"); null shows none.</summary>
    public string? PoolHeading { get; set; }

    /// <summary>DragDropMatch only: whether one card may be dropped on several slots.</summary>
    public bool AllowOptionReuse { get; set; }

    /// <summary>InlineText only: maximum words per answer ("Write one word").</summary>
    public int? MaxWordsPerAnswer { get; set; }

    public ICollection<Question> Questions { get; set; } = [];
}
