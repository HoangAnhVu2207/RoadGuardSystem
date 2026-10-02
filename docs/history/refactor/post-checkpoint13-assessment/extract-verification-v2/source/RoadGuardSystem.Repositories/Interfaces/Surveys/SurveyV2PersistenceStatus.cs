namespace RoadGuardSystem.Repositories.Surveys;

public enum SurveyV2PersistenceStatus { Success = 1, Replayed = 2, NotFound = 3, Conflict = 4, IdempotentConflict = 5, InvalidInput = 6, ConcurrencyConflict = 7, OperatorNotFound = 8 }
