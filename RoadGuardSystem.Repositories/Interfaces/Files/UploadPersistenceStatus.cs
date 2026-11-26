namespace RoadGuardSystem.Repositories.Files;

public enum UploadPersistenceStatus
{
    Success = 1,
    Replayed = 2,
    NotFound = 3,
    Conflict = 4,
    ConcurrencyConflict = 5,
    InvalidInput = 6,
    StorageUnavailable = 7,
    Forbidden = 8,
    PartUrlsExpired = 9,
    PartUrlsIdempotencyConflict = 10,
    PartUrlsSessionUnavailable = 11,
    PartUrlsMultipartConflict = 12
}
