namespace ExamPrep.Domain.Entities;

/// <summary>A teacher's mark for a Writing response, which cannot be marked from an answer key.</summary>
public class ManualMark
{
    public int Id { get; set; }

    public long ResponseId { get; set; }

    public Response? Response { get; set; }

    public int MarkerUserId { get; set; }

    public User? Marker { get; set; }

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    /// <summary>Optional per-criterion marks as JSON.</summary>
    public string? CriteriaJson { get; set; }

    public string? Comment { get; set; }

    public DateTime MarkedAt { get; set; }
}
