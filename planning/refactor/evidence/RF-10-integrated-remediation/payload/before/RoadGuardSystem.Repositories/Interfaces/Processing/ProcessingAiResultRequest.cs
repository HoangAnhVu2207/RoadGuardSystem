namespace RoadGuardSystem.Repositories.Processing;

public sealed record ProcessingAiResultRequest(Guid JobId, Guid AttemptId, string ManifestHash, Guid ModelVersionId, string Mode, Guid RawResultFileId, string ChecksumSha256, string DetectionsJson, string IdempotencyKey);
