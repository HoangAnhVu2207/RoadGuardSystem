namespace RoadGuardSystem.Repositories.Messaging;

public sealed record NotificationReadView(
    Guid Id,
    string Message,
    Guid ResourceId,
    bool Read,
    DateTimeOffset OccurredAtUtc,
    byte[] RowVersion)
{
    public NotificationPriorityFact? Priority { get; init; }
}

public sealed record NotificationPriorityFact(string Value, string ReasonCode, DateTimeOffset EvaluatedAt)
{
    public const string Policy = "MANDATORY_DEADLINE_V1";
}
