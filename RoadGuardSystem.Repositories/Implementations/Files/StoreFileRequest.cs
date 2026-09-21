namespace RoadGuardSystem.Repositories.Files;

public sealed record StoreFileRequest(
    Stream Content,
    string OriginalName,
    string DeclaredMimeType,
    string ExpectedChecksum,
    long? DeclaredSizeBytes,
    Guid? UploadedByUserId,
    bool IsSystemOriginated,
    string IdempotencyKey,
    Guid CorrelationId,
    DateOnly? RetentionUntil = null);
