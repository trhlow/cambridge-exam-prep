using ExamPrep.Domain.Common;
using ExamPrep.Domain.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Api.Monitoring;

/// <summary>The operations of the Inspera child-test monitoring page.</summary>
public enum MonitorAction
{
    Pause,
    Resume,
    AddIncidentTime,
    EndNow,
    EndAt,
    EndIn,
    UseTestEndTime,
    RewindAudio,
}

/// <summary>What the invigilator chose, already checked for shape (minutes present, end time given…).</summary>
public sealed record MonitorCommand(MonitorAction Action, int? Minutes, DateTime? EndAt, bool DisregardExtraTime);

/// <summary>
/// Applies one invigilator operation to one candidate's attempt, under the same conditions as Inspera: pause only a
/// candidate who is online and working, resume only one who is online, rewind only while not paused and by no more than has played, never touch a
/// section that has ended. Returns null when applied, or the reason it was skipped.
/// </summary>
public static class InvigilatorActionRules
{
    public static string? Apply(MonitorCommand command, Attempt? attempt, AttemptClock? clock, bool isOnline, DateTime now, TimeSpan? recordingLength = null)
    {
        if (attempt is null || clock is null)
        {
            return "has not started this section";
        }

        if (attempt.Status == AttemptStatus.Submitted)
        {
            return "has already finished this section";
        }

        try
        {
            return Run(command, attempt, clock, isOnline, now, recordingLength);
        }
        catch (Exception ex) when (ex is DomainRuleException or ArgumentOutOfRangeException)
        {
            return ex is ArgumentOutOfRangeException range ? FirstSentence(range.Message) : ex.Message;
        }
    }

    public static InvigilatorActionType LogType(MonitorAction action) => action switch
    {
        MonitorAction.Pause => InvigilatorActionType.Pause,
        MonitorAction.Resume => InvigilatorActionType.Resume,
        MonitorAction.AddIncidentTime => InvigilatorActionType.AddIncidentExtraTime,
        MonitorAction.EndNow => InvigilatorActionType.EndNow,
        MonitorAction.EndAt or MonitorAction.EndIn => InvigilatorActionType.EditEndTime,
        MonitorAction.UseTestEndTime => InvigilatorActionType.UseTestEndTime,
        MonitorAction.RewindAudio => InvigilatorActionType.RewindAudio,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    private static string? Run(MonitorCommand command, Attempt attempt, AttemptClock clock, bool isOnline, DateTime now, TimeSpan? recordingLength)
    {
        var minutes = TimeSpan.FromMinutes(command.Minutes ?? 0);
        switch (command.Action)
        {
            case MonitorAction.Pause when !isOnline:
                return "is offline; add incident time when they log back in";
            case MonitorAction.Pause:
                clock.Pause(now);
                break;
            case MonitorAction.Resume when !isOnline:
                return "is offline; resume them when they are back online";
            case MonitorAction.Resume:
                clock.Resume(now);
                break;
            case MonitorAction.AddIncidentTime:
                clock.AddIncidentExtraTime(minutes);
                break;
            case MonitorAction.EndNow:
                clock.EndNow(command.DisregardExtraTime, now);
                break;
            case MonitorAction.EndAt:
                clock.EndAt(command.EndAt!.Value, now);
                break;
            case MonitorAction.EndIn:
                clock.EndIn(minutes, now);
                break;
            case MonitorAction.UseTestEndTime:
                clock.UseTestEndTime();
                break;
            case MonitorAction.RewindAudio when attempt.AudioStartedAt is null:
                return "has not started the recording yet";
            case MonitorAction.RewindAudio when minutes > Played(clock, now, recordingLength):
                return $"cannot be rewound further than the recording has played ({(int)Played(clock, now, recordingLength).TotalMinutes} min)";
            case MonitorAction.RewindAudio:
                clock.RewindAudio(minutes, now);
                break;
        }

        return null;
    }

    /// <summary>What has been played of the recording: the position, but never more than the recording is long.</summary>
    public static TimeSpan Played(AttemptClock clock, DateTime now, TimeSpan? recordingLength) =>
        recordingLength is { } length && clock.AudioPosition(now) > length ? length : clock.AudioPosition(now);

    /// <summary>ArgumentOutOfRangeException appends the parameter name; keep the sentence for the invigilator.</summary>
    private static string FirstSentence(string message)
    {
        var end = message.IndexOf(" (Parameter", StringComparison.Ordinal);
        return end > 0 ? message[..end] : message;
    }
}
