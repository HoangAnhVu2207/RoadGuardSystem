namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadCancelResponseDto(Guid Id, string Status, string FailureCode);
