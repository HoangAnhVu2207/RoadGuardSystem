namespace RoadGuardSystem.BusinessObjects.Retention;

public sealed class RetentionBasisHead
{
    public Guid FileId { get; set; }
    public Guid RevisionId { get; set; }
    public int Revision { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RetentionBasisRevision
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public int Revision { get; set; }
    public string PolicyVersion { get; set; } = "pr41a.v1";
    public string Classification { get; set; } = "EVIDENCE";
    public string InventoryVersion { get; set; } = "";
    public bool InventoryComplete { get; set; }
    public string WarrantyReferencesJson { get; set; } = "[]";
    public Guid ConfirmedBy { get; set; }
    public DateTimeOffset ConfirmedAt { get; set; }
    public string Reason { get; set; } = "";
    public Guid? SupersedesId { get; set; }
}
public sealed class RetentionHold
{
    public Guid Id { get; set; }
    public string ScopeType { get; set; } = "";
    public Guid ScopeId { get; set; }
    public string State { get; set; } = "ACTIVE";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Reason { get; set; } = "";
    public Guid? ReleasedBy { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RetentionHoldHistory
{
    public Guid Id { get; set; }
    public Guid HoldId { get; set; }
    public string State { get; set; } = "";
    public Guid ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Reason { get; set; } = "";
}
public sealed class RetentionEvaluation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EvaluatedAt { get; set; }
    public string Status { get; set; } = "QUEUED";
    public string PolicyVersion { get; set; } = "pr41a.v1";
    // null means all known project inventory at evaluation time, never a client supplied snapshot.
    public string? SelectionJson { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RetentionEvaluationItem
{
    public Guid Id { get; set; }
    public Guid EvaluationId { get; set; }
    public Guid FileId { get; set; }
    public string Eligibility { get; set; } = "";
    public string ReasonCodesJson { get; set; } = "[]";
    public DateTimeOffset? EligibleAfter { get; set; }
    public string BasisVersion { get; set; } = "none";
    public string InventoryVersion { get; set; } = "";
    public string HoldVersion { get; set; } = "";
    public string ControlSnapshotJson { get; set; } = "{}";
}
