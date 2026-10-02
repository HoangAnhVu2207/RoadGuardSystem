using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Repositories.Cases;

namespace RoadGuardSystem.Services.Cases;

public sealed record CaseWorkflowResult(int Status, string? Code = null, CaseWriteResult? Write = null,
    InternalCaseDto? Case = null, CasePageDto? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null);
public interface ICaseWorkflowService
{
    Task<CaseWorkflowResult> ReadAsync(Guid actor, UserRoleCode role, Guid id, CancellationToken ct);
    Task<CaseWorkflowResult> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status, int pageSize, string? cursor, CancellationToken ct);
    Task<CaseWorkflowResult> CommandAsync(Guid actor, UserRoleCode role, CaseCommand command, string key, Guid? correlation, CancellationToken ct);
}
