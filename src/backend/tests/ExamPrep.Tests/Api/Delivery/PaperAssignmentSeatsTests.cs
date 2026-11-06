using ExamPrep.Api.Delivery;
using ExamPrep.Domain.Entities;

namespace ExamPrep.Tests.Api.Delivery;

/// <summary>
/// Final review A12 / B16: a candidate without a computer number takes the lowest number nobody typed, in list order,
/// so going Active can never give two candidates the same seat.
/// </summary>
public sealed class PaperAssignmentSeatsTests
{
    private static Candidate Candidate(int id, string number, string? seat) => new()
    {
        Id = id,
        CandidateNumber = number,
        FirstName = "An",
        LastName = "Le",
        Username = $"lean{id}",
        PasswordHash = string.Empty,
        PasswordProtected = string.Empty,
        SeatNo = seat,
    };

    [Fact]
    public void UnseatedCandidates_TakeTheLowestNumbersNobodyTyped_InListOrder()
    {
        // Typed seats 1 and 3; the two unseated candidates come first and third in the list.
        var seats = PaperAssignment.SeatsOf([Candidate(10, "0001", null), Candidate(11, "0002", "3"), Candidate(12, "0003", null), Candidate(13, "0004", "1")]);

        Assert.Equal(2, seats[10]);
        Assert.Equal(4, seats[12]);
        Assert.Equal(3, seats[11]);
        Assert.Equal(1, seats[13]);
        Assert.Equal(seats.Count, seats.Values.Distinct().Count());
    }

    [Fact]
    public void WithoutTypedSeats_TheListOrderGivesTheSeats()
    {
        var seats = PaperAssignment.SeatsOf([Candidate(2, "0002", null), Candidate(1, "0001", "  "), Candidate(3, "0003", "A3")]);

        Assert.Equal([1, 2, 3], new[] { seats[1], seats[2], seats[3] });
    }
}
