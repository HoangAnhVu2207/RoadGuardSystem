namespace RoadGuardSystem.Repositories.Surveys;

public interface ISurveyPlanningRepository
{
    Task<SurveyPlanPersistenceResult> CreatePlanAsync(
        SurveyPlanCreationPersistenceRequest request,
        CancellationToken cancellationToken = default);

    Task<SurveyRequestPersistenceResult> CreateRequestAsync(
        SurveyRequestCreationPersistenceRequest request,
        CancellationToken cancellationToken = default);

    Task<SurveyPlanPostponementPersistenceResult> PostponePlanAsync(
        SurveyPlanPostponementPersistenceRequest request,
        CancellationToken cancellationToken = default);
}
