using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRepairPolicyService
{
    Task<RepairPolicyServiceResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, string action,
        Guid? resource, RepairPolicyDefinitionInput? definition, string? reason, string? key, string? version,
        CancellationToken cancellationToken);
}
