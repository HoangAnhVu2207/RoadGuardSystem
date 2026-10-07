using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Repositories.Processing;

public sealed record AiSourceFileFact(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType,
    string Purpose, string ObjectKey);
public sealed record AiSourcePair(Guid VideoFileId, Guid? TelemetryFileId);
public sealed record AiDatasetSourceFacts(Guid DatasetId, Guid ProjectId, string DatasetManifestHash,
    AiSourceFileFact[] Files, AiSourcePair[] Pairs);
public sealed record AiAdmissionManifest(string CanonicalJson, string Hash, string GeometryVersion);
public sealed record AiCandidateReadFacts(AiMockRun Run, AiResultProvenance Result, AiDetectionProvenance Proof,
    AIDetection Detection, string FrameSha256, long FrameBytes, string FrameMediaType, string SourceVideoVersion, string SourceVideoSha256, long SourceVideoBytes,
    ActiveCandidateDisposition? Disposition, string? DispositionVersion);
public sealed record AiCompletion(AiResultProvenance Result, IReadOnlyList<AIDetection> Detections,
    IReadOnlyList<AiDetectionProvenance> Proofs, IReadOnlyList<RoadGuardSystem.BusinessObjects.Files.StoredFile> Frames);
public sealed class AiRequestException(int status, string code) : Exception(code)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public interface IAnh02AiRepository
{
    Task AuthorizeAsync(Guid actorId, UserRoleCode role, Guid projectId, bool managerOnly, CancellationToken ct);
    Task<AiDatasetSourceFacts> ReadSourcesAsync(Guid projectId, CreateAiMockRunRequestFact request, CancellationToken ct);
    Task<AiMockRunViewFact> AdmitAsync(Guid actorId, UserRoleCode role, Guid projectId, CreateAiMockRunRequestFact request,
        string key, string fingerprint, Func<Guid, Guid, Guid, AiDatasetSourceFacts, CancellationToken, Task<AiAdmissionManifest>> createManifest, CancellationToken ct);
    Task<AiMockRun?> GetAsync(Guid projectId, Guid runId, CancellationToken ct);
    Task<AiResultProvenance?> GetResultAsync(Guid runId, CancellationToken ct);
    Task<AiMockRun?> ClaimAsync(Guid owner, DateTimeOffset now, CancellationToken ct);
    Task<bool> CompleteAsync(Guid runId, Guid owner, AiCompletion completion,
        Func<CancellationToken, Task<bool>> recheckGeometry, CancellationToken ct);
    Task FailAsync(Guid runId, Guid owner, string code, CancellationToken ct);
    Task<AiCandidateReadFacts?> ReadCandidateAsync(Guid projectId, Guid detectionId, CancellationToken ct);
    Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct);
}
