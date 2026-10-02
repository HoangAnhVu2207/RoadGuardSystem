namespace RoadGuardSystem.Services.Surveys;

public enum SurveyV2ServiceStatus { Success = 1, Replayed = 2, InvalidInput = 3, Forbidden = 4, NotFound = 5, Conflict = 6, IdempotentConflict = 7, ConcurrencyConflict = 8, OperatorNotFound = 9 }
