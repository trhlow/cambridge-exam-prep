namespace ExamPrep.Domain.Entities;

/// <summary>
/// Audit row of one correction of an answer key on a published paper: one row per corrected paper, so a source paper
/// and each mix version that took the question from it each have their own.
/// </summary>
public class AnswerCorrection
{
    public long Id { get; set; }

    public int TestPaperId { get; set; }

    public TestPaper? TestPaper { get; set; }

    public int QuestionNumber { get; set; }

    /// <summary>The accepted answers before, joined with " | ".</summary>
    public string OldAnswers { get; set; } = string.Empty;

    /// <summary>The accepted answers after, joined with " | ".</summary>
    public string NewAnswers { get; set; } = string.Empty;

    public int? CorrectedByUserId { get; set; }

    public DateTime CorrectedAt { get; set; }

    /// <summary>Marked attempts on the paper that were looked at again.</summary>
    public int AttemptsRemarked { get; set; }

    /// <summary>Attempts left as they were because they kept changing.</summary>
    public int AttemptsSkipped { get; set; }
}
