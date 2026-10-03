using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Retention;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfirmRetentionBasisRequest(Guid[] WarrantyIds, string ExpectedReferenceInventoryVersion, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRetentionHoldRequest(string ScopeType, Guid ScopeId, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReleaseRetentionHoldRequest(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRetentionEvaluationRequest(Guid[]? FileIds);
public sealed record RetentionReferenceView(string Kind, Guid Id, Guid? ProjectId, string SourceVersion);
public sealed record RetentionWarrantyView(Guid Id, Guid ProjectId, string Scope, DateOnly WarrantyEndDate, string SourceVersion);
public sealed record RetentionHoldView(Guid Id, string ScopeType, Guid ScopeId, string State, Guid CreatedBy,
    DateTimeOffset CreatedAt, string Reason, Guid? ReleasedBy, DateTimeOffset? ReleasedAt, string Version);
public sealed record RetentionItemView(Guid FileId, string Eligibility, IReadOnlyList<string> ReasonCodes,
    DateTimeOffset? EligibleAfter, string BasisVersion, string InventoryVersion, string HoldVersion, bool IsCurrent);
public sealed record RetentionFileView(Guid FileId, Guid? BasisHeadId, string BasisVersion,
    string InventoryVersion, bool InventoryComplete, string Classification,
    IReadOnlyList<RetentionReferenceView> References, IReadOnlyList<RetentionWarrantyView> ApplicableObligations,
    IReadOnlyList<string> InventoryReasonCodes, int ActiveHoldCount, RetentionItemView Evaluation);
public sealed record RetentionBasisView(Guid Id, Guid FileId, int Revision, string PolicyVersion,
    string Classification, string ReferenceInventoryVersion, bool InventoryComplete,
    IReadOnlyList<RetentionWarrantyView> WarrantyRefs, Guid ConfirmedBy, DateTimeOffset ConfirmedAt,
    string Reason, Guid? SupersedesId, string Version);
public sealed record RetentionEvaluationView(Guid Id, Guid ProjectId, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? EvaluatedAt, string PolicyVersion, IReadOnlyList<RetentionItemView> Items, string? NextCursor, string Version);
