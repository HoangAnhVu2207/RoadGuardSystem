namespace RoadGuardSystem.BusinessObjects.Messaging;

public static class NotificationRegisteredTypes
{
    public const string RegistryVersion = "huy-final.notification-registry.v1";
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        "field.task.assigned.v1", "field.task.submitted.v1", "field.task.supplement_requested.v1", "field.task.lifecycle.v1",
        "repair.work.assigned.v1", "repair.work.submitted.v1", "repair.work.rework_requested.v1", "repair.work.fast_track_confirmed.v1",
        "repair.decision.corrected.v1", "review.supervisor_required.v1", "deadline.breached.v1", "safety.warning.v1",
        "safety.measure_assigned.v1",
        "safety.inspection_due.v1", "review.weekly_pending.v1", "project.obligation_transferred.v1", "project.handling_renewed.v1"
    }.Order(StringComparer.Ordinal).ToArray());
    public static bool Owns(string messageType) => All.Contains(messageType, StringComparer.Ordinal);
    public static bool IsOwnedUnregistered(string messageType)
        => !Owns(messageType) && (messageType.StartsWith("field.task.", StringComparison.Ordinal) ||
            messageType.StartsWith("repair.work.", StringComparison.Ordinal) || messageType.StartsWith("repair.decision.", StringComparison.Ordinal) ||
            messageType.StartsWith("review.supervisor_required.", StringComparison.Ordinal) || messageType.StartsWith("review.weekly_pending.", StringComparison.Ordinal) ||
            messageType.StartsWith("deadline.", StringComparison.Ordinal) || messageType.StartsWith("safety.", StringComparison.Ordinal) ||
            messageType.StartsWith("project.obligation_transferred.", StringComparison.Ordinal) || messageType.StartsWith("project.handling_renewed.", StringComparison.Ordinal));
}
