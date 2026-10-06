using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6CalendarAdmissionProofTests
{
    private static readonly DateTimeOffset Period = new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
    [Fact]
    public void FuturePlannedOccurrenceDoesNotSendEarly()
    {
        Assert.Equal(NotificationCalendarAdmission.NotDue,
            NotificationCalendarAdmissionProof.Evaluate(Guid.NewGuid(), Period.AddDays(-1), Period, Period.AddMinutes(-1), null));
    }
    [Fact]
    public void ActualScheduledCallbackOnTheSameProvenContinuousRunCanAdmitThePlannedOccurrence()
    {
        var run = Guid.NewGuid(); var observed = Period.AddMilliseconds(10);
        Assert.Equal(NotificationCalendarAdmission.Ready, NotificationCalendarAdmissionProof.Evaluate(run, Period.AddDays(-1), Period,
            observed, new(run, Period, observed, true)));
    }
    [Fact]
    public void LateCallbackWithinTheSameWeekStillRequiresMissedPeriodPolicy()
    {
        var run = Guid.NewGuid(); var observed = Period.AddMinutes(2);
        Assert.Equal(NotificationCalendarAdmission.PendingCatchupPolicy,
            NotificationCalendarAdmissionProof.Evaluate(run, Period.AddDays(-1), Period, observed,
                new(run, Period, observed, true)));
    }
    [Theory]
    [InlineData("RESTART")]
    [InlineData("POLL")]
    [InlineData("GAP")]
    [InlineData("WRONG_PERIOD")]
    [InlineData("MULTIPLE_PERIODS")]
    public void UncertainOrMissedPeriodCannotBecomeCatchupByTechnicalRetry(string reason)
    {
        var run = Guid.NewGuid(); var observed = reason == "MULTIPLE_PERIODS" ? Period.AddDays(8) : Period.AddMinutes(1);
        var witness = reason == "POLL" ? null : new NotificationScheduledCallbackWitness(reason == "RESTART" ? Guid.NewGuid() : run,
            reason == "WRONG_PERIOD" ? Period.AddDays(7) : Period, observed, reason != "GAP");
        Assert.Equal(NotificationCalendarAdmission.PendingCatchupPolicy,
            NotificationCalendarAdmissionProof.Evaluate(run, Period.AddDays(-1), Period, observed, witness));
    }
}
