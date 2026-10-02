using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadPartUrlsPersistenceResult(UploadPersistenceStatus Status, IReadOnlyList<PresignedUploadPart> Parts);
