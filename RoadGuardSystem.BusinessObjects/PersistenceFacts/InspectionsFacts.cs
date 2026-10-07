using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record FieldEvidenceDeclarationFact(Guid CaptureOriginId, Guid? FileId, string Purpose,
    string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt, string? AttemptChecklist);

public sealed record FieldEvidenceReuseInputFact(Guid FileId, Guid SourceEvidenceId, string SourceKind, string ExpectedChecksum, string Reason);

public sealed record FieldHandoverInputFact(string PerformedPortionState, string Summary, Guid StartOriginId, Guid[] SubmissionIds, Guid? RecipientUserId);

public sealed record FieldLocationImpactActionInputFact(Guid ImpactId, string Action, string Reason, Guid? AssignedToUserId = null, FieldHandoverInputFact? Handover = null);

public sealed record FieldMeasurementInputFact(string SampleId, string Type, decimal? Value, string State,
    string? UnknownReason, string Dimension, string Unit, double? Longitude, double? Latitude,
    string? LocationReason, string Instrument, string Method, string? Notes = null);

public sealed record FieldPositionProofInputFact(string Kind, string? Checklist, double? Longitude, double? Latitude, string? ObservedSlabId = null, Guid? ObservedSegmentId = null, Guid? ObservedRouteVersionId = null, double? ObservedChainageMeters = null);

public sealed record FieldReviewInputFact(Guid SubmissionId, string Decision, string Reason);

public sealed record FieldReviewViewFact(Guid Id, Guid TaskId, Guid SubmissionId, string Decision, string ReceiptActivation);

public sealed record FieldStartInputFact(Guid OriginId, DateTimeOffset ClaimedAt, Guid? DeviceId = null,
    long? MonotonicMilliseconds = null, string? BootId = null, string? OfflineProof = null);

public sealed record FieldSubmissionInputFact(Guid OriginId, Guid StartOriginId, Guid? ParentSubmissionId,
    FieldMeasurementInputFact[]? Measurements, FieldEvidenceDeclarationFact[]? Evidence, FieldPositionProofInputFact? LocationProof,
    string CaptureType, bool? Repaired, string? UnrepairedReason, Guid? DeviceId = null);

public sealed record FieldSubmissionViewFact(Guid Id, Guid TaskId, Guid RootId, Guid? ParentId, int Revision,
    DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalReviewDueAt, string Readiness,
    string[] MissingReasons, string ContentHash, FieldMeasurementInputFact[] Measurements,
    FieldEvidenceDeclarationFact[] Evidence, string CaptureType, bool? Repaired, string? UnrepairedReason);

public sealed record FieldTaskActionInputFact(string Reason, Guid? AssignedToUserId = null, FieldHandoverInputFact? Handover = null);

public sealed record FieldTaskCreateInputFact(Guid DefectId, string DefectVersion, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    byte RequiredMeasurementType, string MeasurementScope, string? Instructions, Guid AssignedToUserId,
    DateTimeOffset DueAt, Guid? MapPublicationId = null, Guid? CrsProfileRevisionId = null);

public sealed record FieldTaskViewFact(Guid Id, Guid ProjectId, Guid DefectId, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    string Mode, string Status, Guid? AssignmentId, Guid? AssignedToUserId, string Version,
    Guid? FirstStartOriginId, Guid? LatestSubmissionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId,
    byte RequiredMeasurementType = 0, string? MeasurementScope = null, string? Instructions = null, DateTimeOffset? DueAt = null);

public sealed record FieldVerificationSourceFactsFact(Guid TaskId, Guid DefectId, Guid SubmissionId, string ContentHash,
    string Decision, Guid[] EvidenceIds, Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId);

public sealed record FieldVerificationSourceQueryFact(Guid DefectId, Guid SubmissionId, string ExpectedContentHash);

public sealed record FieldWorkflowResultFact(int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false);
