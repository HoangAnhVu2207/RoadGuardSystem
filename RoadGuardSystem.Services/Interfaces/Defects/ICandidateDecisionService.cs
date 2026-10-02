using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Services.Defects;

public sealed record CandidateDecisionResult(int Status, string? Code = null, CandidateDecisionResponseDto? Decision = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
public interface ICandidateDecisionService
{
    Task<CandidateDecisionResult> DecideAsync(Guid actor, UserRoleCode role, Guid projectId, CandidateDecisionRequestDto request, string key, Guid? correlation, CancellationToken ct);
    Task<CandidateDecisionResult> ReadAsync(Guid actor, UserRoleCode role, Guid projectId, Guid decisionId, CancellationToken ct);
}
