namespace ExamPrep.Domain.Entities;

/// <summary>An examiner assigned to a session: they review its finished work, mark Writing and see its results.</summary>
public class ExamExaminer
{
    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime AssignedAt { get; set; }
}
