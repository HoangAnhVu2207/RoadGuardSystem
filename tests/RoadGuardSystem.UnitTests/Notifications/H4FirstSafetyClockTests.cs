using RoadGuardSystem.BusinessObjects.Clocks;
using Xunit;
namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H4FirstSafetyClockTests
{
    [Fact]
    public void EarlierScheduleKeepsElapsedUtcOriginAndOriginalDueAfterExtension()
    {
        var origin = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.FromHours(7));
        var first = DeadlineClock.CreateFirstSafetyCheck(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), origin, origin.AddHours(2));
        Assert.Equal(origin.ToUniversalTime(), first.OriginAt); Assert.Equal(origin.AddHours(2).ToUniversalTime(), first.OriginalDueAt);
        Assert.True(first.ObserveBreach(Guid.NewGuid(), origin.AddHours(3)));
        first.Extend(Guid.NewGuid(), Guid.NewGuid(), origin.AddHours(4), "controlled policy seam fixture only", origin.AddHours(3));
        Assert.Equal(origin.AddHours(2).ToUniversalTime(), first.OriginalDueAt);
        Assert.True(Assert.Single(first.Extensions).PreviousDeadlineBreached);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    public void InvalidFirstCheckTimeCannotCreateClock(int hours)
    {
        var origin = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => DeadlineClock.CreateFirstSafetyCheck(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), origin, origin.AddHours(hours)));
    }
}
