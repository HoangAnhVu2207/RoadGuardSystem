using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus Status, SurveyV2PlanPersistenceView? Plan = null, Guid? ExistingPlanId = null, string? ExistingPlanStatus = null, string? ReasonCode = null, SurveyScheduleError? ScheduleError = null);
