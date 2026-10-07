using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Retention;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfirmRetentionBasisRequest(Guid[] WarrantyIds, string ExpectedReferenceInventoryVersion, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.ConfirmRetentionBasisRequestFact?(ConfirmRetentionBasisRequest? value)
        => value is null ? null! : new(value.WarrantyIds, value.ExpectedReferenceInventoryVersion, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ConfirmRetentionBasisRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.ConfirmRetentionBasisRequestFact? value)
        => value is null ? null! : new(value.WarrantyIds, value.ExpectedReferenceInventoryVersion, value.Reason);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRetentionHoldRequest(string ScopeType, Guid ScopeId, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.CreateRetentionHoldRequestFact?(CreateRetentionHoldRequest? value)
        => value is null ? null! : new(value.ScopeType, value.ScopeId, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CreateRetentionHoldRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.CreateRetentionHoldRequestFact? value)
        => value is null ? null! : new(value.ScopeType, value.ScopeId, value.Reason);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReleaseRetentionHoldRequest(string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.ReleaseRetentionHoldRequestFact?(ReleaseRetentionHoldRequest? value)
        => value is null ? null! : new(value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReleaseRetentionHoldRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.ReleaseRetentionHoldRequestFact? value)
        => value is null ? null! : new(value.Reason);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRetentionEvaluationRequest(Guid[]? FileIds)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.CreateRetentionEvaluationRequestFact?(CreateRetentionEvaluationRequest? value)
        => value is null ? null! : new(value.FileIds);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CreateRetentionEvaluationRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.CreateRetentionEvaluationRequestFact? value)
        => value is null ? null! : new(value.FileIds);
}
public sealed record RetentionReferenceView(string Kind, Guid Id, Guid? ProjectId, string SourceVersion)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionReferenceViewFact?(RetentionReferenceView? value)
        => value is null ? null! : new(value.Kind, value.Id, value.ProjectId, value.SourceVersion);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionReferenceView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionReferenceViewFact? value)
        => value is null ? null! : new(value.Kind, value.Id, value.ProjectId, value.SourceVersion);
}
public sealed record RetentionWarrantyView(Guid Id, Guid ProjectId, string Scope, DateOnly WarrantyEndDate, string SourceVersion)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionWarrantyViewFact?(RetentionWarrantyView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Scope, value.WarrantyEndDate, value.SourceVersion);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionWarrantyView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionWarrantyViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Scope, value.WarrantyEndDate, value.SourceVersion);
}
public sealed record RetentionHoldView(Guid Id, string ScopeType, Guid ScopeId, string State, Guid CreatedBy,
    DateTimeOffset CreatedAt, string Reason, Guid? ReleasedBy, DateTimeOffset? ReleasedAt, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionHoldViewFact?(RetentionHoldView? value)
        => value is null ? null! : new(value.Id, value.ScopeType, value.ScopeId, value.State, value.CreatedBy, value.CreatedAt, value.Reason, value.ReleasedBy, value.ReleasedAt, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionHoldView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionHoldViewFact? value)
        => value is null ? null! : new(value.Id, value.ScopeType, value.ScopeId, value.State, value.CreatedBy, value.CreatedAt, value.Reason, value.ReleasedBy, value.ReleasedAt, value.Version);
}
public sealed record RetentionItemView(Guid FileId, string Eligibility, IReadOnlyList<string> ReasonCodes,
    DateTimeOffset? EligibleAfter, string BasisVersion, string InventoryVersion, string HoldVersion, bool IsCurrent)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionItemViewFact?(RetentionItemView? value)
        => value is null ? null! : new(value.FileId, value.Eligibility, value.ReasonCodes, value.EligibleAfter, value.BasisVersion, value.InventoryVersion, value.HoldVersion, value.IsCurrent);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionItemView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionItemViewFact? value)
        => value is null ? null! : new(value.FileId, value.Eligibility, value.ReasonCodes, value.EligibleAfter, value.BasisVersion, value.InventoryVersion, value.HoldVersion, value.IsCurrent);
}
public sealed record RetentionFileView(Guid FileId, Guid? BasisHeadId, string BasisVersion,
    string InventoryVersion, bool InventoryComplete, string Classification,
    IReadOnlyList<RetentionReferenceView> References, IReadOnlyList<RetentionWarrantyView> ApplicableObligations,
    IReadOnlyList<string> InventoryReasonCodes, int ActiveHoldCount, RetentionItemView Evaluation)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionFileViewFact?(RetentionFileView? value)
        => value is null ? null! : new(value.FileId, value.BasisHeadId, value.BasisVersion, value.InventoryVersion, value.InventoryComplete, value.Classification, value.References?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionReferenceViewFact)item).ToArray()!, value.ApplicableObligations?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionWarrantyViewFact)item).ToArray()!, value.InventoryReasonCodes, value.ActiveHoldCount, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionItemViewFact)value.Evaluation);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionFileView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionFileViewFact? value)
        => value is null ? null! : new(value.FileId, value.BasisHeadId, value.BasisVersion, value.InventoryVersion, value.InventoryComplete, value.Classification, value.References?.Select(item => (global::RoadGuardSystem.DTOs.Retention.RetentionReferenceView)item).ToArray()!, value.ApplicableObligations?.Select(item => (global::RoadGuardSystem.DTOs.Retention.RetentionWarrantyView)item).ToArray()!, value.InventoryReasonCodes, value.ActiveHoldCount, (global::RoadGuardSystem.DTOs.Retention.RetentionItemView)value.Evaluation);
}
public sealed record RetentionBasisView(Guid Id, Guid FileId, int Revision, string PolicyVersion,
    string Classification, string ReferenceInventoryVersion, bool InventoryComplete,
    IReadOnlyList<RetentionWarrantyView> WarrantyRefs, Guid ConfirmedBy, DateTimeOffset ConfirmedAt,
    string Reason, Guid? SupersedesId, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionBasisViewFact?(RetentionBasisView? value)
        => value is null ? null! : new(value.Id, value.FileId, value.Revision, value.PolicyVersion, value.Classification, value.ReferenceInventoryVersion, value.InventoryComplete, value.WarrantyRefs?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionWarrantyViewFact)item).ToArray()!, value.ConfirmedBy, value.ConfirmedAt, value.Reason, value.SupersedesId, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionBasisView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionBasisViewFact? value)
        => value is null ? null! : new(value.Id, value.FileId, value.Revision, value.PolicyVersion, value.Classification, value.ReferenceInventoryVersion, value.InventoryComplete, value.WarrantyRefs?.Select(item => (global::RoadGuardSystem.DTOs.Retention.RetentionWarrantyView)item).ToArray()!, value.ConfirmedBy, value.ConfirmedAt, value.Reason, value.SupersedesId, value.Version);
}
public sealed record RetentionEvaluationView(Guid Id, Guid ProjectId, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? EvaluatedAt, string PolicyVersion, IReadOnlyList<RetentionItemView> Items, string? NextCursor, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionEvaluationViewFact?(RetentionEvaluationView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Status, value.CreatedAt, value.EvaluatedAt, value.PolicyVersion, value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionItemViewFact)item).ToArray()!, value.NextCursor, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RetentionEvaluationView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention.RetentionEvaluationViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Status, value.CreatedAt, value.EvaluatedAt, value.PolicyVersion, value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Retention.RetentionItemView)item).ToArray()!, value.NextCursor, value.Version);
}
