using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairLifecycleHandoverInput(Guid FirstStartId, Guid RecipientUserId,
    string PerformedPortion, string SafetyState);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairCancellationInput(string Reason, RepairLifecycleHandoverInput? Handover);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairNormalContinuationInput(string RepairPlan, string ChecklistVersion, string Reason,
    RepairLifecycleHandoverInput? Handover);
public sealed record RepairLifecycleView(Guid SourceItemId, Guid? SuccessorItemId, Guid ObligationId,
    Guid? CancellationEventId, Guid? HandoverEventId, Guid? ContinuationId, string SourceState,
    string? SuccessorState, string SourceVersion, string? SuccessorVersion);
