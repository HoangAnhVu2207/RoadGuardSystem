using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public interface ISurveyV2Service
{
    Task<SurveyV2ServiceResult> CreatePlanAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyPlanV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<SurveyV2ServiceResult> PostponePlanAsync(Guid actorUserId, UserRoleCode role, Guid planId, PostponeSurveyPlanV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<SurveyV2ServiceResult> CreateTaskAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyTaskV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<SurveyV2ServiceResult> GetTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, CancellationToken cancellationToken = default);
}
