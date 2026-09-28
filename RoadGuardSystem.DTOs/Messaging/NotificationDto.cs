namespace RoadGuardSystem.DTOs.Messaging;

public sealed record NotificationDto(
    Guid Id,
    string Message,
    Guid? ResourceId,
    bool Read,
    DateTimeOffset OccurredAt,
    string Version);
