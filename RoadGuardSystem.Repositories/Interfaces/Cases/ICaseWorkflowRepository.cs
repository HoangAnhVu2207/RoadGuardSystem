using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Cases;

namespace RoadGuardSystem.Repositories.Cases;

public sealed class CaseWorkflowException(int status, string code) : Exception
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed record CaseCommand(Guid CaseId, string ExpectedVersion, string Action, string Reason,
    Guid? ProjectId = null, CaseVerificationMethod? Method = null, IReadOnlyList<Guid>? ReportIds = null,
    IReadOnlyDictionary<Guid, string>? SourceCaseVersions = null, CaseConclusionOutcome? Outcome = null,
    IReadOnlyList<Guid>? DefectIds = null, IReadOnlyList<Guid>? EvidenceIds = null,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, string? GeometryVersion = null);
public sealed record CaseWriteResult(InternalCaseDto Case, CasePublicationResponseDto? Publication = null);
public interface ICaseWorkflowRepository
{
    Task GuardAsync(Guid actor, UserRoleCode role, IReadOnlyCollection<Guid> caseIds, Guid? requestedProject,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct);
    Task GuardCommandAsync(Guid actor, UserRoleCode role, CaseCommand command,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct);
    Task<InternalCaseDto> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct);
    Task<CasePageDto> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status,
        int pageSize, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct);
    Task<CaseWriteResult> ApplyAsync(Guid actor, UserRoleCode role, CaseCommand command,
        Func<Guid, CancellationToken, Task<bool>> projectAccess,
        Func<CancellationToken, Task>? geometryCheck, Guid? correlation, CancellationToken ct);
}
