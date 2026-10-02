namespace RoadGuardSystem.Repositories.Processing;

public sealed record ValidationRunPersistenceResult(ProcessingJobPersistenceStatus Status, ValidationRunPersistenceView? Run = null);
