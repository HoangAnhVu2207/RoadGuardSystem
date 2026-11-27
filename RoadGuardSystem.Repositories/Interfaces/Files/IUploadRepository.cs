using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Files;

public interface IUploadRepository
{
    Task<bool> IsCurrentOfflineFileActorAsync(Guid actor, RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role, Guid fileId, CancellationToken token = default) => Task.FromResult(false);
    Task<bool> IsCurrentLegacyFieldFileReaderAsync(Guid actorUserId, RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role, Guid projectId, Guid fileId, string purpose, CancellationToken cancellationToken = default) => Task.FromResult(false);
    Task<bool> IsCurrentFieldActorAsync(Guid actorUserId, RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role, Guid projectId, Guid taskId, string purpose, bool forUpload, CancellationToken cancellationToken = default) => Task.FromResult(false);
    Task RecoverMultipartsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task<bool> IsCurrentSurveyOperatorAsync(Guid actorUserId, Guid projectId, Guid taskId, bool forUpload, CancellationToken cancellationToken = default);
    Task<UploadMutationPersistenceResult> CreateAsync(UploadCreatePersistenceRequest request, CancellationToken cancellationToken = default);
    Task<UploadMutationPersistenceResult> CreateOfflineAsync(RoadGuardSystem.Repositories.Offline.OfflineUploadCaptureRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new UploadMutationPersistenceResult(UploadPersistenceStatus.InvalidInput, null));
    Task<UploadSessionPersistenceView?> GetSessionAsync(Guid uploadId, CancellationToken cancellationToken = default);
    Task<UploadPersistenceStatus> CancelReporterEvidenceAsync(Guid actorUserId, Guid uploadId, CancellationToken cancellationToken = default);
    Task<UploadPersistenceStatus> CancelProjectUploadAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, CancellationToken cancellationToken = default);
    Task<UploadPartUrlsPersistenceResult> GetPartUrlsAsync(Guid actorUserId, Guid? projectId, Guid uploadId, IReadOnlyList<int> partNumbers, string idempotencyKey, string requestFingerprint, DateTimeOffset now, DateTimeOffset urlExpiresAt, CancellationToken cancellationToken = default);
    // HTTP producers pin the authenticated role through every receipt/signing checkpoint.
    Task<UploadPartUrlsPersistenceResult> GetPartUrlsAsync(Guid actorUserId, UserRoleCode role, Guid? projectId, Guid uploadId, IReadOnlyList<int> partNumbers, string idempotencyKey, string requestFingerprint, DateTimeOffset now, DateTimeOffset urlExpiresAt, CancellationToken cancellationToken = default)
        => GetPartUrlsAsync(actorUserId, projectId, uploadId, partNumbers, idempotencyKey, requestFingerprint, now, urlExpiresAt, cancellationToken);
    Task<UploadMutationPersistenceResult> CompleteAsync(UploadCompletePersistenceRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsHandoverDocumentFileAsync(Guid project, Guid fileId, CancellationToken token);
    Task<FileMetadataPersistenceView?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Stream> OpenFileAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<Stream> OpenFileRangeAsync(string objectKey, long first, long last, CancellationToken token = default)
        => throw new NotSupportedException("Storage ranges are not supported.");
    Task<bool> IsSubmittedSurveyVideoAsync(Guid project, Guid task, Guid file, CancellationToken token = default)
        => Task.FromResult(false);
    Task<UploadPersistenceStatus> VerifyNextAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetVerifyingIdsAsync(CancellationToken cancellationToken = default);
    Task<UploadPersistenceStatus> VerifyAsync(Guid uploadId, CancellationToken cancellationToken = default);
}
