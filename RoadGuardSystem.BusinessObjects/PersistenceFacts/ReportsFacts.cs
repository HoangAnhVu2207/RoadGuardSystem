using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record OwnReportFact(Guid Id, string Description, DateTimeOffset CreatedAt, string Version,
    IReadOnlyList<OwnReportEvidenceFact> Evidence, string RoutingStatus,
    IReadOnlyList<OwnReportUpdateFact> PublicUpdates, IReadOnlyList<OwnReportSupplementFact> Supplements);

public sealed record OwnReportEvidenceFact(Guid Id, Guid FileId, string FileVersion, Guid? SupplementId,
    DateTimeOffset? CapturedAt, string LocationSource, ReportEvidenceLocationFact? Location);

public sealed record OwnReportPageFact(IReadOnlyList<OwnReportFact> Items, string? NextCursor);

public sealed record OwnReportSupplementFact(Guid Id, string Description, DateTimeOffset CreatedAt);

public sealed record OwnReportUpdateFact(Guid Id, string Summary, DateTimeOffset PublishedAt, IReadOnlyList<Guid> EvidenceIds);

public sealed record ReportEvidenceLocationFact(decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters);
