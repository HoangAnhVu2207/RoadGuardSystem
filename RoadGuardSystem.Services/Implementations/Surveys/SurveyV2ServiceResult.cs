using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record SurveyV2ServiceResult(
    SurveyV2ServiceStatus Status,
    SurveyPlanV2ResponseDto? Plan = null,
    SurveyTaskV2ResponseDto? Task = null,
    DatasetResponseDto? Dataset = null, Guid? ExistingPlanId = null, string? ExistingPlanStatus = null, string? ReasonCode = null, SurveyScheduleError? ScheduleError = null);
