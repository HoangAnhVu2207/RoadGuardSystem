namespace RoadGuardSystem.Repositories.Files;

public enum UploadPersistenceStatus
{
    Success = 1,
    Replayed = 2,
    NotFound = 3,
    Conflict = 4,
    ConcurrencyConflict = 5,
    InvalidInput = 6,
    StorageUnavailable = 7
}
