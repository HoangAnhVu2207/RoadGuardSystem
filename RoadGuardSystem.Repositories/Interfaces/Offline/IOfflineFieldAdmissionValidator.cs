using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Offline;

// Internal admission facts. Public payload selectors or a client time enum never construct this proof.
public sealed record OfflineFieldAdmissionFacts(Guid AdmissionId, Guid EffectId, Guid ProjectId, Guid TaskId,
    Guid AssignmentId, Guid OriginalActorId, Guid SourceDeviceId, Guid CallerId, UserRoleCode CallerRole,
    Guid? GrantId, string Kind, string CorePayloadHash, Guid SnapshotId,
    DateTimeOffset? VerifiedOriginalAt, string TimeProvenance);

public interface IOfflineFieldAdmissionValidator
{
    // Must run inside the caller-owned transaction, including protected replay/conflict/recovery.
    Task<OfflineFieldAdmissionFacts?> ValidateAsync(FieldWorkflowCommand command, CancellationToken cancellationToken);
    // Direct commands also respect a previously bound offline identity; existing canonical hashes stay unchanged.
    Task GuardOriginBindingAsync(Guid projectId, Guid originId, string kind, string corePayloadHash,
        Guid originalActorId, Guid taskId, CancellationToken cancellationToken);
}
