using RoadGuardSystem.BusinessObjects.Processing.Worker;

namespace RoadGuardSystem.Repositories.Processing;

public sealed record AiVideoSource(Guid DatasetId, Guid ProjectId, Guid FileId, Guid OwnerId,
    string ObjectKey, string Sha256, long Bytes, string? ScopeManifest = null, string? SourceVideoVersion = null);
public sealed record AiPreparedVideo(Guid JobId, Guid AttemptId, Guid ModelVersionId,
    string LogicalConfigurationIdentity, string AdmissionJson, string ManifestJson, string ManifestHash);
public sealed record AiRegisteredArtifact(Guid ArtifactId, string Sha256, long Bytes, string MediaType,
    string Kind, string ObjectKey, int? Width, int? Height);
public sealed record AiProposedDetection(Guid Id, Guid FrameArtifactId, string SourceClass,
    string TypeCode, decimal Confidence, string PayloadJson);
public interface IAiVideoWorkRepository
{
    Task<AiVideoSource?> NextSourceAsync(string logicalConfigurationIdentity, CancellationToken ct, IReadOnlySet<Guid>? excludedDatasetIds = null);
    Task RecordSourceFailureAsync(AiVideoSource source, string logicalConfigurationIdentity, string errorCode, CancellationToken ct);
    Task<Guid> PrepareAsync(AiVideoSource source, AiPreparedVideo p, IReadOnlyDictionary<string, string> classMap, CancellationToken ct);
    Task<AiVideoWorkItem?> ClaimAsync(string owner, DateTimeOffset now, TimeSpan lease, CancellationToken ct);
    Task<AiVideoSource> ReadSourceAsync(AiVideoWorkItem work, CancellationToken ct);
    Task ChangeAsync(Guid id, string owner, Guid attemptId, AiVideoWorkState? state, string? errorCode,
        DateTimeOffset now, DateTimeOffset nextPoll, CancellationToken ct);
    Task CompleteAsync(Guid id, string owner, Guid attemptId, IReadOnlyList<AiRegisteredArtifact> artifacts,
        IReadOnlyList<AiProposedDetection> detections, IReadOnlyDictionary<string, string> classMap, DateTimeOffset now, CancellationToken ct);
}
