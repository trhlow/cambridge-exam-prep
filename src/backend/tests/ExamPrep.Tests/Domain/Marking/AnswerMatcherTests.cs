using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;

namespace ExamPrep.Tests.Domain.Marking;

/// <summary>VE-11: deciding whether a response matches the answer key.</summary>
public sealed class AnswerMatcherTests
{
    private static AnswerKey[] Keys(params string[] values) => values.Select(v => new AnswerKey { Value = v }).ToArray();

    [Theory]
    [InlineData(InteractionType.SingleChoice, "B")]
    [InlineData(InteractionType.SingleChoice, "b")]
    [InlineData(InteractionType.ImageChoice, " B ")]
    [InlineData(InteractionType.InlineChoice, "B")]
    [InlineData(InteractionType.DragDropMatch, "B")]
    public void ChoiceIsCorrect_WhenTheChosenKeyMatches(InteractionType type, string response)
    {
        Assert.True(AnswerMatcher.IsCorrect(type, response, Keys("B")));
    }

    [Theory]
    [InlineData(InteractionType.SingleChoice, "A")]
    [InlineData(InteractionType.DragDropMatch, "")]
    [InlineData(InteractionType.InlineChoice, null)]
    public void ChoiceIsWrong_WhenAnotherKeyOrNothingIsChosen(InteractionType type, string? response)
    {
        Assert.False(AnswerMatcher.IsCorrect(type, response, Keys("B")));
    }

    [Theory]
    [InlineData("for")]
    [InlineData("For")]
    [InlineData("FOR")]
    [InlineData("  for  ")]
    [InlineData("for.")]
    public void TypedAnswer_IgnoresCaseSurroundingSpacesAndAFinalFullStop(string response)
    {
        Assert.True(AnswerMatcher.IsCorrect(InteractionType.InlineText, response, Keys("for")));
    }

    [Theory]
    [InlineData("14")]
    [InlineData("14th")]
    [InlineData("Fourteenth")]
    public void TypedAnswer_AcceptsEveryAlternativeInTheKey(string response)
    {
        Assert.True(AnswerMatcher.IsCorrect(InteractionType.InlineText, response, Keys("14", "14th", "fourteenth")));
    }

    [Theory]
    [InlineData("’ll")]
    [InlineData("‘ll")]
    [InlineData("quarter  past   eight")]
    public void TypedAnswer_TreatsCurlyApostrophesAndRepeatedSpacesAsTheSame(string response)
    {
        Assert.True(AnswerMatcher.IsCorrect(InteractionType.InlineText, response, Keys("'ll", "quarter past eight")));
    }

    [Theory]
    [InlineData("from")]
    [InlineData("fo r")]
    [InlineData("")]
    [InlineData(null)]
    public void TypedAnswer_IsWrong_WhenItDoesNotMatchAnyAlternative(string? response)
    {
        Assert.False(AnswerMatcher.IsCorrect(InteractionType.InlineText, response, Keys("for")));
    }

    [Fact]
    public void TypedAnswer_RespectsCase_WhenTheKeyIsCaseSensitive()
    {
        AnswerKey[] keys = [new AnswerKey { Value = "Bristol", IsCaseSensitive = true }];

        Assert.True(AnswerMatcher.IsCorrect(InteractionType.InlineText, "Bristol", keys));
        Assert.False(AnswerMatcher.IsCorrect(InteractionType.InlineText, "bristol", keys));
    }

    [Fact]
    public void Essay_CannotBeMarkedFromAKey()
    {
        Assert.False(AnswerMatcher.CanAutoMark(InteractionType.Essay));
        Assert.True(AnswerMatcher.CanAutoMark(InteractionType.InlineText));
    }
}
