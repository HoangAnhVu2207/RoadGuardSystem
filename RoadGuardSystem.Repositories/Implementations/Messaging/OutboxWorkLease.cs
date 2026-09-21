namespace RoadGuardSystem.Repositories.Messaging;

public sealed record OutboxWorkLease(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAtUtc,
    Guid? CorrelationId,
    string PayloadJson,
    int AttemptCount,
    DateTimeOffset LeaseExpiresAtUtc);
