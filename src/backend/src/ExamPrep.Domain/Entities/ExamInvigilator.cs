namespace ExamPrep.Domain.Entities;

public class ExamInvigilator
{
    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime AssignedAt { get; set; }
}
