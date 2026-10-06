using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Inspections;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldTaskCreateInput(Guid DefectId, string DefectVersion, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    byte RequiredMeasurementType, string MeasurementScope, string? Instructions, Guid AssignedToUserId,
    DateTimeOffset DueAt, Guid? MapPublicationId = null, Guid? CrsProfileRevisionId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldTaskActionInput(string Reason, Guid? AssignedToUserId = null, FieldHandoverInput? Handover = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldHandoverInput(string PerformedPortionState,string Summary,Guid StartOriginId,Guid[] SubmissionIds,Guid? RecipientUserId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldLocationImpactActionInput(Guid ImpactId,string Action,string Reason,Guid? AssignedToUserId=null,FieldHandoverInput? Handover=null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldStartInput(Guid OriginId, DateTimeOffset ClaimedAt, Guid? DeviceId = null,
    long? MonotonicMilliseconds = null, string? BootId = null, string? OfflineProof = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldMeasurementInput(string SampleId, string Type, decimal? Value, string State,
    string? UnknownReason, string Dimension, string Unit, double? Longitude, double? Latitude,
    string? LocationReason, string Instrument, string Method, string? Notes = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldEvidenceDeclaration(Guid CaptureOriginId, Guid? FileId, string Purpose,
    string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt, string? AttemptChecklist);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldPositionProofInput(string Kind, string? Checklist, double? Longitude, double? Latitude, string? ObservedSlabId = null, Guid? ObservedSegmentId = null, Guid? ObservedRouteVersionId = null, double? ObservedChainageMeters = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldSubmissionInput(Guid OriginId, Guid StartOriginId, Guid? ParentSubmissionId,
    FieldMeasurementInput[]? Measurements, FieldEvidenceDeclaration[]? Evidence, FieldPositionProofInput? LocationProof,
    string CaptureType, bool? Repaired, string? UnrepairedReason, Guid? DeviceId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldReviewInput(Guid SubmissionId, string Decision, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldEvidenceReuseInput(Guid FileId, Guid SourceEvidenceId, string SourceKind, string ExpectedChecksum, string Reason);
public sealed record FieldTaskView(Guid Id, Guid ProjectId, Guid DefectId, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    string Mode, string Status, Guid? AssignmentId, Guid? AssignedToUserId, string Version,
    Guid? FirstStartOriginId, Guid? LatestSubmissionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId,
    byte RequiredMeasurementType=0,string? MeasurementScope=null,string? Instructions=null,DateTimeOffset? DueAt=null);
public sealed record FieldSubmissionView(Guid Id, Guid TaskId, Guid RootId, Guid? ParentId, int Revision,
    DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalReviewDueAt, string Readiness,
    string[] MissingReasons, string ContentHash, FieldMeasurementInput[] Measurements,
    FieldEvidenceDeclaration[] Evidence, string CaptureType, bool? Repaired, string? UnrepairedReason);
public sealed record FieldReviewView(Guid Id, Guid TaskId, Guid SubmissionId, string Decision, string ReceiptActivation);
public sealed record FieldWorkflowResult(int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false);
public sealed record FieldVerificationSourceQuery(Guid DefectId, Guid SubmissionId, string ExpectedContentHash);
public sealed record FieldVerificationSourceFacts(Guid TaskId, Guid DefectId, Guid SubmissionId, string ContentHash,
    string Decision, Guid[] EvidenceIds, Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId);
