namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;

public sealed record RoutingGeometry(string Type, double[][] Coordinates);
public sealed record RoutingRouteProperties(Guid ProjectId, string ProjectName, string ProjectStatus,
    Guid RoadSectionId, string RoadSectionCode, string? RoadSectionName, Guid RouteVersionId,
    int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, string GeometryHash,
    Guid? CrsProfileRevisionId, IReadOnlyList<RoutingSegmentSetPin> SegmentSets,
    IReadOnlyList<RoutingCoveragePin> CoveragePins, IReadOnlyList<RoutingPolicyPin> PolicyPins,
    string GeometryUsage = "DISPLAY_ONLY");
public sealed record RoutingPolicyPin(Guid Id, int Revision, string DefectTypeCode, DateTimeOffset PublishedAt,
    bool Revoked, string Version);
public sealed record RoutingSegmentSetPin(Guid Id, string Status, string Version, string? GeometryHash);
public sealed record RoutingCoveragePin(Guid Id, Guid ObligationId, string LocationVersion, string Provenance,
    string SourceKind, Guid SourceId, string SourceVersion, DateTimeOffset ApplicableFromUtc, DateTimeOffset ApplicableToUtc,
    string Version, Guid OriginProjectId, Guid CurrentResponsibleProjectId, Guid? CurrentRepairItemId);
public sealed record RoutingHistoricalScopePin(Guid ObligationId, Guid RouteVersionId, string LocationVersion);
public sealed record RoutingMapRouteProperties(Guid ProjectId, string ProjectName, string ProjectStatus,
    Guid RoadSectionId, string RoadSectionCode, string? RoadSectionName, Guid RouteVersionId,
    int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, string GeometryHash,
    Guid? CrsProfileRevisionId, IReadOnlyList<RoutingSegmentSetPin> SegmentSets,
    IReadOnlyList<RoutingHistoricalScopePin> ObligationGeometryPins, bool MetadataTruncated,
    string GeometryUsage = "DISPLAY_ONLY", string GeometryStatus = "AVAILABLE");
public sealed record RoutingFeature(Guid Id, RoutingGeometry Geometry, RoutingMapRouteProperties Properties,
    string Type = "Feature");
public sealed record RoutingUnavailableRoute(Guid RouteVersionId, Guid ProjectId, Guid RoadSectionId,
    IReadOnlyList<string> MissingReasons, string BboxRelation = "UNKNOWN");
public sealed record RoutingMapView(IReadOnlyList<RoutingFeature> Features,
    IReadOnlyList<RoutingUnavailableRoute> UnavailableRoutes, string? NextCursor, string SnapshotVersion,
    int ScannedRoutes, string Scope, string Type = "FeatureCollection");
public sealed record RoutingEvidencePin(Guid CaseId, string CaseVersion, Guid ReportId, string ReportVersion,
    Guid? EvidenceId, Guid? SupplementId, Guid? FileId, string? FileVersion, string CaseSourceVersion,
    decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters, string LocationSource,
    DateTimeOffset? CapturedAt, DateTimeOffset ReceivedAt, string CaptureTimeProvenance = "CLIENT_CLAIMED");
public sealed record RoutingCandidate(RoutingRouteProperties Route, double? DistanceMeters,
    string? DistanceMethod, string SpatialRelation, string UncertaintyStatus, string Responsibility,
    string TemporalBasis, DateTimeOffset ResponsibilityAt, IReadOnlyList<string> MissingReasons);
public sealed record RoutingCandidatesView(RoutingEvidencePin Source, string EvaluationVersion,
    string AlgorithmVersion, DateTimeOffset EvaluatedAt, IReadOnlyList<RoutingCandidate> Candidates,
    string? NextCursor, string Completeness, IReadOnlyList<string> MissingReasons, string SearchScope);
public sealed record RoutingSourceReference(string Kind, Guid Id, string Version);
public sealed record RoutingReportAttestation(Guid ReportId, Guid? EvidenceId, string LocationReason,
    string ResponsibilityReason, IReadOnlyList<RoutingSourceReference> SourceReferences);
public sealed record RoutingWholeCaseAttestation(string ScopeReason, string ConflictResolution,
    string TemporalBasis, IReadOnlyList<RoutingReportAttestation> Reports);
public sealed record CaseRoutingDecisionInput(string Outcome, string Reason, Guid ReportId, Guid? EvidenceId,
    string? RoutingEvaluationVersion = null, RoutingWholeCaseAttestation? Attestation = null);
public sealed record RoutingDecisionView(Guid DecisionId, string Outcome, string Reason, Guid ActorId,
    DateTimeOffset DecidedAt, Guid ReportId, Guid? EvidenceId, string CaseSourceVersion,
    string? RoutingEvaluationVersion, string TemporalBasis, Guid? SupersedesDecisionId,
    string State, RoutingWholeCaseAttestation? Attestation, RoutingEvidencePin? Source);
public sealed record RoutingDecisionSourceSnapshot(RoutingEvidencePin Evidence, RoutingWholeCaseAttestation? Attestation);
public sealed record RoutingDecisionPage(IReadOnlyList<RoutingDecisionView> Items, string? NextCursor);
public sealed record RoutingDispositionView(Guid DecisionId, string? Outcome, string State, DateTimeOffset DecidedAt);
public sealed record RoutingTriagePin(Guid ReportId, Guid? EvidenceId, Guid? RoadSectionId,
    string? RoutingEvaluationVersion, Guid? RouteVersionId = null, Guid? SegmentSetId = null, string? GeometryVersion = null);
public sealed record RoutingTriageSourceView(Guid EventId, RoutingTriagePin Source,
    string CaseSourceVersion, DateTimeOffset RecordedAt);
public sealed record RoutingWriteView(RoutingDecisionView Decision, string CaseVersion);
