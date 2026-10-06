using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6DeadlineSourceProofTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    [Fact]
    public void ActualObservedBreachPinsItsOwnStableOriginAndRevision()
    {
        var clock = Clock(); var breach = Guid.NewGuid(); clock.ObserveBreach(breach, At.AddHours(25));
        Assert.True(NotificationDeadlineSourceProof.Verify(new(clock.ProjectId, clock.Id, breach, breach, At.AddHours(25)), clock));
    }
    [Fact]
    public void EarlierBreachRemainsGenuineAfterDeadlineExtensionAndCompletion()
    {
        var clock = Clock(); var breach = Guid.NewGuid(); clock.ObserveBreach(breach, At.AddHours(25));
        // Fixture records domain history only; no per-clock extension command authority is granted.
        clock.Extend(Guid.NewGuid(), Guid.NewGuid(), At.AddHours(60), "fixture history", At.AddHours(26));
        clock.Complete(At.AddHours(30));
        Assert.True(NotificationDeadlineSourceProof.Verify(new(clock.ProjectId, clock.Id, breach, breach, At.AddHours(25)), clock));
    }
    [Fact]
    public void ElapsedDeadlineAloneCannotManufactureAnObservedBreach()
    {
        var clock = Clock(); var invented = Guid.NewGuid();
        Assert.True(clock.IsOverdueAt(At.AddHours(25)));
        Assert.False(NotificationDeadlineSourceProof.Verify(new(clock.ProjectId, clock.Id, invented, invented, At.AddHours(25)), clock));
    }
    [Fact]
    public void BreachCannotBeRelabeledToAnotherScopeRevisionOrObservationTime()
    {
        var clock = Clock(); var breach = Guid.NewGuid(); clock.ObserveBreach(breach, At.AddHours(25));
        var claim = new NotificationDeadlineSourceClaim(clock.ProjectId, clock.Id, breach, breach, At.AddHours(25));
        Assert.False(NotificationDeadlineSourceProof.Verify(claim with { ProjectId = Guid.NewGuid() }, clock));
        Assert.False(NotificationDeadlineSourceProof.Verify(claim with { RevisionId = Guid.NewGuid() }, clock));
        Assert.False(NotificationDeadlineSourceProof.Verify(claim with { OccurredAtUtc = At.AddHours(26) }, clock));
    }
    private static DeadlineClock Clock() => DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), DeadlineClockKind.ProjectManagerReview,
        Guid.NewGuid(), Guid.NewGuid(), At);
}
