using System.Text.Json.Serialization;
using RoadGuardSystem.DTOs.Inspections;

namespace RoadGuardSystem.DTOs.Repairs;

// Local typed command design. Routes/SQL activation and canonical inventory belong to the H4 checkpoint.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairActualScopeInput(Guid PhysicalRoadId, Guid RouteVersionId, Guid? SegmentSetId,
    Guid? LayoutRevisionId, string? SlabId, decimal FromMeters, decimal ToMeters, decimal OffsetFromMeters, decimal OffsetToMeters);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairObligationInput(string Kind, bool Mandatory, RepairActualScopeInput Scope, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPackageCreateInput(Guid DefectId, string DefectVersion, RepairObligationInput[] Obligations, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairItemProposeInput(Guid ObligationId, string Mode, string RepairPlan, string ChecklistVersion, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairDecisionInput(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairItemAssignInput(FieldTaskCreateInput Task, Guid? PolicyRevisionId, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairMeasurementAssessmentInput(Guid OriginId, Guid FieldFirstStartId,
    FieldMeasurementInput[]? Measurements, FieldEvidenceDeclaration[]? Evidence,
    FieldPositionProofInput? LocationProof, Guid? DeviceId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, bool?>? StopConditions = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairExecutionStartInput(Guid OriginId, Guid FieldFirstStartId, DateTimeOffset ClaimedAt, Guid AssessmentId,
    Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairExecutionFinishInput(Guid OriginId, Guid ExecutionStartId, DateTimeOffset ClaimedAt,
    Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptSubmitInput(Guid FieldSubmissionId, string ExpectedContentHash, Guid? ExecutionFinishId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptSupplementInput(Guid FieldSubmissionId, string ExpectedContentHash);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptReviewInput(Guid FieldSubmissionId, string Decision, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairItemCancelInput(string Reason, FieldHandoverInput? Handover);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPolicyRuleInput(string Code, string Unit, decimal Minimum, decimal Maximum);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPolicyDraftInput(string DefectTypeCode, string ChecklistVersion,
    RepairPolicyRuleInput[] Measurements, string[] StopConditions, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyCreateInput(Guid FormalRepairObligationId, Guid SafetyObligationId, Guid ResponsibleActorId,
    string CheckSchedule, string ReplacementCondition, string RemovalCondition, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyCheckInput(string Result, string Findings, Guid[] EvidenceIds);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairSafetyResponsibilityInput(Guid NextResponsibleActorId, string PerformedPortion,
    string Handover, string Reason);

public sealed record RepairTaskBindingView(Guid ItemId, Guid ObligationId, Guid TaskId, Guid AssignmentId,
    string Mode, Guid? AuthorizationId, Guid? PolicyRevisionId, Guid RouteVersionId, Guid? SegmentSetId,
    Guid? LayoutRevisionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId, string? SlabId, Guid? FirstStartId);
public sealed record RepairEligibilityView(bool Eligible, string Reason, string SourceMapping,
    string[] UnknownSources, DateTimeOffset? OriginalVerifiedStart, DateTimeOffset? ExecutionExpiresAt);
public sealed record RepairAssessmentView(Guid Id, Guid ItemId, Guid TaskId, Guid FirstStartId, Guid SessionId,
    string Stage, string Readiness, string[] MissingReasons, string LocationState, string ContentHash);
public sealed record RepairExecutionStartView(Guid Id, Guid ItemId, Guid TaskId, Guid AssessmentId, Guid FirstStartId,
    DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance);
public sealed record RepairExecutionFinishView(Guid Id, Guid ItemId, Guid TaskId, Guid ExecutionStartId,
    DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance,
    Guid? SyncClockId, DateTimeOffset? OriginalSyncDueAt);
public sealed record RepairAttemptIntakeView(Guid AttemptId, Guid LinkId, Guid ItemId, Guid SubmissionId,
    Guid ReviewClockId, DateTimeOffset OriginalReviewDueAt, string IntakeReadiness);
public sealed record RepairAttemptReviewView(Guid ReviewId, Guid ItemId, Guid SubmissionId, string Decision,
    string ReceiptActivation);
public sealed record RepairItemView(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId, string Mode,
    string State, string Result, Guid? EffectiveDecisionId, Guid[] AttemptIds, Guid[] DecisionIds, string Version);
public sealed record RepairPackageView(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved,
    Guid[] UnresolvedMandatoryObligationIds, RepairItemView[] Items, string Version);
