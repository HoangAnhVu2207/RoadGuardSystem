using RoadGuardSystem.BusinessObjects.Clocks;
using Xunit;

namespace RoadGuardSystem.UnitTests.Clocks;

public sealed class HuyFinalDeadlineClockTests
{
    private static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-10-06T01:00:00+07:00");

    [Fact]
    public void UnknownOriginOrKind_CannotAdmitClock()
    {
        Assert.Throws<ArgumentException>(() => DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), DeadlineClockKind.FastTrackExecution, Guid.NewGuid(), Guid.Empty, Origin));
        Assert.Throws<ArgumentException>(() => DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), DeadlineClockKind.FastTrackExecution, Guid.NewGuid(), Guid.NewGuid(), default));
        Assert.Throws<ArgumentOutOfRangeException>(() => DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), (DeadlineClockKind)99, Guid.NewGuid(), Guid.NewGuid(), Origin));
    }

    [Fact]
    public void DangerAcknowledgment_RecordsActorAndEventWithoutAnyWorkflowApproval()
    {
        var clock = New(DeadlineClockKind.DangerAcknowledgment);
        Assert.Throws<InvalidOperationException>(() => clock.Complete(Origin.AddMinutes(20)));
        Assert.Null(clock.CompletedAt); Assert.Null(clock.AcknowledgedAt);
        var actor = Guid.NewGuid(); var eventId = Guid.NewGuid();
        clock.Acknowledge(eventId, actor, Origin.AddMinutes(20));
        Assert.Equal(actor, clock.AcknowledgedByUserId);
        Assert.Equal(eventId, clock.AcknowledgmentEventId);
        Assert.Equal(Origin.AddMinutes(20).ToUniversalTime(), clock.AcknowledgedAt);
        Assert.Equal(clock.AcknowledgedAt, clock.CompletedAt);
        Assert.Empty(clock.Breaches);
        clock.Acknowledge(eventId, actor, Origin.AddMinutes(20));
        Assert.Throws<InvalidOperationException>(() => clock.Acknowledge(Guid.NewGuid(), actor, Origin.AddMinutes(21)));
        Assert.Throws<InvalidOperationException>(() => New().Acknowledge(eventId, actor, Origin));
    }

    [Theory]
    [InlineData(DeadlineClockKind.FastTrackExecution, 24)]
    [InlineData(DeadlineClockKind.DeviceHandover, 24)]
    [InlineData(DeadlineClockKind.FinishedDataSync, 24)]
    [InlineData(DeadlineClockKind.ProjectManagerReview, 24)]
    [InlineData(DeadlineClockKind.SupervisorInitialApproval, 48)]
    [InlineData(DeadlineClockKind.SupervisorFinalConfirmation, 48)]
    [InlineData(DeadlineClockKind.CrewSupplement, 48)]
    [InlineData(DeadlineClockKind.SupervisorEscalation, 24)]
    [InlineData(DeadlineClockKind.DangerAcknowledgment, 1)]
    [InlineData(DeadlineClockKind.FirstSafetyCheck, 24)]
    public void AssignedClock_UsesElapsedUtcAndExactDueBoundary(DeadlineClockKind kind, int hours)
    {
        var clock = New(kind);
        Assert.Equal(Origin.ToUniversalTime(), clock.OriginAt);
        Assert.Equal(Origin.AddHours(hours).ToUniversalTime(), clock.OriginalDueAt);
        Assert.False(clock.IsOverdueAt(clock.OriginalDueAt.AddTicks(-1)));
        Assert.True(clock.IsOverdueAt(clock.OriginalDueAt));
    }

    [Fact]
    public void Extension_PreservesOriginOriginalDueAndPriorBreach()
    {
        var clock = New();
        var original = clock.OriginalDueAt;
        var actor = Guid.NewGuid();
        var history = clock.Extend(Guid.NewGuid(), actor, original.AddHours(2), "Recipient absent", original.AddHours(1));
        Assert.Equal(Origin.ToUniversalTime(), clock.OriginAt);
        Assert.Equal(original, clock.OriginalDueAt);
        Assert.Equal(original, history.PreviousDueAt);
        Assert.Equal(actor, history.ActorUserId);
        Assert.True(history.PreviousDeadlineBreached);
        Assert.Single(clock.Breaches);
        Assert.False(clock.IsOverdueAt(original.AddHours(1)));
        Assert.True(clock.IsOverdueAt(original.AddHours(2)));
        Assert.False(clock.ObserveBreach(Guid.NewGuid(), original.AddHours(1)));
        Assert.True(clock.ObserveBreach(Guid.NewGuid(), original.AddHours(2)));
        Assert.Equal(2, clock.Breaches.Count);
    }

    [Fact]
    public void CompletedLate_BreachSurvivesAndClockCannotExtendOrReopen()
    {
        var clock = New();
        clock.Complete(clock.OriginalDueAt.AddMinutes(1));
        Assert.Single(clock.Breaches);
        Assert.False(clock.IsOverdueAt(clock.OriginalDueAt.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => clock.Extend(Guid.NewGuid(), Guid.NewGuid(), clock.OriginalDueAt.AddDays(1), "Late extension", clock.OriginalDueAt.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() => clock.Complete(clock.OriginalDueAt.AddMinutes(2)));
    }

    [Fact]
    public void ExtensionBeforeDue_DoesNotInventBreachOrAcceptMissingReason()
    {
        var clock = New();
        Assert.Throws<ArgumentException>(() => clock.Extend(Guid.NewGuid(), Guid.NewGuid(), clock.OriginalDueAt.AddHours(1), " ", Origin));
        Assert.Empty(clock.Extensions);
        Assert.Empty(clock.Breaches);
        var change = clock.Extend(Guid.NewGuid(), Guid.NewGuid(), clock.OriginalDueAt.AddHours(1), "Review handover", Origin.AddHours(1));
        Assert.False(change.PreviousDeadlineBreached);
        Assert.Empty(clock.Breaches);
    }

    [Theory]
    [InlineData("2026-10-04T23:00:00Z", "2026-10-05T02:00:00Z")]
    [InlineData("2026-10-05T02:00:00Z", "2026-10-12T02:00:00Z")]
    [InlineData("2026-10-05T02:00:01Z", "2026-10-12T02:00:00Z")]
    public void WeeklyReviewDigest_NextMondayNineVietnamIsUtcTwo(string now, string expected)
        => Assert.Equal(DateTimeOffset.Parse(expected), DeadlineClock.NextWeeklyReviewDigest(DateTimeOffset.Parse(now)));

    private static DeadlineClock New(DeadlineClockKind kind = DeadlineClockKind.ProjectManagerReview)
        => DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), kind, Guid.NewGuid(), Guid.NewGuid(), Origin);
}
