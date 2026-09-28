namespace RoadGuardSystem.Repositories.Messaging;

public enum NotificationMarkReadPersistenceStatus
{
    Success = 1,
    Replayed = 2,
    NotFound = 3,
    StaleConcurrency = 4,
    IdempotentConflict = 5
}
