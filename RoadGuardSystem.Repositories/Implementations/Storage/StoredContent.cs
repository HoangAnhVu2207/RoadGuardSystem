namespace RoadGuardSystem.Repositories.Storage;

public sealed record StoredContent(
    string StorageUri,
    string OriginalName,
    string MimeType,
    int SizeBytes,
    string Checksum);
