namespace RoadGuardSystem.Repositories.Files;

public interface IUploadRepository
{
    Task RecoverMultipartsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task<bool> IsCurrentSurveyOperatorAsync(Guid actorUserId, Guid projectId, Guid taskId, bool forUpload, CancellationToken cancellationToken = default);
    Task<UploadMutationPersistenceResult> CreateAsync(UploadCreatePersistenceRequest request, CancellationToken cancellationToken = default);
    Task<UploadSessionPersistenceView?> GetSessionAsync(Guid uploadId, CancellationToken cancellationToken = default);
    Task<UploadPartUrlsPersistenceResult> GetPartUrlsAsync(Guid actorUserId, Guid? projectId, Guid uploadId, IReadOnlyList<int> partNumbers, string idempotencyKey, string requestFingerprint, DateTimeOffset now, DateTimeOffset urlExpiresAt, CancellationToken cancellationToken = default);
    Task<UploadMutationPersistenceResult> CompleteAsync(UploadCompletePersistenceRequest request, CancellationToken cancellationToken = default);
    Task<FileMetadataPersistenceView?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Stream> OpenFileAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<UploadPersistenceStatus> VerifyNextAsync(CancellationToken cancellationToken = default);
}
