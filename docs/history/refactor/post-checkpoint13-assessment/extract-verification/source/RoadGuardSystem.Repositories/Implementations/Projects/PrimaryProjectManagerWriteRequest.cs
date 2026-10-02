namespace RoadGuardSystem.Repositories.Projects;

public sealed record PrimaryProjectManagerWriteRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid ReplacementProjectManagerUserId,
    DateOnly EffectiveFrom,
    string Reason,
    byte[] ExpectedCurrentMembershipRowVersion,
    Guid OperationId,
    Guid? CorrelationId,
    DateTimeOffset OccurredAt);
