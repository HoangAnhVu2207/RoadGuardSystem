using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;

namespace RoadGuardSystem.Services.Cases;

public sealed record RoutingResult(int Status, string? Code = null, object? Value = null);

public interface IRoutingMapService
{
    Task<RoutingResult> MapAsync(Guid actor, UserRoleCode role, string? bbox, int limit, string? cursor,
        bool history, CancellationToken token);
    Task<RoutingResult> CandidatesAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId, Guid? evidenceId,
        int limit, string? cursor, CancellationToken token);
    Task<RoutingResult> HistoryAsync(Guid actor, UserRoleCode role, Guid caseId, int limit, string? cursor,
        CancellationToken token);
    Task<RoutingResult> DecideAsync(Guid actor, UserRoleCode role, Guid caseId, string expectedVersion, string key,
        CaseRoutingDecisionInput input, Guid? correlation, CancellationToken token);
}
