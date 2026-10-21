using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Domain.Papers;

/// <summary>In-memory papers for the shuffling rules: one group per part, numbered on from the previous part.</summary>
internal static class PaperFactory
{
    /// <summary>A paper whose parts hold the given (interaction, question count); Listening parts get a recording.</summary>
    public static TestPaper Paper(int id, string title, params (InteractionType Type, int Questions)[] parts) =>
        Paper(id, title, templateId: 1, withAudio: false, parts);

    public static TestPaper Paper(int id, string title, int templateId, bool withAudio, params (InteractionType Type, int Questions)[] parts)
    {
        var paper = new TestPaper { Id = id, Title = title, SectionTemplateId = templateId };
        var number = 1;
        for (var i = 0; i < parts.Length; i++)
        {
            var (type, count) = parts[i];
            var group = new QuestionGroup { OrderNo = 1, InteractionType = type, Instructions = $"Part {i + 1}" };
            for (var q = 0; q < count; q++)
            {
                group.Questions.Add(new Question { Number = number, OrderNo = q + 1, AnswerKeys = [new AnswerKey { Value = $"{title}-{number}" }] });
                number++;
            }

            paper.Parts.Add(new PaperPart { PartNo = i + 1, Title = $"{title} part {i + 1}", AudioAssetId = withAudio ? 100 * id + i : null, Groups = [group] });
        }

        return paper;
    }

    /// <summary>A2 Reading &amp; Writing: 7 parts, 32 questions.</summary>
    public static TestPaper A2ReadingWriting(int id, string title) => Paper(id, title,
        (InteractionType.SingleChoice, 6), (InteractionType.SingleChoice, 7), (InteractionType.SingleChoice, 5),
        (InteractionType.InlineChoice, 6), (InteractionType.InlineText, 6), (InteractionType.Essay, 1), (InteractionType.Essay, 1));
}
