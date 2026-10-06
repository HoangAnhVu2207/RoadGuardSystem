namespace RoadGuardSystem.BusinessObjects.Messaging;

public enum NotificationCalendarDecision : byte { NotDue = 1, PendingPolicy = 2 }

public static class NotificationCalendarPolicy
{
    public static DateTimeOffset NextWeeklyReview(DateTimeOffset after)
    {
        NotificationDomainGuard.Timestamp(after);
        return Clocks.DeadlineClock.NextWeeklyReviewDigest(after);
    }
    public static NotificationCalendarDecision EvaluateMissedPeriod(DateTimeOffset scheduledAtUtc, DateTimeOffset observedAtUtc)
    {
        ValidateWeeklyPeriod(scheduledAtUtc); NotificationDomainGuard.Timestamp(observedAtUtc);
        // The calendar is confirmed; catch-up/skip authority is not. This result does not activate delivery.
        return observedAtUtc.ToUniversalTime() < scheduledAtUtc.ToUniversalTime()
            ? NotificationCalendarDecision.NotDue : NotificationCalendarDecision.PendingPolicy;
    }
    internal static void ValidateWeeklyPeriod(DateTimeOffset scheduledAtUtc)
    {
        NotificationDomainGuard.Timestamp(scheduledAtUtc);
        var local = scheduledAtUtc.ToOffset(TimeSpan.FromHours(7));
        if (local.DayOfWeek != DayOfWeek.Monday || local.TimeOfDay != TimeSpan.FromHours(9))
            throw new ArgumentException("The weekly period must be Monday 09:00 Asia/Ho_Chi_Minh.", nameof(scheduledAtUtc));
    }
}
