namespace ExamPrep.Domain.Entities;

/// <summary>One source paper of a mix; <see cref="Position"/> 1 is the letter A of the patterns.</summary>
public class PaperMixSource
{
    public int PaperMixId { get; set; }

    public int Position { get; set; }

    public int TestPaperId { get; set; }

    public TestPaper? TestPaper { get; set; }

    /// <summary>"A" for position 1, "B" for 2, …</summary>
    public string Letter => ((char)('A' + Position - 1)).ToString();
}
