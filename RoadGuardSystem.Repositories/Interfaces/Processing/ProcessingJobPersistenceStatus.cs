namespace RoadGuardSystem.Repositories.Processing;

public enum ProcessingJobPersistenceStatus { Success = 1, Replayed = 2, InvalidInput = 3, NotFound = 4, Conflict = 5, IdempotentConflict = 6, ConcurrencyConflict = 7 }
