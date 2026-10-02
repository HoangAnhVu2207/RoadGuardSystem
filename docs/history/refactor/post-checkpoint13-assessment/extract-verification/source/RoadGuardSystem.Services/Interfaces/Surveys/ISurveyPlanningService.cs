using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public interface ISurveyPlanningService
{
    Task<SurveyPlanServiceResult> CreatePlanAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateSurveyPlanCommand command,
        CancellationToken cancellationToken = default);

    Task<SurveyRequestServiceResult> CreateRequestAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateSurveyRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<SurveyPlanPostponementServiceResult> PostponePlanAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        PostponeSurveyPlanCommand command,
        CancellationToken cancellationToken = default);
}
