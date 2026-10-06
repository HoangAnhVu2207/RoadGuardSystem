using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairCorrectionCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    Guid PackageId, Guid ItemId, RepairCorrectionData Input, string Key, string ExpectedVersion);
public sealed record RepairReviewRequestCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    Guid PackageId, Guid ItemId, RepairReviewRequestData Input, string Key, string ExpectedVersion);
public sealed record RepairWorkflowResult(int Status, string? Code = null, object? Value = null,
    string? Version = null, bool Replayed = false);

public interface IRepairWorkflowRepository
{
    Task<RepairWorkflowResult> CorrectAsync(RepairCorrectionCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> RequestReviewAsync(RepairReviewRequestCommand command, CancellationToken cancellationToken);
    Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken);
}
