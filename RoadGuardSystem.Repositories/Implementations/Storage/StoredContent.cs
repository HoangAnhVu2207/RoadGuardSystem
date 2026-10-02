namespace RoadGuardSystem.Repositories.Storage;

public sealed record StoredContent(
    string StorageUri,
    string OriginalName,
    string MimeType,
    long SizeBytes,
    string Checksum);
