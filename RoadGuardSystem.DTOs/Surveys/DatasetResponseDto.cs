namespace RoadGuardSystem.DTOs.Surveys;

public sealed record DatasetResponseDto(
    Guid Id,
    Guid SurveyTaskId,
    Guid DataVersionId,
    string IntegrityStatus,
    string TelemetryStatus,
    string Version);
