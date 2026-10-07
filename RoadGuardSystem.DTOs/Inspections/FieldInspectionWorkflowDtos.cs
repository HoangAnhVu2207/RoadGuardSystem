using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Inspections;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldTaskCreateInput(Guid DefectId, string DefectVersion, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    byte RequiredMeasurementType, string MeasurementScope, string? Instructions, Guid AssignedToUserId,
    DateTimeOffset DueAt, Guid? MapPublicationId = null, Guid? CrsProfileRevisionId = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskCreateInputFact?(FieldTaskCreateInput? value)
        => value is null ? null! : new(value.DefectId, value.DefectVersion, value.SurveyId, value.SourceKind, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.SlabId, value.Purpose, value.RequiredMeasurementType, value.MeasurementScope, value.Instructions, value.AssignedToUserId, value.DueAt, value.MapPublicationId, value.CrsProfileRevisionId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldTaskCreateInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskCreateInputFact? value)
        => value is null ? null! : new(value.DefectId, value.DefectVersion, value.SurveyId, value.SourceKind, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.SlabId, value.Purpose, value.RequiredMeasurementType, value.MeasurementScope, value.Instructions, value.AssignedToUserId, value.DueAt, value.MapPublicationId, value.CrsProfileRevisionId);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldTaskActionInput(string Reason, Guid? AssignedToUserId = null, FieldHandoverInput? Handover = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact?(FieldTaskActionInput? value)
        => value is null ? null! : new(value.Reason, value.AssignedToUserId, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldHandoverInputFact?)value.Handover);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldTaskActionInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact? value)
        => value is null ? null! : new(value.Reason, value.AssignedToUserId, (global::RoadGuardSystem.DTOs.Inspections.FieldHandoverInput?)value.Handover);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldHandoverInput(string PerformedPortionState, string Summary, Guid StartOriginId, Guid[] SubmissionIds, Guid? RecipientUserId)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldHandoverInputFact?(FieldHandoverInput? value)
        => value is null ? null! : new(value.PerformedPortionState, value.Summary, value.StartOriginId, value.SubmissionIds, value.RecipientUserId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldHandoverInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldHandoverInputFact? value)
        => value is null ? null! : new(value.PerformedPortionState, value.Summary, value.StartOriginId, value.SubmissionIds, value.RecipientUserId);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldLocationImpactActionInput(Guid ImpactId, string Action, string Reason, Guid? AssignedToUserId = null, FieldHandoverInput? Handover = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldLocationImpactActionInputFact?(FieldLocationImpactActionInput? value)
        => value is null ? null! : new(value.ImpactId, value.Action, value.Reason, value.AssignedToUserId, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldHandoverInputFact?)value.Handover);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldLocationImpactActionInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldLocationImpactActionInputFact? value)
        => value is null ? null! : new(value.ImpactId, value.Action, value.Reason, value.AssignedToUserId, (global::RoadGuardSystem.DTOs.Inspections.FieldHandoverInput?)value.Handover);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldStartInput(Guid OriginId, DateTimeOffset ClaimedAt, Guid? DeviceId = null,
    long? MonotonicMilliseconds = null, string? BootId = null, string? OfflineProof = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact?(FieldStartInput? value)
        => value is null ? null! : new(value.OriginId, value.ClaimedAt, value.DeviceId, value.MonotonicMilliseconds, value.BootId, value.OfflineProof);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldStartInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact? value)
        => value is null ? null! : new(value.OriginId, value.ClaimedAt, value.DeviceId, value.MonotonicMilliseconds, value.BootId, value.OfflineProof);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldMeasurementInput(string SampleId, string Type, decimal? Value, string State,
    string? UnknownReason, string Dimension, string Unit, double? Longitude, double? Latitude,
    string? LocationReason, string Instrument, string Method, string? Notes = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldMeasurementInputFact?(FieldMeasurementInput? value)
        => value is null ? null! : new(value.SampleId, value.Type, value.Value, value.State, value.UnknownReason, value.Dimension, value.Unit, value.Longitude, value.Latitude, value.LocationReason, value.Instrument, value.Method, value.Notes);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldMeasurementInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldMeasurementInputFact? value)
        => value is null ? null! : new(value.SampleId, value.Type, value.Value, value.State, value.UnknownReason, value.Dimension, value.Unit, value.Longitude, value.Latitude, value.LocationReason, value.Instrument, value.Method, value.Notes);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldEvidenceDeclaration(Guid CaptureOriginId, Guid? FileId, string Purpose,
    string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt, string? AttemptChecklist)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceDeclarationFact?(FieldEvidenceDeclaration? value)
        => value is null ? null! : new(value.CaptureOriginId, value.FileId, value.Purpose, value.ChecksumSha256, value.MediaType, value.CapturedAt, value.AttemptChecklist);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldEvidenceDeclaration?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceDeclarationFact? value)
        => value is null ? null! : new(value.CaptureOriginId, value.FileId, value.Purpose, value.ChecksumSha256, value.MediaType, value.CapturedAt, value.AttemptChecklist);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldPositionProofInput(string Kind, string? Checklist, double? Longitude, double? Latitude, string? ObservedSlabId = null, Guid? ObservedSegmentId = null, Guid? ObservedRouteVersionId = null, double? ObservedChainageMeters = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldPositionProofInputFact?(FieldPositionProofInput? value)
        => value is null ? null! : new(value.Kind, value.Checklist, value.Longitude, value.Latitude, value.ObservedSlabId, value.ObservedSegmentId, value.ObservedRouteVersionId, value.ObservedChainageMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldPositionProofInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldPositionProofInputFact? value)
        => value is null ? null! : new(value.Kind, value.Checklist, value.Longitude, value.Latitude, value.ObservedSlabId, value.ObservedSegmentId, value.ObservedRouteVersionId, value.ObservedChainageMeters);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldSubmissionInput(Guid OriginId, Guid StartOriginId, Guid? ParentSubmissionId,
    FieldMeasurementInput[]? Measurements, FieldEvidenceDeclaration[]? Evidence, FieldPositionProofInput? LocationProof,
    string CaptureType, bool? Repaired, string? UnrepairedReason, Guid? DeviceId = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact?(FieldSubmissionInput? value)
        => value is null ? null! : new(value.OriginId, value.StartOriginId, value.ParentSubmissionId, value.Measurements?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldMeasurementInputFact)item).ToArray(), value.Evidence?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceDeclarationFact)item).ToArray(), (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldPositionProofInputFact?)value.LocationProof, value.CaptureType, value.Repaired, value.UnrepairedReason, value.DeviceId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldSubmissionInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact? value)
        => value is null ? null! : new(value.OriginId, value.StartOriginId, value.ParentSubmissionId, value.Measurements?.Select(item => (global::RoadGuardSystem.DTOs.Inspections.FieldMeasurementInput)item).ToArray(), value.Evidence?.Select(item => (global::RoadGuardSystem.DTOs.Inspections.FieldEvidenceDeclaration)item).ToArray(), (global::RoadGuardSystem.DTOs.Inspections.FieldPositionProofInput?)value.LocationProof, value.CaptureType, value.Repaired, value.UnrepairedReason, value.DeviceId);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldReviewInput(Guid SubmissionId, string Decision, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldReviewInputFact?(FieldReviewInput? value)
        => value is null ? null! : new(value.SubmissionId, value.Decision, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldReviewInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldReviewInputFact? value)
        => value is null ? null! : new(value.SubmissionId, value.Decision, value.Reason);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldEvidenceReuseInput(Guid FileId, Guid SourceEvidenceId, string SourceKind, string ExpectedChecksum, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceReuseInputFact?(FieldEvidenceReuseInput? value)
        => value is null ? null! : new(value.FileId, value.SourceEvidenceId, value.SourceKind, value.ExpectedChecksum, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldEvidenceReuseInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceReuseInputFact? value)
        => value is null ? null! : new(value.FileId, value.SourceEvidenceId, value.SourceKind, value.ExpectedChecksum, value.Reason);
}
public sealed record FieldTaskView(Guid Id, Guid ProjectId, Guid DefectId, Guid? SurveyId, string SourceKind,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId, string Purpose,
    string Mode, string Status, Guid? AssignmentId, Guid? AssignedToUserId, string Version,
    Guid? FirstStartOriginId, Guid? LatestSubmissionId, Guid? MapPublicationId, Guid? CrsProfileRevisionId,
    byte RequiredMeasurementType = 0, string? MeasurementScope = null, string? Instructions = null, DateTimeOffset? DueAt = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskViewFact?(FieldTaskView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.DefectId, value.SurveyId, value.SourceKind, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.SlabId, value.Purpose, value.Mode, value.Status, value.AssignmentId, value.AssignedToUserId, value.Version, value.FirstStartOriginId, value.LatestSubmissionId, value.MapPublicationId, value.CrsProfileRevisionId, value.RequiredMeasurementType, value.MeasurementScope, value.Instructions, value.DueAt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldTaskView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.DefectId, value.SurveyId, value.SourceKind, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.SlabId, value.Purpose, value.Mode, value.Status, value.AssignmentId, value.AssignedToUserId, value.Version, value.FirstStartOriginId, value.LatestSubmissionId, value.MapPublicationId, value.CrsProfileRevisionId, value.RequiredMeasurementType, value.MeasurementScope, value.Instructions, value.DueAt);
}
public sealed record FieldSubmissionView(Guid Id, Guid TaskId, Guid RootId, Guid? ParentId, int Revision,
    DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalReviewDueAt, string Readiness,
    string[] MissingReasons, string ContentHash, FieldMeasurementInput[] Measurements,
    FieldEvidenceDeclaration[] Evidence, string CaptureType, bool? Repaired, string? UnrepairedReason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionViewFact?(FieldSubmissionView? value)
        => value is null ? null! : new(value.Id, value.TaskId, value.RootId, value.ParentId, value.Revision, value.ServerReceivedAt, value.OriginalReviewDueAt, value.Readiness, value.MissingReasons, value.ContentHash, value.Measurements?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldMeasurementInputFact)item).ToArray()!, value.Evidence?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldEvidenceDeclarationFact)item).ToArray()!, value.CaptureType, value.Repaired, value.UnrepairedReason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldSubmissionView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionViewFact? value)
        => value is null ? null! : new(value.Id, value.TaskId, value.RootId, value.ParentId, value.Revision, value.ServerReceivedAt, value.OriginalReviewDueAt, value.Readiness, value.MissingReasons, value.ContentHash, value.Measurements?.Select(item => (global::RoadGuardSystem.DTOs.Inspections.FieldMeasurementInput)item).ToArray()!, value.Evidence?.Select(item => (global::RoadGuardSystem.DTOs.Inspections.FieldEvidenceDeclaration)item).ToArray()!, value.CaptureType, value.Repaired, value.UnrepairedReason);
}
public sealed record FieldReviewView(Guid Id, Guid TaskId, Guid SubmissionId, string Decision, string ReceiptActivation)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldReviewViewFact?(FieldReviewView? value)
        => value is null ? null! : new(value.Id, value.TaskId, value.SubmissionId, value.Decision, value.ReceiptActivation);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldReviewView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldReviewViewFact? value)
        => value is null ? null! : new(value.Id, value.TaskId, value.SubmissionId, value.Decision, value.ReceiptActivation);
}
public sealed record FieldWorkflowResult(int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldWorkflowResultFact?(FieldWorkflowResult? value)
        => value is null ? null! : new(value.Status, value.Code, BoundaryFactMappings.ToFacts(value.Value), value.Version, value.Replayed);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldWorkflowResult?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldWorkflowResultFact? value)
        => value is null ? null! : new(value.Status, value.Code, BoundaryFactMappings.ToWire(value.Value), value.Version, value.Replayed);
}
public sealed record FieldVerificationSourceQuery(Guid DefectId, Guid SubmissionId, string ExpectedContentHash)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldVerificationSourceQueryFact?(FieldVerificationSourceQuery? value)
        => value is null ? null! : new(value.DefectId, value.SubmissionId, value.ExpectedContentHash);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldVerificationSourceQuery?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldVerificationSourceQueryFact? value)
        => value is null ? null! : new(value.DefectId, value.SubmissionId, value.ExpectedContentHash);
}
public sealed record FieldVerificationSourceFacts(Guid TaskId, Guid DefectId, Guid SubmissionId, string ContentHash,
    string Decision, Guid[] EvidenceIds, Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldVerificationSourceFactsFact?(FieldVerificationSourceFacts? value)
        => value is null ? null! : new(value.TaskId, value.DefectId, value.SubmissionId, value.ContentHash, value.Decision, value.EvidenceIds, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator FieldVerificationSourceFacts?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldVerificationSourceFactsFact? value)
        => value is null ? null! : new(value.TaskId, value.DefectId, value.SubmissionId, value.ContentHash, value.Decision, value.EvidenceIds, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId);
}
