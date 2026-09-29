namespace RoadGuardSystem.Repositories.Processing;

public sealed record ProcessingJobPersistenceResult(ProcessingJobPersistenceStatus Status, ProcessingJobPersistenceView? Job = null);
