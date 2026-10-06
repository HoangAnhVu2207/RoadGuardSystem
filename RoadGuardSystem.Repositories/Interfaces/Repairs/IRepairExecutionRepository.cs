using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairAssessmentCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairMeasurementAssessmentData Input, string Key, string ExpectedItemVersion);
public sealed record RepairExecutionStartCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairExecutionStartData Input, string Key, string ExpectedItemVersion);
public sealed record RepairExecutionFinishCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairExecutionFinishData Input, string Key, string ExpectedItemVersion);
public sealed record RepairAttemptSubmitCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairAttemptSubmitData Input, string Key, string ExpectedItemVersion);
public sealed record RepairAttemptSupplementCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairAttemptSupplementData Input, string Key, string ExpectedItemVersion);
public sealed record RepairAttemptReviewCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, Guid TaskId, RepairAttemptReviewData Input, string Key, string ExpectedItemVersion);
public sealed record RepairFinalConfirmCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId,
    Guid ItemId, RepairDecisionData Input, string Key, string ExpectedItemVersion);

public interface IRepairExecutionRepository
{
    Task<RepairWorkflowResult> AssessAsync(RepairAssessmentCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> StartExecutionAsync(RepairExecutionStartCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> FinishExecutionAsync(RepairExecutionFinishCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> SubmitAttemptAsync(RepairAttemptSubmitCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> SupplementAttemptAsync(RepairAttemptSupplementCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ReviewAttemptAsync(RepairAttemptReviewCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ConfirmFinalAsync(RepairFinalConfirmCommand command, CancellationToken cancellationToken);
}
