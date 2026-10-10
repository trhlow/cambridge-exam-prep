namespace ExamPrep.Domain.Enums;

/// <summary>Invigilator operations, mirroring the Inspera Admin Portal.</summary>
public enum InvigilatorActionType
{
    Pause,
    Resume,
    AddIncidentExtraTime,
    EditEndTime,
    EndNow,
    UseTestEndTime,
    RewindAudio,
    SendMessage,
}
