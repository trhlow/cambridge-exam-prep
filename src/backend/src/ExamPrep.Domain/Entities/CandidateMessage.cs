namespace ExamPrep.Domain.Entities;

/// <summary>A message from the invigilator shown under the bell icon. No candidate means the whole room.</summary>
public class CandidateMessage
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public int? CandidateId { get; set; }

    public Candidate? Candidate { get; set; }

    public required string Text { get; set; }

    public int SentByUserId { get; set; }

    public DateTime SentAt { get; set; }
}
