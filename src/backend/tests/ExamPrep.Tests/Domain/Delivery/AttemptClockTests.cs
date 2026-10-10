using ExamPrep.Domain.Common;
using ExamPrep.Domain.Delivery;
using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Enums;

namespace ExamPrep.Tests.Domain.Delivery;

/// <summary>Server-side exam clock rules (Inspera invigilator guide and Exam Day Booklet).</summary>
public sealed class AttemptClockTests
{
    private const int SectionMinutes = 60;
    private static readonly DateTime T0 = new(2026, 11, 20, 2, 0, 0, DateTimeKind.Utc);

    private static DateTime At(int minutes) => T0.AddMinutes(minutes);

    private static (Attempt Attempt, AttemptClock Clock) Started(int plannedExtraPercent = 0, DateTime? parentEnd = null)
    {
        var attempt = new Attempt();
        var clock = new AttemptClock(attempt, SectionMinutes, parentEnd);
        clock.Start(plannedExtraPercent, T0);
        return (attempt, clock);
    }

    [Fact]
    public void Start_SetsInProgressAndEndAfterSectionDuration()
    {
        var (attempt, _) = Started();

        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(T0, attempt.StartedAt);
        Assert.Equal(At(60), attempt.EndsAt);
    }

    [Fact]
    public void Start_AddsPlannedExtraTimeAsPercentageOfDuration()
    {
        var (attempt, clock) = Started(plannedExtraPercent: 25);

        Assert.Equal(15 * 60, attempt.PlannedExtraSeconds);
        Assert.Equal(At(75), attempt.EndsAt);
        Assert.Equal(TimeSpan.FromMinutes(75), clock.Remaining(T0));
    }

    [Fact]
    public void Start_Throws_WhenAlreadyStarted()
    {
        var (_, clock) = Started();

        Assert.Throws<DomainRuleException>(() => clock.Start(0, At(1)));
    }

    [Fact]
    public void Remaining_IsFrozen_WhilePaused()
    {
        var (_, clock) = Started();
        clock.Pause(At(10));

        Assert.Equal(TimeSpan.FromMinutes(50), clock.Remaining(At(30)));
    }

    [Fact]
    public void Resume_MovesEndByTimeSpentPaused()
    {
        var (attempt, clock) = Started();
        clock.Pause(At(10));

        clock.Resume(At(20));

        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(At(70), attempt.EndsAt);
        Assert.Equal(TimeSpan.FromMinutes(50), clock.Remaining(At(20)));
    }

    [Fact]
    public void Pause_Throws_WhenNotInProgress()
    {
        var (_, clock) = Started();
        clock.Pause(At(5));

        Assert.Throws<DomainRuleException>(() => clock.Pause(At(6)));
    }

    [Fact]
    public void Resume_Throws_WhenNotPaused()
    {
        var (_, clock) = Started();

        Assert.Throws<DomainRuleException>(() => clock.Resume(At(5)));
    }

