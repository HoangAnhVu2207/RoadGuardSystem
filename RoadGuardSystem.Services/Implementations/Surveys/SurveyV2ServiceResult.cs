using RoadGuardSystem.DTOs.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record SurveyV2ServiceResult(
    SurveyV2ServiceStatus Status,
    SurveyPlanV2ResponseDto? Plan = null,
    SurveyTaskV2ResponseDto? Task = null,
    DatasetResponseDto? Dataset = null);
