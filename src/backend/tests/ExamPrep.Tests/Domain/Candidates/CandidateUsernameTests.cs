using ExamPrep.Domain.Candidates;

namespace ExamPrep.Tests.Domain.Candidates;

/// <summary>
/// VE-22: login usernames are built the Cambridge way — 5 letters of the family name, first initial, number; a shorter
/// family name takes more letters from the given name (Exam Day Booklet 2026, p. 11).
/// </summary>
public sealed class CandidateUsernameTests
{
    [Theory]
    [InlineData("Nguyen", "An", "0001", "nguyea0001")]
    [InlineData("Nguyễn", "An", "0101", "nguyea0101")]
    [InlineData("O'Brien-Smith", "kate", "A7", "obrieka7")]
    public void For_TakesFiveLettersOfTheFamilyNameTheFirstInitialAndTheNumber(string familyName, string givenName, string number, string expected)
    {
        Assert.Equal(expected, CandidateUsername.For(familyName, givenName, number));
    }

    [Theory]
    [InlineData("Lê", "Thị Hoa", "0002", "lethih0002")]
    [InlineData("Đặng", "Minh", "12", "dangmi12")]
    [InlineData("Lý", "An", "0003", "lyan0003")]
    [InlineData("Ng", "Bao", "0004", "ngbao0004")]
    public void For_TakesMoreLettersOfTheGivenName_WhenTheFamilyNameIsShorterThanFive(string familyName, string givenName, string number, string expected)
    {
        Assert.Equal(expected, CandidateUsername.For(familyName, givenName, number));
    }

    [Fact]
    public void For_RejectsNamesWithoutAnyLetter()
    {
        Assert.Throws<ArgumentException>(() => CandidateUsername.For("123", "An", "0001"));
    }
}
