namespace ExamPrep.Domain.Exams;

/// <summary>
/// Cambridge Digital Exam Day Booklet 2026, "Requirements for invigilators / supervisors": "You must have at least one
/// invigilator for every 25 candidates in each exam room." A session is one room; its invigilators all watch the
/// whole room.
/// </summary>
public static class Invigilation
{
    public const int CandidatesPerInvigilator = 25;

    /// <summary>The fewest invigilators a room of this many candidates needs (at least one).</summary>
    public static int Needed(int candidates) => Math.Max(1, (candidates + CandidatesPerInvigilator - 1) / CandidatesPerInvigilator);
}
