using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Services.Integration;

// Named Huy implementation remains unbound until its exact SQL reader is handed
// off. Capture in the caller's scoped SERIALIZABLE/SNAPSHOT transaction; never
// open another context or best-effort page. Null means unavailable, not empty.
// Throw UnauthorizedAccessException for current actor/role/project denial.
// ReceivedAt filtering is [from,to); Case/Defect status is current stock.
// Missing spatial/source/lifecycle facts must be explicit MissingReasons.
public interface ICaseDefectReadReader
{
    Task<RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1?> CaptureAsync(Guid actorId, UserRoleCode role,
        Guid projectId, RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto filters, CancellationToken cancellationToken = default);
}

// Local ANH-02 additive producer boundary. Huy implementation/adoption at an
// exact commit is still pending. A missing reader is not an empty success.
public sealed record TrainingLabelFilterV1(Guid[] SegmentIds, Guid[] DefectIds, DateTimeOffset? From, DateTimeOffset? To);
public sealed record NormalizedBoundingBoxV1(decimal X, decimal Y, decimal Width, decimal Height);
public sealed record ApprovedTrainingLabelV1(Guid LabelId, int Revision, Guid RevisionId, Guid ProjectId,
    string TypeCode, NormalizedBoundingBoxV1 Annotation, Guid FileId, string FileVersion, string Sha256,
    long SizeBytes, string MediaType, string SourceKind, Guid SourceId, string SourceVersion,
    Guid ApprovalId, Guid ApprovedBy, DateTimeOffset ApprovedAt, Guid? ProcessingJobId,
    Guid? ModelVersionId, Guid? DatasetVersionId, string Mode, Guid? SegmentId);
public sealed record ApprovedLabelSnapshotV1(string SchemaVersion, Guid SnapshotId, string Hash,
    DateTimeOffset CapturedAt, IReadOnlyList<ApprovedTrainingLabelV1> Labels);
// Capture under the exporter's scoped SQL transaction or return a durable
// immutable producer snapshot. Only current APPROVED revisions are eligible.
public interface IApprovedTrainingLabelReader
{
    Task<ApprovedLabelSnapshotV1?> CaptureApprovedAsync(Guid actorId, UserRoleCode role, Guid projectId,
        TrainingLabelFilterV1 filters, CancellationToken cancellationToken = default);
}
// Recheck current actor/role/project/source/file permission even for historical
// exports. The snapshot's label need not remain the latest approved head.
// Historical approval/export inclusion never grants current resource access.
public interface ITrainingSourceAccessReader
{
    Task<bool> CanReadAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid[] fileIds,
        CancellationToken cancellationToken = default);
}

public sealed record MatchingCandidateItemV1(Guid DefectId, string Version, Guid? SegmentId, Guid RouteVersionId);
public sealed record MatchingCandidateSnapshotV1(Guid SnapshotId, string Hash, Guid ProjectId,
    Guid RouteVersionId, Guid SegmentSetId, string GeometryVersion, DateTimeOffset CreatedAt,
    IReadOnlyList<MatchingCandidateItemV1> Items);
// Capture/recheck must enlist in the caller scoped SERIALIZABLE transaction,
// or lock/validate a durable immutable producer snapshot at command completion.
public interface IMatchingCandidateSnapshotReader
{
    Task<MatchingCandidateSnapshotV1?> CaptureAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid routeVersionId, Guid segmentSetId, string geometryVersion, Guid[] detectionIds,
        Guid requestedSnapshotId, CancellationToken cancellationToken = default);
}

public sealed record AiCandidateFactsV1(string SchemaVersion, Guid DetectionId, Guid ProjectId,
    Guid DatasetVersionId, Guid ModelVersionId, Guid RunId, Guid JobId, Guid AttemptId,
    Guid ResultId, string ManifestHash, string ResultHash, string FixtureVersion, string Mode,
    Guid SourceVideoFileId, string SourceVideoFileVersion, Guid FrameFileId, string FrameFileVersion,
    string FrameSha256, long FrameSizeBytes, string FrameMediaType, long TimestampMs,
    string TypeCode, decimal Confidence, NormalizedBoundingBoxV1 BoundingBox,
    Guid RouteVersionId, Guid SegmentSetId, Guid? SegmentId, string GeometryVersion,
    string PositionStatus, string SourceVersion, ActiveCandidateDisposition? Disposition,
    string? DispositionVersion);
public interface IAiCandidateFactsReader
{
    Task<AnhHuyProducerResult<AiCandidateFactsV1>> ResolveAsync(Guid actorId, UserRoleCode role,
        Guid projectId, Guid detectionId, string? expectedSourceVersion = null,
        string? expectedGeometryVersion = null, string? expectedDispositionVersion = null,
        CancellationToken cancellationToken = default);
}
