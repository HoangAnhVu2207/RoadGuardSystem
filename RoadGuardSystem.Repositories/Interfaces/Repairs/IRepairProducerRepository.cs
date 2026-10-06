using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairPackageCreateCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    RepairPackageCreateData Input, string Key, string ExpectedDefectVersion);
public sealed record RepairItemProposeCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    RepairItemProposeData Input, string Key, string ExpectedPackageVersion);
public sealed record RepairItemApprovalCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, RepairDecisionData Input, string Key, string ExpectedItemVersion);
public sealed record RepairItemAssignCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, RepairItemAssignData Input, string Key, string ExpectedItemVersion);

// Independent typed producing admission. Existing correction/request DTOs and committed hashes remain unchanged.
public interface IRepairProducerRepository
{
    Task<RepairWorkflowResult> CreatePackageAsync(RepairPackageCreateCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ProposeItemAsync(RepairItemProposeCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ApproveItemAsync(RepairItemApprovalCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> AssignItemAsync(RepairItemAssignCommand command, CancellationToken cancellationToken);
}
