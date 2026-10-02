namespace RoadGuardSystem.Repositories.Storage;

public sealed record PresignedUploadPart(int PartNumber, string Url, DateTimeOffset ExpiresAt);
