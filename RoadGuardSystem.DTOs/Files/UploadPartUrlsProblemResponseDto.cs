namespace RoadGuardSystem.DTOs.Files;

/// <summary>409 part-URL ProblemDetails. Receipt fields are present only for an authorized,
/// matching expired receipt. An idempotency conflict contains no receipt metadata.</summary>
public sealed record UploadPartUrlsProblemResponseDto(string? Type, string Title, int Status,
    string Detail, string Instance, string Code, string CorrelationId, Guid? UploadId,
    IReadOnlyList<int>? PartNumbers, DateTimeOffset? ExpiresAt, DateTimeOffset? ServerTime,
    bool? RenewalAllowed, string? SessionStatus, string? Reason);
