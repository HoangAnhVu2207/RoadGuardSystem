namespace RoadGuardSystem.Repositories.Idempotency;

public enum IdempotencyOperationStatus
{
    Executed = 1,
    Replayed = 2,
    Conflict = 3
}

public sealed record IdempotencyOperationResult(
    IdempotencyOperationStatus Status,
    Guid OperationId,
    string OutcomeJson,
    Guid? ActorUserId,
    Guid? ProjectId,
    string Operation);
