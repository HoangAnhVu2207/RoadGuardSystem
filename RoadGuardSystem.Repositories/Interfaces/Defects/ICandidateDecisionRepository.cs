using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public sealed record CandidateTargetMatchFact(Guid DefectId, Guid ProjectId, Guid? RouteVersionId,
    Guid? SegmentId, string Version, IReadOnlyList<Guid> LinkedReportIds);

public interface ICandidateDecisionRepository
{
    Task<T?> ReadSnapshotConsistentlyAsync<T>(Func<CancellationToken, Task<T?>> read, CancellationToken ct) where T : class;
    Task<T> ReadConsistentlyAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken ct);
    Task<IReadOnlyList<CandidateTargetMatchFact>> MatchTargetsAsync(Guid projectId, CancellationToken ct);
    Task LockSourceAsync(Guid reportId, Guid expectedCase, CancellationToken ct);
    Task LockAiSourceAsync(Guid detectionId, CancellationToken ct);
    Task<CandidateDecisionResponseFact> SaveRejectAsync(Guid actor, CandidateSourceFacts facts, CandidateCorrection? correction,
        string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseFact> SaveAcceptedAsync(Guid actor, CandidateSourceFacts facts,
        CandidateDecisionKind kind, CandidateClassification? classification, Guid? targetDefectId,
        string? targetVersion, CandidateCorrection? correction, string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseFact?> ReadAsync(Guid projectId, Guid decisionId, Func<CancellationToken, Task> guard, CancellationToken ct);
}
