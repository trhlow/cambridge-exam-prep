namespace ExamPrep.Domain.Enums;

/// <summary>Warnings the server can actually observe for a candidate.</summary>
public enum SecurityEventType
{
    InvalidSebKey,
    LockdownBrowserExited,
    ConnectionLost,
    ClockSkew,
    StartedEarly,
}
