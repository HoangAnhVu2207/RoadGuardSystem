namespace RoadGuardSystem.Repositories.Messaging;

public sealed record NotificationReadView(
    Guid Id,
    string Message,
    Guid ResourceId,
    bool Read,
    DateTimeOffset OccurredAtUtc,
    byte[] RowVersion);
