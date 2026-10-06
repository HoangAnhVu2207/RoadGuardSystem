using RoadGuardSystem.BusinessObjects.Clocks;

namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed record NotificationDeadlineSourceClaim(Guid ProjectId, Guid ClockId, Guid OriginEventId,
    Guid RevisionId, DateTimeOffset OccurredAtUtc);

// Admission pins an immutable observed breach, not current wall-clock lateness or a receipt flag.
public static class NotificationDeadlineSourceProof
{
    public static bool Verify(NotificationDeadlineSourceClaim claim, DeadlineClock clock)
    {
        ArgumentNullException.ThrowIfNull(claim); ArgumentNullException.ThrowIfNull(clock);
        if (claim.ProjectId == Guid.Empty || claim.ProjectId != clock.ProjectId || claim.ClockId != clock.Id ||
            claim.OriginEventId == Guid.Empty || claim.OriginEventId != claim.RevisionId ||
            claim.OccurredAtUtc == default || claim.OccurredAtUtc.Offset != TimeSpan.Zero ||
            clock.Breaches.Select(row => row.Id).Distinct().Count() != clock.Breaches.Count) return false;
        var breach = clock.Breaches.SingleOrDefault(row => row.Id == claim.OriginEventId);
        if (breach is null || breach.ClockId != clock.Id || breach.ObservedAt != claim.OccurredAtUtc ||
            breach.ObservedAt < breach.DueAt || breach.ObservedAt < clock.OriginAt) return false;
        return breach.DueAt == clock.OriginalDueAt || breach.DueAt == clock.CurrentDueAt ||
            clock.Extensions.Any(row => row.ClockId == clock.Id && (row.PreviousDueAt == breach.DueAt || row.NewDueAt == breach.DueAt));
    }
}
