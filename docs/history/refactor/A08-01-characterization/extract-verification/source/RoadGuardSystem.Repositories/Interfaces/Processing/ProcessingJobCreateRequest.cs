namespace RoadGuardSystem.Repositories.Processing;

public sealed record ProcessingJobCreateRequest(Guid ActorUserId, Guid DatasetId, string ScopeJson, Guid ModelVersionId, string PreprocessingVersion, string ConfigVersion, string Mode, string IdempotencyKey, string RequestFingerprint, Guid? CorrelationId);
