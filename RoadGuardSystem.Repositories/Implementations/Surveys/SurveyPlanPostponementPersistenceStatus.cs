namespace RoadGuardSystem.Repositories.Surveys;

public enum SurveyPlanPostponementPersistenceStatus
{
    Success = 1,
    Replayed = 2,
    InvalidInput = 3,
    NotFound = 4,
    ScopeConflict = 5,
    Conflict = 6,
    IdempotentConflict = 7
}
