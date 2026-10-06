using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRepairLifecycleService
{
    Task<RepairWorkflowResult> CancelItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairCancellationInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> ContinueNormallyAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairNormalContinuationInput input, string? key, string? version, CancellationToken token);
}
