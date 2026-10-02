namespace RoadGuardSystem.Repositories.Files;

public sealed record FileMetadataPersistenceView(
    Guid Id,
    Guid OwnerUserId,
    Guid ProjectId,
    string ObjectKey,
    string Status,
    string ChecksumSha256,
    string MediaType,
    long SizeBytes,
    string Version);
