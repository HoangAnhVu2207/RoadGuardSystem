using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Processing.Worker;

namespace RoadGuardSystem.Repositories.Processing;

public sealed record AiEmbeddedTelemetryFile(Guid ArtifactId, Guid FileId, string ObjectKey, string Sha256, long Bytes, string MediaType);
public sealed record AiPairedTelemetryFile(AiEmbeddedTelemetryFile File, long OffsetMs);
public sealed record AiVideoWorkSummary(Guid WorkId, Guid SourceVideoFileId, Guid JobId, Guid CurrentAttemptId, string State, string? ErrorCode);
public sealed record AiVideoWorkPage(IReadOnlyList<AiVideoWorkSummary> Items, int Offset, int Limit, bool HasMore);
public sealed record AiVideoTelemetryScope(Guid WorkId, Guid ProjectId, Guid CurrentAttemptId, Guid SourceVideoFileId,
    string SourceVideoHash, long DurationMs, string AdmissionJson, Guid HeadRevisionId, AiEmbeddedTelemetryFile? EmbeddedSrt,
    AiPairedTelemetryFile? PairedSrt = null);
public sealed record AiVideoTelemetryAppend(Guid RevisionId, Guid ExpectedHeadId, Guid ExpectedAttemptId, string SourceVideoHash,
    string Kind, string IntervalsJson, string? OriginalSrtObjectKey, string? OriginalSrtSha256, long? OriginalSrtBytes,
    long OffsetMs, string? Note, string IdempotencyKeyHash, string PayloadFingerprint,
    Guid? ExpectedEmbeddedSrtFileId, bool EmbeddedSrtIsValid);

public interface IAiVideoTelemetryRepository
{
    Task<AiVideoWorkPage> DiscoverAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid datasetId, int offset, int limit, CancellationToken ct);
    Task<AiVideoTelemetryScope> ReadScopeAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct);
    Task<IReadOnlyList<AiVideoTelemetryRevision>> ListAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct);
    Task<AiVideoTelemetryRevision> AppendAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, AiVideoTelemetryAppend request, CancellationToken ct);
}
