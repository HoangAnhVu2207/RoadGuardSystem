using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;

namespace RoadGuardSystem.Repositories.Inspections;

public sealed record FieldAdmissionContext(Guid CallerId, UserRoleCode CallerRole, Guid OriginalActorId,
    string Mode, bool TrustedOnlineOrigin, Guid? HandoverGrantId = null, Guid? OfflineAdmissionId = null);
public sealed record FieldWorkflowCommand(Guid ProjectId, Guid? TaskId, string Action, object? Input,
    string? Key, string? ExpectedVersion, FieldAdmissionContext Admission);
public interface IFieldInspectionWorkflowRepository
{
    Task<FieldWorkflowResultFact> ExecuteAsync(FieldWorkflowCommand command,
        Func<CancellationToken, Task<bool>> projectGuard, CancellationToken cancellationToken);
    // Caller already owns the atomic transaction; this is the same finite business core used by direct admission.
    Task<FieldWorkflowResultFact> ApplyInTransactionAsync(FieldWorkflowCommand command,
        Func<CancellationToken, Task<bool>> projectGuard, CancellationToken cancellationToken);
    // Internal offline command path; preserves canonical input bytes while calling the same transaction core.
    Task<FieldCoreOutcome> ApplyInternalInTransactionAsync(FieldWorkflowCommand command,
        Func<CancellationToken, Task<bool>> projectGuard, CancellationToken cancellationToken);
}
public sealed record FieldTaskEvidenceFile(Guid FileId, string State, string Checksum, string MediaType, long SizeBytes,
    [property: System.Text.Json.Serialization.JsonIgnore] string ObjectKey);
public sealed record FieldTaskListQuery(Guid? AfterId, int Limit);
