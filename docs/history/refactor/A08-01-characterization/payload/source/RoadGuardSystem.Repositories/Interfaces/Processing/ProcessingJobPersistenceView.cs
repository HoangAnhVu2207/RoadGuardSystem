namespace RoadGuardSystem.Repositories.Processing;

public sealed record ProcessingJobPersistenceView(Guid Id, Guid ProjectId, string Status, Guid? ResultId, int AttemptNumber, string Version, string? Error);
