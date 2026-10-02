namespace RoadGuardSystem.Repositories.Messaging;

public sealed record NotificationMarkReadPersistenceResult(
    NotificationMarkReadPersistenceStatus Status,
    NotificationReadView? Notification = null);