    [Fact]
    public void AddIncidentExtraTime_MovesEndLater()
    {
        var (attempt, clock) = Started();

        clock.AddIncidentExtraTime(TimeSpan.FromMinutes(5));

        Assert.Equal(5 * 60, attempt.IncidentExtraSeconds);
        Assert.Equal(At(65), attempt.EndsAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AddIncidentExtraTime_Throws_WhenNotPositive(int minutes)
    {
        var (_, clock) = Started();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.AddIncidentExtraTime(TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void EndIn_LeavesExactlyThatMuchTime()
    {
        var (attempt, clock) = Started(plannedExtraPercent: 25);

        clock.EndIn(TimeSpan.FromMinutes(10), At(20));

        Assert.Equal(At(30), attempt.EndsAt);
        Assert.Equal(TimeSpan.FromMinutes(10), clock.Remaining(At(20)));
    }

    [Fact]
    public void EndAt_EndsAtTheGivenTime()
    {
        var (attempt, clock) = Started();

        clock.EndAt(At(45), At(20));

        Assert.Equal(At(45), attempt.EndsAt);
    }

    [Fact]
    public void EndAt_Throws_WhenTimeIsInThePast()
    {
        var (_, clock) = Started();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.EndAt(At(10), At(20)));
    }

    [Fact]
    public void PauseAfterEndIn_StillMovesTheEnd()
    {
        var (attempt, clock) = Started();
        clock.EndIn(TimeSpan.FromMinutes(10), At(20));

        clock.Pause(At(25));
        clock.Resume(At(30));

        Assert.Equal(At(35), attempt.EndsAt);
    }

    [Fact]
    public void EndNow_KeepsExtraTime_WhenNotDisregarded()
    {
        var (attempt, clock) = Started(plannedExtraPercent: 25);
        clock.AddIncidentExtraTime(TimeSpan.FromMinutes(5));

        clock.EndNow(disregardExtraTime: false, At(20));

        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(At(40), attempt.EndsAt);
    }

    [Fact]
    public void EndNow_SubmitsImmediately_WhenExtraTimeIsDisregarded()
    {
        var (attempt, clock) = Started(plannedExtraPercent: 25);

        clock.EndNow(disregardExtraTime: true, At(20));

        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.Equal(SubmitReason.InvigilatorEndNow, attempt.SubmitReason);
        Assert.Equal(At(20), attempt.SubmittedAt);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(At(20)));
    }

    [Fact]
    public void EndNow_SubmitsImmediately_WhenCandidateHasNoExtraTime()
    {
        var (attempt, clock) = Started();

        clock.EndNow(disregardExtraTime: false, At(20));

        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
    }

    [Fact]
    public void UseTestEndTime_UndoesEndInAndDisregard()
    {
        var (attempt, clock) = Started(plannedExtraPercent: 25);
        clock.EndIn(TimeSpan.FromMinutes(1), At(20));

        clock.UseTestEndTime();

        Assert.Equal(0, attempt.EndAdjustSeconds);
        Assert.Equal(At(75), attempt.EndsAt);
    }

    [Fact]
    public void Remaining_IsCutShort_ByParentLevelEnd()
    {
        var (attempt, clock) = Started(parentEnd: At(40));

        Assert.Equal(At(40), attempt.EndsAt);
        Assert.Equal(TimeSpan.FromMinutes(10), clock.Remaining(At(30)));
    }

    [Fact]
    public void IsExpired_IsTrue_OnceEndTimeIsReached()
    {
        var (_, clock) = Started();

        Assert.False(clock.IsExpired(At(59)));
        Assert.True(clock.IsExpired(At(60)));
    }

    [Fact]
    public void Submit_EndsTheAttempt()
    {
        var (attempt, clock) = Started();

        clock.Submit(SubmitReason.Candidate, At(50));

        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.Equal(At(50), attempt.SubmittedAt);
        Assert.Throws<DomainRuleException>(() => clock.Pause(At(51)));
    }

    [Fact]
    public void AudioPosition_CountsFromPlay()
    {
        var (_, clock) = Started();
        clock.StartAudio(At(1));

        Assert.Equal(TimeSpan.FromMinutes(2), clock.AudioPosition(At(3)));
    }

    [Fact]
    public void AudioPosition_StopsWhilePaused_AndExcludesThePause()
    {
        var (_, clock) = Started();
        clock.StartAudio(At(1));
        clock.Pause(At(3));

        Assert.Equal(TimeSpan.FromMinutes(2), clock.AudioPosition(At(5)));

        clock.Resume(At(6));
        Assert.Equal(TimeSpan.FromMinutes(3), clock.AudioPosition(At(7)));
    }

    [Fact]
    public void RewindAudio_MovesPositionBack()
    {
        var (_, clock) = Started();
        clock.StartAudio(At(1));

        clock.RewindAudio(TimeSpan.FromMinutes(1), At(4));

        Assert.Equal(TimeSpan.FromMinutes(2), clock.AudioPosition(At(4)));
    }

    [Fact]
    public void RewindAudio_Throws_WhenRewindingMoreThanPlayed()
    {
        var (_, clock) = Started();
        clock.StartAudio(At(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RewindAudio(TimeSpan.FromMinutes(5), At(3)));
    }

    [Fact]
    public void RewindAudio_Throws_WhilePaused()
    {
        var (_, clock) = Started();
        clock.StartAudio(At(1));
        clock.Pause(At(3));

        Assert.Throws<DomainRuleException>(() => clock.RewindAudio(TimeSpan.FromMinutes(1), At(4)));
    }
}
