using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadCompletePersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid UploadId,
    string ExpectedVersion,
    IReadOnlyList<CompletedStoragePart> Parts,
    string ChecksumSha256,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId);
