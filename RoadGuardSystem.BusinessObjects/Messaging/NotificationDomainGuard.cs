namespace RoadGuardSystem.BusinessObjects.Messaging;

internal static class NotificationDomainGuard
{
    internal static void Id(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An explicit non-empty identity is required.", nameof(id));
    }
    internal static void OptionalId(Guid? id) { if (id.HasValue) Id(id.Value); }
    internal static void Timestamp(DateTimeOffset at)
    {
        if (at == default) throw new ArgumentException("An explicit timestamp is required.", nameof(at));
    }
}
