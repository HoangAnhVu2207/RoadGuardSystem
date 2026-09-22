namespace RoadGuardSystem.Services.Surveys;

public enum SurveyPlanningServiceStatus
{
    Success = 1,
    Replayed = 2,
    Forbidden = 3,
    InvalidInput = 4,
    NotFound = 5,
    Conflict = 6,
    IdempotentConflict = 7
}
