using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    private async Task<OfflineFieldAdmissionFacts?> ImportedFactsAsync(FieldWorkflowCommand command, CancellationToken cancellationToken)
    {
        var context = command.Admission;
        if (context.Mode == "DIRECT") return null;
        if (context.Mode is not ("SYNC" or "HANDOVER") || command.Action is not ("accept" or "start" or "submit") ||
            context.TrustedOnlineOrigin || context.OfflineAdmissionId is null || offlineAdmission is null)
            Deny(403, "offline_admission_required");
        var facts = await offlineAdmission.ValidateAsync(command, cancellationToken);
        var kind = command.Action switch { "accept" => "FIELD_ACCEPT", "start" => "FIELD_START", _ => "FIELD_SUBMISSION" };
        if (facts is null || facts.AdmissionId != context.OfflineAdmissionId || facts.CallerId != context.CallerId ||
            facts.CallerRole != context.CallerRole || facts.OriginalActorId != context.OriginalActorId || facts.ProjectId != command.ProjectId ||
            facts.TaskId != command.TaskId || facts.Kind != kind || facts.EffectId == Guid.Empty)
            Deny(403, "offline_admission_required");
        if (OriginMetadata(command.Input) is { } origin &&
            (origin.OriginId != facts.EffectId || origin.DeviceId != facts.SourceDeviceId))
            Deny(409, "origin_content_conflict");
        return facts;
    }
}
