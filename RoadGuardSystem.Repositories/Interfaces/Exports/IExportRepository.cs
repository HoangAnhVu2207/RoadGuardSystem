using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Exports;

public sealed record ExportPersistenceView(ExportJob Job, ExportSnapshot Snapshot, StoredFile? ArtifactFile);
public sealed record ExportAdmissionResult(string? ErrorCode, ExportPersistenceView? Export);
public sealed record ExportCaptureResult(string? ErrorCode, ExportSnapshotPayloadFact? Payload);
public sealed record ExportClaim(Guid Id, Guid Token, Guid RequestedBy, Guid ProjectId, ExportSnapshot Snapshot);
public sealed class ExportRepositoryOptions
{
    public int LeaseDurationSeconds { get; set; } = 300;
    public int RetryBackoffSeconds { get; set; } = 60;
}
public interface IExportRepository
{
    Task<bool> HasRepairResourceScopeAsync(Guid actor, Guid project, CancellationToken token)
        => Task.FromResult(false);
    Task<bool> CanReadRepairItemsAsync(Guid actor, Guid project, Guid[] items, CancellationToken token)
        => Task.FromResult(false);
    Task<RoadGuardSystem.Repositories.Integration.ReporterFileFacts?> ReadRepairFileAsync(Guid actor, Guid project,
        Guid[] items, Guid fileId, CancellationToken token)
        => Task.FromResult<RoadGuardSystem.Repositories.Integration.ReporterFileFacts?>(null);
    Task<RepairExportSelection[]> ResolveRepairItemsAsync(Guid project, Guid[] packages, Guid[] items, CancellationToken token)
        => Task.FromResult(Array.Empty<RepairExportSelection>());
    Task<RepairExportSupplement?> ReadRepairSupplementAsync(Guid project, Guid item, CancellationToken token)
        => Task.FromResult<RepairExportSupplement?>(null);
    Task<ExportAdmissionResult> AdmitAsync(Guid actorId, Guid projectId, CreateExportRequestFact request, string key, string fingerprint, Guid? correlationId, Func<Guid, DateTimeOffset, CancellationToken, Task<ExportCaptureResult>> capture, CancellationToken ct);
    Task<ExportPersistenceView?> GetAsync(Guid projectId, Guid id, CancellationToken ct);
    Task<ExportClaim?> ClaimAsync(CancellationToken ct);
    Task<bool> RenewAsync(Guid id, Guid token, CancellationToken ct);
    Task<bool> CompleteAsync(ExportClaim claim, Anh02ArtifactMetadata metadata, Func<CancellationToken, Task<bool>> authorize, CancellationToken ct);
    Task FailAsync(ExportClaim claim, string code, bool permanent, CancellationToken ct);
    Task<StoredFile?> GetSourceFileAsync(Guid fileId, CancellationToken ct);
    Task<bool> CanReadSurveySourcesAsync(Guid projectId, Guid[] fileIds, CancellationToken ct);
    Task<bool> CanReadSnapshotSourcesAsync(Guid actorId, Guid projectId, ExportSourceAuthorityQuery query, CancellationToken ct);
    Task<bool> HasCurrentProjectMembershipAsync(Guid actorId, Guid projectId, CancellationToken ct);
}
public sealed record RepairExportSelection(Guid PackageId, Guid ItemId);
public sealed record RepairExportSupplement(System.Text.Json.JsonElement Scope,
    System.Text.Json.JsonElement[] Submissions, System.Text.Json.JsonElement[] Reviews);
