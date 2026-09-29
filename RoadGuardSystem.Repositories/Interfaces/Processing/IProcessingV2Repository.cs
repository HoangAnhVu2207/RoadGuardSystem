namespace RoadGuardSystem.Repositories.Processing;

public interface IProcessingV2Repository
{
    Task<Guid?> GetDatasetProjectIdAsync(Guid datasetId, CancellationToken cancellationToken = default);
    Task<ProcessingJobPersistenceResult> CreateAsync(ProcessingJobCreateRequest request, CancellationToken cancellationToken = default);
    Task<ProcessingJobPersistenceView?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ProcessingJobPersistenceResult> RetryAsync(ProcessingJobRetryRequest request, CancellationToken cancellationToken = default);
    Task<ProcessingJobPersistenceResult> ReceiveResultAsync(ProcessingAiResultRequest request, CancellationToken cancellationToken = default);
    Task<ValidationRunPersistenceResult> CreateValidationAsync(ValidationRunCreateRequest request, CancellationToken cancellationToken = default);
    Task<ValidationRunPersistenceView?> GetValidationAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<bool> CompleteNextValidationRunAsync(CancellationToken cancellationToken = default);
}
