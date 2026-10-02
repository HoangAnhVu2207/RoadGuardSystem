using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Repositories.Models.Huy01;

// Relational projections of domain collections. Huy's repository must synchronize
// these rows and the domain snapshots in the same transaction as history/receipt/outbox.
public sealed class HuyCaseReportLink
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid ReportId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HuyPublicationRecipient
{
    public Guid PublicationId { get; set; }
    public Guid ReportId { get; set; }
}

public sealed class HuyPublicationEvidence
{
    public Guid PublicationId { get; set; }
    public Guid RecipientReportId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid SourceReportId { get; set; }
    public Guid? OriginalEvidenceId { get; set; }
    public Guid? SupplementEvidenceId { get; set; }
}

public sealed class HuyCandidateSourceHead
{
    public CandidateSourceKind SourceKind { get; set; }
    public Guid SourceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DecisionId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HuyConclusionDefect
{
    public Guid ConclusionId { get; set; }
    public Guid DefectId { get; set; }
}

public sealed class HuyPublicationDefect
{
    public Guid PublicationId { get; set; }
    public Guid DefectId { get; set; }
}

public sealed class HuyConclusionEvidence
{
    public Guid ConclusionId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid SourceReportId { get; set; }
    public Guid? OriginalEvidenceId { get; set; }
    public Guid? SupplementEvidenceId { get; set; }
}

public sealed class HuyLinkHistoryReport
{
    public Guid HistoryId { get; set; }
    public Guid ReportId { get; set; }
}
