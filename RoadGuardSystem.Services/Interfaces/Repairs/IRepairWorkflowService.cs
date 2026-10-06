using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRepairWorkflowService
{
    Task<RepairWorkflowResult> CorrectAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, RepairCorrectionInput input, string? key, string? expectedVersion,
        CancellationToken cancellationToken);
    Task<RepairWorkflowResult> RequestReviewAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, RepairReviewRequestInput input, string? key, string? expectedVersion,
        CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken);
}
