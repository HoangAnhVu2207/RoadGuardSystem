namespace RoadGuardSystem.Repositories.Storage;

public sealed record UploadObjectVerification(long SizeBytes, string ChecksumSha256, string MimeType);
