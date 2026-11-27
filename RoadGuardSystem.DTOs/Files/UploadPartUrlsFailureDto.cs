namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadPartUrlsFailureDto(Guid UploadId, IReadOnlyList<int>? PartNumbers,
    DateTimeOffset? ExpiresAt, DateTimeOffset ServerTime, bool RenewalAllowed, string SessionStatus, string Reason);
