using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record ConfirmRetentionBasisRequestFact(Guid[] WarrantyIds, string ExpectedReferenceInventoryVersion, string Reason);

public sealed record CreateRetentionEvaluationRequestFact(Guid[]? FileIds);

public sealed record CreateRetentionHoldRequestFact(string ScopeType, Guid ScopeId, string Reason);

public sealed record ReleaseRetentionHoldRequestFact(string Reason);

public sealed record RetentionBasisViewFact(Guid Id, Guid FileId, int Revision, string PolicyVersion,
    string Classification, string ReferenceInventoryVersion, bool InventoryComplete,
    IReadOnlyList<RetentionWarrantyViewFact> WarrantyRefs, Guid ConfirmedBy, DateTimeOffset ConfirmedAt,
    string Reason, Guid? SupersedesId, string Version);

public sealed record RetentionEvaluationViewFact(Guid Id, Guid ProjectId, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? EvaluatedAt, string PolicyVersion, IReadOnlyList<RetentionItemViewFact> Items, string? NextCursor, string Version);

public sealed record RetentionFileViewFact(Guid FileId, Guid? BasisHeadId, string BasisVersion,
    string InventoryVersion, bool InventoryComplete, string Classification,
    IReadOnlyList<RetentionReferenceViewFact> References, IReadOnlyList<RetentionWarrantyViewFact> ApplicableObligations,
    IReadOnlyList<string> InventoryReasonCodes, int ActiveHoldCount, RetentionItemViewFact Evaluation);

public sealed record RetentionHoldViewFact(Guid Id, string ScopeType, Guid ScopeId, string State, Guid CreatedBy,
    DateTimeOffset CreatedAt, string Reason, Guid? ReleasedBy, DateTimeOffset? ReleasedAt, string Version);

public sealed record RetentionItemViewFact(Guid FileId, string Eligibility, IReadOnlyList<string> ReasonCodes,
    DateTimeOffset? EligibleAfter, string BasisVersion, string InventoryVersion, string HoldVersion, bool IsCurrent);

public sealed record RetentionReferenceViewFact(string Kind, Guid Id, Guid? ProjectId, string SourceVersion);

public sealed record RetentionWarrantyViewFact(Guid Id, Guid ProjectId, string Scope, DateOnly WarrantyEndDate, string SourceVersion);
