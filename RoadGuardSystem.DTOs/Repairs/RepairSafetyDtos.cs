using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyInstallInput(DateTimeOffset FirstCheckDueAt, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyAcknowledgementInput(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyResolveInput(Guid CheckId, string Outcome, bool ConditionSatisfied,
    string ChecklistConfirmation, string Reason, Guid[] EvidenceIds, string ObligationVersion);
public sealed record SafetyResolutionEvidenceView(Guid FileId, string? FileVersion, string? Hash, Guid EvidenceLinkId,
    DateTimeOffset? CapturedAt, Guid? ActualUploaderId);
public sealed record SafetyResolutionView(Guid Id, Guid ActorId, DateTimeOffset At, string Outcome, Guid CheckId,
    string Reason, string Condition, string ChecklistConfirmation, string MeasureVersion, string ObligationVersion,
    SafetyResolutionEvidenceView[] Evidence);
public sealed record RepairSafetyView(Guid Id, Guid ProjectId, Guid DefectId, Guid FormalObligationId,
    Guid SafetyObligationId, Guid ResponsibleActorId, DateTimeOffset? InstalledAt, DateTimeOffset? FirstCheckDueAt,
    Guid? CurrentCheckId, RepairSafetyCheckView[] Checks, RepairSafetyWarningView[] Warnings,
    RepairSafetyAcknowledgementView[] Acknowledgements, string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RepairSafetyEvidenceLink[]? EligibleEvidenceLinks = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SafetyObligationReadView? Obligation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SafetyResolutionView? Resolution = null);
public sealed record SafetyObligationReadView(bool Resolved, Guid OriginProjectId, Guid ResponsibilityProjectId,
    string Version, SafetyObligationScopeView Scope);
public sealed record RepairSafetyEvidenceLink(Guid EvidenceLinkId, Guid FileId);
public sealed record CrewSafetyMeasureList(Guid[] MeasureIds);
public sealed record RepairSafetyCheckView(Guid Id, Guid ActorId, DateTimeOffset At, string Result, string Findings,
    Guid[] EvidenceLinkIds);
public sealed record RepairSafetyWarningView(Guid Id, Guid SourceCheckId, Guid IntendedActorId,
    DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalDueAt, string Reason);
public sealed record RepairSafetyAcknowledgementView(Guid Id, Guid WarningId, Guid ActorId, DateTimeOffset At,
    bool AfterOriginalDue);
