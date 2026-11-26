using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;

namespace RoadGuardSystem.Repositories.Cases;

public interface IRoutingCursorCodec
{
    string Protect(string value);
    string Unprotect(string value);
}

public interface IRoutingMapRepository
{
    Task GuardAsync(Guid actor, UserRoleCode role, Guid? caseId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task<RoutingMapView> MapAsync(Guid actor, UserRoleCode role, string? bbox, int limit, string? cursor, bool includeHistorical, CancellationToken token);
    Task<RoutingCandidatesView> CandidatesAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId, Guid? evidenceId,
        int limit, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task<RoutingDecisionPage> HistoryAsync(Guid actor, UserRoleCode role, Guid caseId, int limit, string? cursor,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task<RoutingWriteView> DecideAsync(Guid actor, UserRoleCode role, Guid caseId, string expectedVersion,
        CaseRoutingDecisionInput input, Guid? correlation, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task GuardDecisionAsync(Guid actor, UserRoleCode role, Guid caseId, CaseRoutingDecisionInput input,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task ValidateTriageAsync(Guid actor, UserRoleCode role, CaseCommand command, CancellationToken token);
    Task RecordTriageAsync(Guid actor, CaseCommand command, CancellationToken token);
}
