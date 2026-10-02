namespace RoadGuardSystem.DTOs.Messaging;

public sealed record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    string? NextCursor,
    DateTimeOffset AsOf);
