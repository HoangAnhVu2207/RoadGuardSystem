using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

public interface ISurveyV2Repository
{
    Task<SurveyV2PlanPersistenceResult> CreatePlanAsync(SurveyV2PlanCreationRequest request, CancellationToken cancellationToken = default);
    Task<SurveyV2PlanPersistenceResult> PostponePlanAsync(SurveyV2PlanPostponementRequest request, CancellationToken cancellationToken = default);
    Task<SurveyV2TaskPersistenceResult> CreateTaskAsync(SurveyV2TaskCreationRequest request, CancellationToken cancellationToken = default);
    Task<SurveyV2TaskPersistenceView?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<SurveyV2TaskPagePersistenceResult> ListMyTasksAsync(Guid operatorUserId, string? cursor, int limit, CancellationToken cancellationToken = default);
    Task<SurveyV2TaskPersistenceResult> MutateTaskAsync(SurveyV2TaskMutationRequest request, CancellationToken cancellationToken = default);
    Task<Guid?> GetPlanProjectIdAsync(Guid planId, CancellationToken cancellationToken = default);
}
