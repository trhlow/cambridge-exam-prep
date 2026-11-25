using System.Text.Json;
using ExamPrep.Domain.Entities;

namespace ExamPrep.Api.Monitoring;

/// <summary>
/// One line of the session's invigilator log (Cambridge: the invigilator records every incident and what was done).
/// <paramref name="Action"/> is the operation as the invigilator chose it (EndAt and EndIn rather than EditEndTime).
/// </summary>
/// <param name="Recipients">A message only: how many candidates it went to; 0 means the whole room.</param>
public sealed record ActionLogRow(
    long Id,
    DateTime At,
    string Action,
    string By,
    string? CandidateNumber,
    string? Candidate,
    string? Section,
    int? Minutes,
    DateTime? EndAt,
    bool DisregardExtraTime,
    bool WholeTest,
    string? Message,
    int? Recipients,
    string? Reason);

/// <summary>Reads the parameters each action wrote to <c>invigilator_actions.payload_json</c>.</summary>
public static class ActionLog
{
    public static ActionLogRow Row(InvigilatorAction action, string by, Candidate? candidate, string? section)
    {
        var payload = Parse(action.PayloadJson);
        string? Text(string name) => Find(payload, name) is { ValueKind: JsonValueKind.String } v ? v.GetString() : null;
        var minutes = Find(payload, "minutes") is { ValueKind: JsonValueKind.Number } m ? m.GetInt32() : (int?)null;
        var endAt = Find(payload, "endAt") is { ValueKind: JsonValueKind.String } e && e.TryGetDateTime(out var at) ? at : (DateTime?)null;
        var recipients = Find(payload, "candidateIds") is { ValueKind: JsonValueKind.Array } ids ? ids.GetArrayLength() : (int?)null;
        return new ActionLogRow(
            action.Id,
            action.CreatedAt,
            Text("action") ?? action.ActionType.ToString(),
            by,
            candidate?.CandidateNumber,
            candidate is null ? null : $"{candidate.FirstName} {candidate.LastName}",
            section,
            minutes,
            endAt,
            Find(payload, "disregardExtraTime") is { ValueKind: JsonValueKind.True },
            Text("level") == "test" || action.ActionType == Domain.Enums.InvigilatorActionType.EndAll,
            Text("text"),
            recipients,
            action.Reason);
    }

    /// <summary>The candidate or section an action without an attempt names in its payload.</summary>
    public static int? Id(string? payloadJson, string name) =>
        Find(Parse(payloadJson), name) is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : null;

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Anonymous objects were written with both camelCase (<c>candidateId</c>) and PascalCase (<c>Minutes</c>) names.</summary>
    private static JsonElement? Find(JsonElement? payload, string name) =>
        payload is { ValueKind: JsonValueKind.Object } o
            ? o.EnumerateObject().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Select(p => (JsonElement?)p.Value).FirstOrDefault()
            : null;
}
