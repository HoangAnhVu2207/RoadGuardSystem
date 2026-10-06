namespace RoadGuardSystem.DTOs.Projects;

public sealed record GeometryMapLayer(string Key, string Status, string FeatureKind, int Count, string[] ReasonCodes,
    int? SpatialSrid = null, Guid? CrsProfileRevisionId = null, bool SampleOnly = false);
public sealed record GeometryMapManifest(Guid PublicationId, Guid ProjectId, Guid RouteVersionId, Guid SegmentSetId,
    Guid LayoutRevisionId, Guid? CrsProfileRevisionId, int NativeSrid, string ProfileStatus, bool SampleOnly,
    double DisplayToleranceMeters, string ContentHash, GeometryMapLayer[] Layers,
    Guid? RouteSystemId = null, Guid? RoadSectionId = null, double? CanonicalLengthMeters = null,
    double? DeclaredLengthMeters = null, string? ChainageStatus = null);
public sealed record GeometryMapFeature(string Id, string Layer, GeometryShape Geometry, double[] Bbox, object Properties);
public sealed record GeometryMapSnapshot(GeometryMapManifest Manifest, GeometryMapFeature[] Features);
public sealed record GeometryMapPage(Guid PublicationId, string ContentHash, string Layer,
    GeometryMapFeature[] Features, string? NextCursor);
public sealed record GeometryMapPublishInput(Guid LayoutRevisionId, string PublicationMode, string Reason);
public sealed record GeometryImpactInput(Guid PreviousRouteVersionId, Guid NewRouteVersionId, string Reason);
public sealed record GeometryImpactDecisionInput(Guid TaskId, string Action, string Reason);
public sealed record GeometryAffectedReference(string Kind, Guid Id, Guid RouteVersionId, Guid? SegmentSetId,
    string Readiness, string[] MissingReasonCodes);
