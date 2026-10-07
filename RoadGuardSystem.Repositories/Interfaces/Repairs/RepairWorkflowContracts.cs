using System.Text.Json.Serialization;
namespace RoadGuardSystem.Repositories.Repairs;
// Internal typed command and persisted fact contracts. Property order preserves existing receipts and hashes.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairFieldEvidenceData(Guid CaptureOriginId, Guid? FileId, string Purpose,
    string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt, string? AttemptChecklist);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairFieldMeasurementData(string SampleId, string Type, decimal? Value, string State,
    string? UnknownReason, string Dimension, string Unit, double? Longitude, double? Latitude,
    string? LocationReason, string Instrument, string Method, string? Notes = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairFieldPositionData(string Kind, string? Checklist, double? Longitude, double? Latitude, string? ObservedSlabId = null, Guid? ObservedSegmentId = null, Guid? ObservedRouteVersionId = null, double? ObservedChainageMeters = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairFieldSubmissionData(Guid OriginId, Guid StartOriginId, Guid? ParentSubmissionId,
    RepairFieldMeasurementData[]? Measurements, RepairFieldEvidenceData[]? Evidence, RepairFieldPositionData? LocationProof,
    string CaptureType, bool? Repaired, string? UnrepairedReason, Guid? DeviceId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairFieldTaskData(Guid DefectId, string DefectVersion, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    byte RequiredMeasurementType, string MeasurementScope, string? Instructions, Guid AssignedToUserId,
    DateTimeOffset DueAt, Guid? MapPublicationId = null, Guid? CrsProfileRevisionId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairActualScopeData(Guid PhysicalRoadId, Guid RouteVersionId, Guid? SegmentSetId,
    Guid? LayoutRevisionId, string? SlabId, decimal FromMeters, decimal ToMeters, decimal OffsetFromMeters, decimal OffsetToMeters);
public sealed record RepairAssessmentFact(Guid Id, Guid ItemId, Guid TaskId, Guid FirstStartId, Guid SessionId,
    string Stage, string Readiness, string[] MissingReasons, string LocationState, string ContentHash);
public sealed record RepairAttemptHistoryFact(Guid Id, Guid OriginId, string ContentHash, Guid ItemId,
    Guid ObligationId, Guid ProjectId, Guid DefectId, Guid CrewId, Guid TaskId, Guid AssignmentId,
    Guid? AuthorizationId, string LocationVersion, Guid? PolicyRevisionId, bool Performed, string? UnperformedReason,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, DateTimeOffset ServerReceivedAt,
    string TimeProvenance, RepairEvidenceHistoryFact[] Evidence);
public sealed record RepairAttemptIntakeFact(Guid AttemptId, Guid LinkId, Guid ItemId, Guid SubmissionId,
    Guid ReviewClockId, DateTimeOffset OriginalReviewDueAt, string IntakeReadiness);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptReviewData(Guid FieldSubmissionId, string Decision, string Reason);
public sealed record RepairAttemptReviewFact(Guid ReviewId, Guid ItemId, Guid SubmissionId, string Decision,
    string ReceiptActivation);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptSubmitData(Guid FieldSubmissionId, string ExpectedContentHash, Guid? ExecutionFinishId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairAttemptSupplementData(Guid FieldSubmissionId, string ExpectedContentHash);
public sealed record RepairCorrectionBasisHistoryFact(string Text, RepairEvidenceHistoryFact[] Evidence);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairCorrectionBasisData(string Text, Guid[] EvidenceReferenceIds);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairCorrectionData(Guid SupersedesDecisionId, string Result, string Reason, RepairCorrectionBasisData Basis);
public sealed record RepairCorrectionFact(Guid Id, Guid ItemId, Guid ObligationId, Guid SupersedesDecisionId,
    string Result, string Reason, string Basis, Guid[] EvidenceReferenceIds, Guid ActorId, DateTimeOffset At,
    bool ObligationResolved, string ItemState, string Version);
public sealed record RepairDecisionHistoryFact(Guid Id, Guid ItemId, Guid ObligationId, Guid DefectId,
    string Mode, Guid ActorId, string Role, string Reason, DateTimeOffset At, Guid? SupersedesDecisionId,
    string Result, RepairCorrectionBasisHistoryFact? Basis);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairDecisionData(string Reason);
public sealed record RepairEvidenceHistoryFact(Guid FileId, string? FileVersion, string? Sha256, string Purpose,
    bool Verified, string SourceKind, Guid SourceId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId, bool ReusedSource);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairExecutionFinishData(Guid OriginId, Guid ExecutionStartId, DateTimeOffset ClaimedAt,
    Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null);
public sealed record RepairExecutionFinishFact(Guid Id, Guid ItemId, Guid TaskId, Guid ExecutionStartId,
    DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance,
    Guid? SyncClockId, DateTimeOffset? OriginalSyncDueAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairExecutionStartData(Guid OriginId, Guid FieldFirstStartId, DateTimeOffset ClaimedAt, Guid AssessmentId,
    Guid? DeviceId = null, long? MonotonicMilliseconds = null, string? BootId = null);
public sealed record RepairExecutionStartFact(Guid Id, Guid ItemId, Guid TaskId, Guid AssessmentId, Guid FirstStartId,
    DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset? VerifiedOriginalAt, string TimeProvenance);
public sealed record RepairHistoryFact(RepairItemFact Item, RepairDecisionHistoryFact[] Decisions,
    RepairAttemptHistoryFact[] Attempts, RepairReviewRequestHistoryFact[] ReviewRequests);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairItemAssignData(RepairFieldTaskData Task, Guid? PolicyRevisionId, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairItemProposeData(Guid ObligationId, string Mode, string RepairPlan, string ChecklistVersion, string Reason);
public sealed record RepairItemFact(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId, string Mode,
    string State, string Result, Guid? EffectiveDecisionId, Guid[] AttemptIds, Guid[] DecisionIds, string Version);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairMeasurementAssessmentData(Guid OriginId, Guid FieldFirstStartId,
    RepairFieldMeasurementData[]? Measurements, RepairFieldEvidenceData[]? Evidence,
    RepairFieldPositionData? LocationProof, Guid? DeviceId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, bool?>? StopConditions = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairObligationData(string Kind, bool Mandatory, RepairActualScopeData Scope, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPackageCreateData(Guid DefectId, string DefectVersion, RepairObligationData[] Obligations, string Reason);
public sealed record RepairPackageFact(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved,
    Guid[] UnresolvedMandatoryObligationIds, RepairItemFact[] Items, string Version);
public sealed record RepairReviewRequestHistoryFact(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,
    string Role, string Reason, DateTimeOffset At);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairReviewRequestData(string Reason);
public sealed record RepairReviewRequestFact(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,
    string Role, string Reason, DateTimeOffset At, string Version);
public sealed record RepairTaskBindingFact(Guid ItemId, Guid ObligationId, Guid TaskId, Guid AssignmentId,
    string Mode, Guid? AuthorizationId, Guid? PolicyRevisionId, Guid RouteVersionId, Guid? SegmentSetId,
    Guid? LayoutRevisionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId, string? SlabId, Guid? FirstStartId);
public sealed record RepairFieldSourceResult(int Status, string? Code = null);
public sealed class RepairFieldCoreRejectedException(int status, string? code) : Exception("Repair FIELD source admission rejected.") { public int Status { get; } = status; public string? Code { get; } = code; }
public sealed record RepairCorrectionOutboxEvent(int SchemaVersion, Guid EventId, Guid OriginEventId, Guid ProjectId, string Kind, string SourceKind, Guid SourceId, Guid SourceRevisionId, DateTimeOffset OccurredAtUtc, Guid ObligationId, Guid SupersedesDecisionId, string Result);
