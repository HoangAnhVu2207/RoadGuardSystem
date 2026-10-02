namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadMutationPersistenceResult(UploadPersistenceStatus Status, UploadSessionPersistenceView? Session);
