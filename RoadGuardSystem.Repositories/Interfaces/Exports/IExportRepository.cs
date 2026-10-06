using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Exports;

public sealed record ExportPersistenceView(ExportJob Job, ExportSnapshot Snapshot, StoredFile? ArtifactFile);
public sealed record ExportAdmissionResult(string? ErrorCode, ExportPersistenceView? Export);
public sealed record ExportCaptureResult(string? ErrorCode, ExportSnapshotPayloadDto? Payload);
public sealed record ExportClaim(Guid Id, Guid Token, Guid RequestedBy, Guid ProjectId, ExportSnapshot Snapshot);
public sealed class ExportRepositoryOptions
{
    public int LeaseDurationSeconds { get; set; } = 300;
    public int RetryBackoffSeconds { get; set; } = 60;
}
public interface IExportRepository
{
    Task<ExportAdmissionResult> AdmitAsync(Guid actorId, Guid projectId, CreateExportRequestDto request, string key, string fingerprint, Guid? correlationId, Func<Guid, DateTimeOffset, CancellationToken, Task<ExportCaptureResult>> capture, CancellationToken ct);
    Task<ExportPersistenceView?> GetAsync(Guid projectId, Guid id, CancellationToken ct);
    Task<ExportClaim?> ClaimAsync(CancellationToken ct);
    Task<bool> RenewAsync(Guid id, Guid token, CancellationToken ct);
    Task<bool> CompleteAsync(ExportClaim claim, Anh02ArtifactMetadata metadata, Func<CancellationToken, Task<bool>> authorize, CancellationToken ct);
    Task FailAsync(ExportClaim claim, string code, bool permanent, CancellationToken ct);
    Task<StoredFile?> GetSourceFileAsync(Guid fileId, CancellationToken ct);
    Task<bool> CanReadSurveySourcesAsync(Guid projectId, Guid[] fileIds, CancellationToken ct);
    Task<bool> CanReadSnapshotSourcesAsync(Guid actorId, Guid projectId, ExportSourceAuthorityQuery query, CancellationToken ct);
}
