using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>
/// One candidate taking one section. The server owns the clock: <see cref="EndsAt"/> is recomputed
/// from the start time, duration, planned and incident extra time, pauses and end-time overrides.
/// </summary>
public class Attempt
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public Candidate? Candidate { get; set; }

    public int ExamSectionId { get; set; }

    public ExamSection? ExamSection { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.NotStarted;

    public DateTime? StartedAt { get; set; }

    /// <summary>Non-null while paused.</summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>Total seconds spent paused (pauses do not consume test time).</summary>
    public int PausedSeconds { get; set; }

    public int PlannedExtraSeconds { get; set; }

    public int IncidentExtraSeconds { get; set; }

    /// <summary>Set by "End now" with "Disregard extra time".</summary>
    public bool DisregardExtraTime { get; set; }

    /// <summary>Absolute end set by "End at" / "End in" / "End now"; null uses the computed end.</summary>
    public DateTime? EndAtOverride { get; set; }

    /// <summary>Effective end time, stored for monitoring queries.</summary>
    public DateTime? EndsAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public SubmitReason? SubmitReason { get; set; }

    public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Offline;

    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>Listening: when the candidate pressed Play.</summary>
    public DateTime? AudioStartedAt { get; set; }

    /// <summary>Listening: milliseconds to subtract from elapsed time (pauses and invigilator rewinds).</summary>
    public int AudioAdjustMs { get; set; }

    public decimal? RawScore { get; set; }

    public decimal? MaxScore { get; set; }

    public GradingStatus GradingStatus { get; set; } = GradingStatus.NotGraded;

    /// <summary>Optimistic concurrency token: timer changes from candidate and invigilator must not overwrite each other.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Response> Responses { get; set; } = [];

    public ICollection<Annotation> Annotations { get; set; } = [];
}
