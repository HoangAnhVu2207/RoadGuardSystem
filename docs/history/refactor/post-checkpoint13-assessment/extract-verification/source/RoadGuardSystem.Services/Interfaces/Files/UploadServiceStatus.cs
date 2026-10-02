namespace RoadGuardSystem.Services.Files;

public enum UploadServiceStatus
{
    Success = 1,
    Replayed = 2,
    NotFound = 3,
    Forbidden = 4,
    Conflict = 5,
    PreconditionFailed = 6,
    InvalidInput = 7,
    StorageUnavailable = 8
}
