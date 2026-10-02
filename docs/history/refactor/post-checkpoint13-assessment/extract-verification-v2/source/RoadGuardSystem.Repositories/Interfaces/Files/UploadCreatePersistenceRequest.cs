namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadCreatePersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid? TargetId,
    string Purpose,
    string FileName,
    string MediaType,
    long SizeBytes,
    string ChecksumSha256,
    int PartSizeBytes,
    DateTimeOffset ExpiresAt,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId);
