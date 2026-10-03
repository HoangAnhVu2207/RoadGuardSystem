using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Services.Candidates;

public interface ICandidateMatchingService
{
    Task<CandidateMatchResult> MatchAsync(Guid actor, UserRoleCode role, Guid project,
        string? sourceKind, Guid sourceId, bool expand, int pageSize, string? cursor,
        CancellationToken token);
}
