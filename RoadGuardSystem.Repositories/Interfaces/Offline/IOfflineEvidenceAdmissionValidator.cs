using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Offline;

public sealed record OfflineEvidenceAdmissionFacts(Guid CaptureReferenceId, Guid AdmissionId, Guid OriginalActorId,
    Guid ActualUploaderId, Guid? GrantId, Guid FileId, Guid UploadSessionId, string Checksum, string Purpose,
    string MediaType, DateTimeOffset AdmittedAt);

// Called under the same authority/file/upload locks as the existing upload or FIELD transaction.
public interface IOfflineEvidenceAdmissionValidator
{
    // Resolve only while creating a new admitted revision. Persist the returned link separately;
    // never fill the signed declaration or reevaluate an old pending revision during review.
    Task<OfflineEvidenceAdmissionFacts?> ResolveDeclaredEvidenceAsync(Guid projectId, Guid taskId,
        Guid currentOfflineAdmissionId, Guid originalActorId, RepairFieldEvidenceData declaration,
        CancellationToken cancellationToken);
    Task<OfflineEvidenceAdmissionFacts?> ValidateFieldEvidenceAsync(Guid projectId, Guid taskId,
        Guid originalActorId, Guid captureOriginId, Guid fileId, string checksum, string purpose,
        string mediaType, CancellationToken cancellationToken);
    // null means this is an ordinary DIRECT file; a recognized but invalid admission must throw.
    Task<OfflineEvidenceAdmissionFacts?> GuardExistingFileAsync(Guid currentActorId, Guid fileId,
        bool technicalContinuation, CancellationToken cancellationToken);
}
