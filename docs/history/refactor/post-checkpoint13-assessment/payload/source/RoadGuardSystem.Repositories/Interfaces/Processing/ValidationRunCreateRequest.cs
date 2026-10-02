namespace RoadGuardSystem.Repositories.Processing;

public sealed record ValidationRunCreateRequest(Guid ActorUserId, Guid ProjectId, Guid ModelVersionId, string DatasetSplitId, string MeasurementType, string Unit, string PairsJson, string IdempotencyKey, string RequestFingerprint, Guid? CorrelationId);
