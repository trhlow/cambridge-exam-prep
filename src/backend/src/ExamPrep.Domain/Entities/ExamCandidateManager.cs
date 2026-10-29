namespace ExamPrep.Domain.Entities;

/// <summary>
/// VE-72 (Victory 29/10): a manager of candidates assigned to a session by the person who created it; managers of
/// candidates enter and change candidates only in the sessions they are assigned to.
/// </summary>
public class ExamCandidateManager
{
    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime AssignedAt { get; set; }
}
