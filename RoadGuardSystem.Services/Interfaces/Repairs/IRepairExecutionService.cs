using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRepairExecutionService
{
    Task<RepairWorkflowResult> AssessAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairMeasurementAssessmentInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> StartExecutionAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairExecutionStartInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> FinishExecutionAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairExecutionFinishInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> SubmitAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairAttemptSubmitInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> SupplementAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairAttemptSupplementInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> ReviewAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairAttemptReviewInput input, string? key, string? version, CancellationToken token);
    Task<RepairWorkflowResult> ConfirmFinalAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairDecisionInput input, string? key, string? version, CancellationToken token);
}
