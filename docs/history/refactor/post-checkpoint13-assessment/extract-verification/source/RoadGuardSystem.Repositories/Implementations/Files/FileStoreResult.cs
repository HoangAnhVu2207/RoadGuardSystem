namespace RoadGuardSystem.Repositories.Files;

public enum FileStoreStatus
{
    Stored = 1,
    Replayed = 2,
    Conflict = 3,
    OwnerNotFound = 4
}

public sealed record FileStoreResult(
    FileStoreStatus Status,
    Guid? FileId = null,
    string? StorageUri = null,
    string? MimeType = null,
    int? SizeBytes = null,
    string? Checksum = null,
    string? ErrorCode = null);
