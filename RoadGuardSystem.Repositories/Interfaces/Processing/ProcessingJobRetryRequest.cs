namespace RoadGuardSystem.Repositories.Processing;

public sealed record ProcessingJobRetryRequest(Guid ActorUserId, Guid JobId, string Reason, string ExpectedVersion, string IdempotencyKey, string RequestFingerprint, Guid? CorrelationId);
