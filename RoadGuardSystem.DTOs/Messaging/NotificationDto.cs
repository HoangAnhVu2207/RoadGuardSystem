namespace RoadGuardSystem.DTOs.Messaging;

public sealed record NotificationDto(
    Guid Id,
    string Message,
    Guid? ResourceId,
    bool Read,
    DateTimeOffset OccurredAt,
    string Version)
{
    public string? Priority { get; init; }
    public string? PriorityPolicy { get; init; }
    public string? PriorityReasonCode { get; init; }
    public DateTimeOffset? PriorityEvaluatedAt { get; init; }
}
