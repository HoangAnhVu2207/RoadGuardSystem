using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairLifecycleHandoverData(Guid FirstStartId, Guid RecipientUserId,
    string PerformedPortion, string SafetyState);
public sealed record RepairCancellationData(string Reason, RepairLifecycleHandoverData? Handover);
public sealed record RepairNormalContinuationData(string RepairPlan, string ChecklistVersion, string Reason,
    RepairLifecycleHandoverData? Handover);
public sealed record RepairCancellationCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, RepairCancellationData Input, string Key, string ExpectedVersion);
public sealed record RepairNormalContinuationCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, RepairNormalContinuationData Input, string Key, string ExpectedVersion);
public sealed record RepairLifecycleFact(Guid SourceItemId, Guid? SuccessorItemId, Guid ObligationId,
    Guid? CancellationEventId, Guid? HandoverEventId, Guid? ContinuationId, string SourceState,
    string? SuccessorState, string SourceVersion, string? SuccessorVersion);
public interface IRepairLifecycleRepository
{
    Task<RepairWorkflowResult> CancelItemAsync(RepairCancellationCommand command, CancellationToken token);
    Task<RepairWorkflowResult> ContinueNormallyAsync(RepairNormalContinuationCommand command, CancellationToken token);
}
