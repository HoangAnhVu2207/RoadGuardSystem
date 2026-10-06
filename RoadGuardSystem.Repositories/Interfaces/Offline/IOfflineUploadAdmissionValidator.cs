using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Files;

namespace RoadGuardSystem.Repositories.Offline;

public sealed record OfflineUploadCaptureRequest(Guid ProjectId, Guid TaskId, Guid AdmissionId,
    Guid CaptureOriginId, Guid CurrentActorId, UserRoleCode CurrentRole, UploadCreatePersistenceRequest Upload);
public sealed record OfflineUploadCaptureFacts(Guid ProjectId, Guid TaskId, Guid AdmissionId, Guid BindingId,
    Guid CaptureOriginId, Guid OriginalActorId, Guid ActualUploaderId, Guid? GrantId,
    string Checksum, string Purpose, string MediaType, DateTimeOffset? DeclaredCapturedAt, string CaptureFactsJson);

// This validator shares the existing upload transaction/context. It never starts a nested transaction.
public interface IOfflineUploadAdmissionValidator
{
    Task<OfflineUploadCaptureFacts> GuardNewUploadAsync(OfflineUploadCaptureRequest request,
        CancellationToken cancellationToken);
    Task BindCreatedFileAsync(OfflineUploadCaptureFacts facts, Guid fileId, Guid uploadSessionId,
        DateTimeOffset admittedAt, CancellationToken cancellationToken);
    Task GuardCommittedUploadAsync(OfflineUploadCaptureRequest request, Guid fileId, Guid uploadSessionId,
        CancellationToken cancellationToken);
}
