namespace RoadGuardSystem.BusinessObjects.Messaging;

public enum NotificationCalendarDecision : byte { NotDue = 1, PendingPolicy = 2, RecoverLatest = 3 }

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
        // Owner Anh 07/10/2026: latest missed period only; production recovery captures pending work atomically.
        return observedAtUtc.ToUniversalTime() < scheduledAtUtc.ToUniversalTime()
            ? NotificationCalendarDecision.NotDue : NotificationCalendarDecision.RecoverLatest;
    }
    internal static void ValidateWeeklyPeriod(DateTimeOffset scheduledAtUtc)
    {
        NotificationDomainGuard.Timestamp(scheduledAtUtc);
        var local = scheduledAtUtc.ToOffset(TimeSpan.FromHours(7));
        if (local.DayOfWeek != DayOfWeek.Monday || local.TimeOfDay != TimeSpan.FromHours(9))
            throw new ArgumentException("The weekly period must be Monday 09:00 Asia/Ho_Chi_Minh.", nameof(scheduledAtUtc));
    }
}
