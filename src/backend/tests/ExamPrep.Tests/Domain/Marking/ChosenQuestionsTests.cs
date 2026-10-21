using ExamPrep.Domain.Enums;
using ExamPrep.Domain.Marking;
using static ExamPrep.Domain.Marking.ChosenQuestions;

namespace ExamPrep.Tests.Domain.Marking;

/// <summary>VE-35: which questions of a "choose one" group are marked, and what the group is worth.</summary>
public sealed class ChosenQuestionsTests
{
    private static Answer A(int number, QuestionSelection selection = QuestionSelection.Undecided, bool written = true) => new(number, selection, written);

    [Fact]
    public void EveryQuestionCounts_WhenTheGroupHasNoChoice() =>
        Assert.Equal([2, 3], Counted(null, [A(3), A(2, written: false)]).Order());

    [Fact]
    public void TheQuestionSetToYes_Counts() =>
        Assert.Equal([3], Counted(1, [A(2), A(3, QuestionSelection.Yes)]));

    [Fact]
    public void WithoutAChoice_TheFirstWrittenAnswerCounts() =>
        Assert.Equal([3], Counted(1, [A(2, written: false), A(3), A(4)]));

    [Fact]
    public void AQuestionSetToNo_NeverCounts() =>
        Assert.Equal([3], Counted(1, [A(2, QuestionSelection.No), A(3)]));

    [Fact]
    public void NothingCounts_WhenNothingWasChosenOrWritten() =>
        Assert.Empty(Counted(1, [A(2, written: false), A(3, written: false)]));

    [Theory]
    [InlineData(null, 40)]
    [InlineData(1, 20)]
    public void TheGroupIsWorthItsBestQuestions(int? chooseCount, int expected) =>
        Assert.Equal(expected, MaxPoints(chooseCount, [20m, 20m]));
}
