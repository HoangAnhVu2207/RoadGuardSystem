using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record CaseConclusionViewFact(Guid Id, string Outcome, string Reason, DateTimeOffset ConcludedAt,
    IReadOnlyList<Guid> DefectIds, IReadOnlyList<Guid> EvidenceIds);

public sealed record CasePageFact(IReadOnlyList<InternalCaseFact> Items, string? NextCursor);

public sealed record CasePublicationResponseFact(Guid Id, Guid CaseId, string Status, string Version);

public sealed record InternalCaseFact(Guid Id, Guid? ProjectId, string Status, string Version,
    IReadOnlyList<Guid> ReportIds, IReadOnlyList<Guid> DefectIds, string? VerificationMethod, string WarrantyRouting,
    CaseConclusionViewFact? Conclusion, DateTimeOffset CreatedAt)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing.RoutingDispositionView? RoutingDisposition { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing.RoutingTriageSourceView? RoutingSource { get; init; }
}
