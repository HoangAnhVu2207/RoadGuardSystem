using RoadGuardSystem.DTOs.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record DatasetCoverageServiceResult(SurveyV2ServiceStatus Status, DatasetCoverageResponseDto? Coverage = null);
