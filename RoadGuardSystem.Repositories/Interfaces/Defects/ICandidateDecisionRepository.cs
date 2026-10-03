using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public sealed record CandidateTargetMatchFact(Guid DefectId, Guid ProjectId, Guid? RouteVersionId,
    Guid? SegmentId, string Version, IReadOnlyList<Guid> LinkedReportIds);

public interface ICandidateDecisionRepository
{
    Task<T> ReadConsistentlyAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken ct);
    Task<IReadOnlyList<CandidateTargetMatchFact>> MatchTargetsAsync(Guid projectId, CancellationToken ct);
    Task LockSourceAsync(Guid reportId, Guid expectedCase, CancellationToken ct);
    Task<CandidateDecisionResponseDto> SaveRejectAsync(Guid actor, CandidateSourceFacts facts, CandidateCorrection? correction,
        string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseDto> SaveAcceptedAsync(Guid actor, CandidateSourceFacts facts,
        CandidateDecisionKind kind, CandidateClassification? classification, Guid? targetDefectId,
        string? targetVersion, CandidateCorrection? correction, string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseDto?> ReadAsync(Guid projectId, Guid decisionId, Func<CancellationToken, Task> guard, CancellationToken ct);
}
