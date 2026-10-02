namespace RoadGuardSystem.Repositories.Files;

public sealed record UploadSessionPersistenceView(
    Guid Id,
    Guid FileId,
    Guid OwnerUserId,
    Guid ProjectId,
    string ObjectKey,
    string Status,
    int PartSizeBytes,
    DateTimeOffset ExpiresAt,
    string Version,
    string? Purpose = null,
    Guid? TargetId = null);
