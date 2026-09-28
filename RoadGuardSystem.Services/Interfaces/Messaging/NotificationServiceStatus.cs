namespace RoadGuardSystem.Services.Messaging;

public enum NotificationServiceStatus
{
    Success = 1,
    Replayed = 2,
    InvalidInput = 3,
    NotFound = 4,
    StaleConcurrency = 5,
    IdempotentConflict = 6
}
