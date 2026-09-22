namespace RoadGuardSystem.Repositories.Defects;

public enum DetectionReviewPersistenceStatus
{
    Executed = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    InvalidInput = 4,
    DuplicateRetainedDetection = 5,
    InvalidReference = 6
}
