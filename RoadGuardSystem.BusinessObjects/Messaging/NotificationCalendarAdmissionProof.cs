namespace RoadGuardSystem.BusinessObjects.Messaging;

public enum NotificationCalendarAdmission : byte { NotDue = 1, Ready = 2, PendingCatchupPolicy = 3 }
// Server scheduler provenance only. This type is not accepted from an HTTP/offline client.
public sealed record NotificationScheduledCallbackWitness(Guid SchedulerRunId, DateTimeOffset ScheduledAtUtc,
    DateTimeOffset ObservedAtUtc, bool ContinuousRunVerified);
public static class NotificationCalendarAdmissionProof
{
    public static NotificationCalendarAdmission Evaluate(Guid plannedRunId, DateTimeOffset plannedAtUtc,
        DateTimeOffset scheduledAtUtc, DateTimeOffset observedAtUtc, NotificationScheduledCallbackWitness? witness)
    {
        if (plannedRunId == Guid.Empty || plannedAtUtc.Offset != TimeSpan.Zero ||
            scheduledAtUtc.Offset != TimeSpan.Zero || observedAtUtc.Offset != TimeSpan.Zero ||
            plannedAtUtc >= scheduledAtUtc)
            return NotificationCalendarAdmission.PendingCatchupPolicy;
        if (observedAtUtc < scheduledAtUtc) return NotificationCalendarAdmission.NotDue;
        if (witness is null || !witness.ContinuousRunVerified || witness.SchedulerRunId != plannedRunId ||
            witness.ScheduledAtUtc != scheduledAtUtc || witness.ObservedAtUtc != observedAtUtc ||
            observedAtUtc >= scheduledAtUtc.AddMinutes(1))
            return NotificationCalendarAdmission.PendingCatchupPolicy;
        return NotificationCalendarAdmission.Ready;
    }
}
