namespace RoadGuardSystem.Repositories.Messaging;

public sealed record NotificationPageReadResult(
    IReadOnlyList<NotificationReadView> Items,
    DateTimeOffset? NextOccurredAtUtc,
    Guid? NextId,
    DateTimeOffset AsOf);
