using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Offline;

public sealed record OfflineRepairSnapshotQuery(Guid ProjectId, Guid TaskId, Guid CallerId, UserRoleCode CallerRole);
public sealed record OfflineRepairSnapshotFacts(Guid ItemId, Guid TaskId, Guid AssignmentId, Guid OriginalActorId,
    string ItemVersion, Guid? AuthorizationId, Guid? PolicyRevisionId, string? PolicyHash, object SafePayload);
public sealed record OfflineRepairCoreCommand(OfflineOperationData Operation, Guid ProjectId,
    Guid CallerId, UserRoleCode CallerRole, OfflineFieldAdmissionFacts Admission,
    string ExpectedItemVersion);
public sealed record OfflineRepairEffect(int Status, string? Code, Guid? EffectId, Guid? CanonicalOriginId,
    string? ResourceVersion, object? SafeOutcome, DateTimeOffset? IndependentlyVerifiedFinishedAt);

// Actual finite H4 core; there is no second repair policy implementation in offline sync.
public interface IOfflineRepairCommandAdapter
{
    string ComputeCoreHash(OfflineOperationData operation);
    Task<OfflineRepairSnapshotFacts?> ReadSnapshotInTransactionAsync(OfflineRepairSnapshotQuery query,
        CancellationToken cancellationToken);
    Task<OfflineRepairEffect> ApplyInTransactionAsync(OfflineRepairCoreCommand command,
        CancellationToken cancellationToken);
}
