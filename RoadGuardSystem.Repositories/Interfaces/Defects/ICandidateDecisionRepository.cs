using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public interface ICandidateDecisionRepository
{
    Task LockSourceAsync(Guid reportId, Guid expectedCase, CancellationToken ct);
    Task<CandidateDecisionResponseDto> SaveRejectAsync(Guid actor, CandidateSourceFacts facts, CandidateCorrection? correction,
        string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseDto> SaveAcceptedAsync(Guid actor, CandidateSourceFacts facts,
        CandidateDecisionKind kind, CandidateClassification? classification, Guid? targetDefectId,
        string? targetVersion, CandidateCorrection? correction, string reason, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResponseDto?> ReadAsync(Guid projectId, Guid decisionId, Func<CancellationToken, Task> guard, CancellationToken ct);
}
