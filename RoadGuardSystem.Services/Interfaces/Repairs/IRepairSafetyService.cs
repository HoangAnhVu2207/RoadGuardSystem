using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public sealed record RepairSafetyServiceResult(int Status, string? Code = null, RepairSafetyView? Value = null);
public interface IRepairSafetyService
{
    Task<RepairSafetyServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, CancellationToken token);
    Task<RepairSafetyServiceResult> CreateAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, RepairSafetyCreateInput input, string? key, string? version, CancellationToken token);
    Task<RepairSafetyServiceResult> InstallAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, RepairSafetyInstallInput input, string? key, string? version, CancellationToken token);
    Task<RepairSafetyServiceResult> CheckAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, RepairSafetyCheckInput input, string? key, string? version, CancellationToken token);
    Task<RepairSafetyServiceResult> AcknowledgeAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, Guid warning, RepairSafetyAcknowledgementInput input,
        string? key, string? version, CancellationToken token);
}
