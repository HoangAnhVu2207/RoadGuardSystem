namespace RoadGuardSystem.DTOs.Processing;

public sealed record ProcessingJobResponseDto(
    Guid Id,
    string Status,
    Guid? ResultId,
    int AttemptNumber,
    string Version,
    string JobType,
    string? Error);
