namespace RoadGuardSystem.Services.Processing;

public enum ProcessingV2ServiceStatus { Success = 1, Replayed = 2, InvalidInput = 3, Forbidden = 4, NotFound = 5, Conflict = 6, IdempotentConflict = 7, ConcurrencyConflict = 8 }
