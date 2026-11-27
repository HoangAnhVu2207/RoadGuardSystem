using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadPartUrlsPersistenceResult(UploadPersistenceStatus Status, IReadOnlyList<PresignedUploadPart> Parts, UploadPartUrlsFailure? Failure = null);

public sealed record UploadPartUrlsFailure(Guid UploadId, IReadOnlyList<int>? PartNumbers,
    DateTimeOffset? ExpiresAt, DateTimeOffset ServerTime, bool RenewalAllowed, string SessionStatus, string Reason);
