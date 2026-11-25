using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;

namespace ExamPrep.Api.Results;

/// <summary>One skill of a candidate's result: raw marks, out of how many, and the Cambridge English Scale score.</summary>
/// <param name="Scale">Null until the skill is fully marked, or below the minimum Cambridge reports.</param>
public sealed record SkillResult(string Skill, decimal? RawScore, decimal? MaxScore, int? Scale);

/// <summary>
/// Splits section scores into Cambridge's four skills. A2 Key's Reading &amp; Writing section holds two: its automatic
/// marks are Reading and its Essay marks Writing, as Cambridge reports them (Parts 1–5 and 6–7).
/// </summary>
public static class SkillScores
{
    /// <param name="AutoRaw">The automatic marks of the attempt (questions other than Essay).</param>
    /// <param name="AutoMax">What the paper's questions other than Essay are worth.</param>
    public sealed record SectionAttempt(Skill Skill, AttemptStatus Status, GradingStatus Grading, decimal? RawScore, decimal? MaxScore, decimal AutoRaw, decimal AutoMax);

    /// <summary>
    /// The four skills in Cambridge's order with the overall score and grade once all four have a scale score; null when
    /// the exam type has no conversion table.
    /// </summary>
    public static (IReadOnlyList<SkillResult> Skills, int? Overall, CambridgeScale.Band? Band)? For(string examTypeCode, IEnumerable<SectionAttempt> attempts, decimal? speakingScore)
    {
        if (!CambridgeScale.Covers(examTypeCode))
        {
            return null;
        }

        var raw = new Dictionary<string, (decimal Raw, decimal Max)>();
        foreach (var a in attempts.Where(a => a.Status == AttemptStatus.Submitted && a.Grading != GradingStatus.NotGraded && a.RawScore is not null && a.MaxScore is not null))
        {
            var final = a.Grading is GradingStatus.Final or GradingStatus.AutoGraded;
            switch (a.Skill)
            {
                case Skill.ReadingWriting:
                    raw[CambridgeScale.Reading] = (a.AutoRaw, a.AutoMax);
                    if (final)
                    {
                        raw[CambridgeScale.Writing] = (a.RawScore!.Value - a.AutoRaw, a.MaxScore!.Value - a.AutoMax);
                    }

                    break;
                case Skill.Reading or Skill.Listening when final:
                    raw[a.Skill.ToString()] = (a.RawScore!.Value, a.MaxScore!.Value);
                    break;
                case Skill.Writing when final:
                    raw[CambridgeScale.Writing] = (a.RawScore!.Value, a.MaxScore!.Value);
                    break;
            }
        }

        if (speakingScore is { } speaking)
        {
            raw[CambridgeScale.Speaking] = (speaking, CambridgeScale.SpeakingMax(examTypeCode)!.Value);
        }

        var skills = CambridgeScale.Skills.Select(skill => raw.TryGetValue(skill, out var r)
            ? new SkillResult(skill, r.Raw, r.Max, CambridgeScale.Score(examTypeCode, skill, r.Raw, r.Max))
            // Speaking keeps its maximum before it is entered, for the form that takes it.
            : new SkillResult(skill, null, skill == CambridgeScale.Speaking ? CambridgeScale.SpeakingMax(examTypeCode) : null, null)).ToList();
        if (skills.Any(s => s.Scale is null))
        {
            return (skills, null, null);
        }

        var overall = CambridgeScale.Overall(skills.Select(s => s.Scale!.Value).ToList());
        return (skills, overall, CambridgeScale.GradeFor(examTypeCode, overall));
    }
}
