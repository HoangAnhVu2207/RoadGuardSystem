using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRepairProducerService
{
    Task<RepairWorkflowResult> CreatePackageAsync(Guid actor, UserRoleCode role, Guid project,
        RepairPackageCreateInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> ProposeItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        RepairItemProposeInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> ApproveItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairDecisionInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> AssignItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairItemAssignInput input, string? key, string? version, CancellationToken token);
}
