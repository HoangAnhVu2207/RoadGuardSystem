using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record CandidateDecisionResponseFact(Guid Id, string SourceKind, Guid SourceId, string Decision,
    Guid? DefectId, string Version, Guid? SupersedesDecisionId);

public sealed record DefectGeometryFact(int Srid, string Wkt);

public sealed record DefectPageFact(IReadOnlyList<DefectViewFact> Items, string? NextCursor);

public sealed record DefectViewFact(Guid Id, Guid ProjectId, Guid? RoadSectionVersionId,
    Guid? SegmentId, string DefectTypeCode, string? CauseCategoryCode, string Severity,
    string Status, DefectGeometryFact? Geometry, string Version);
