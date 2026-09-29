using RoadGuardSystem.DTOs.Processing;

namespace RoadGuardSystem.Services.Processing;

public sealed record ProcessingV2ServiceResult(
    ProcessingV2ServiceStatus Status,
    ProcessingJobResponseDto? Job = null,
    ValidationResultDto? Validation = null);
