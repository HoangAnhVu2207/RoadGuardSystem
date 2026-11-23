using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;

namespace RoadGuardSystem.Repositories.Processing;

public sealed record RealAiArtifactReadFact(AiWorkerArtifact Proof, StoredFile File);
public sealed record RealAiCandidateReadFacts(AiVideoWorkItem Work, ProcessingJob Job, AIDetection Detection,
    AiVideoSource Source, IReadOnlyList<RealAiArtifactReadFact> Artifacts, ActiveCandidateDisposition? Disposition, string? DispositionVersion,
    IReadOnlyList<AiVideoTelemetryRevision>? TelemetryRevisions = null, AiPairedTelemetryFile? PairedSrt = null, string? PairsManifestHash = null);
public interface IRealAiCandidateRepository
{
    Task<RealAiCandidateReadFacts?> ReadAsync(Guid projectId, Guid detectionId, CancellationToken ct);
    Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct);
}
