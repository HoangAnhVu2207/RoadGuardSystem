using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record AsBuiltLayoutInputFact(Guid SourcePlanId, AsBuiltSlabInputFact[] Slabs, string Reason);

public sealed record AsBuiltSlabInputFact(string Key, int? PlannedSequence, GeometryPointFact[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null);

public sealed record ChainageCalibrationInputFact(string SourceReference, string SourceChecksum,
    ChainageControlFact[] Controls, string SelectedReason);

public sealed record ChainageControlFact(double GeometricOffsetMeters, double StationMeters);

public sealed record CrsControlPointFact(GeometryPointFact Native, GeometryPointFact Wgs84, string IndependentSource);

public sealed record CrsEllipsoidDefinitionFact(double SemiMajorAxisMeters, double InverseFlattening, string SourceReference);

public sealed record CrsProfileInputFact(string Code, int SourceSrid, string Datum, string Projection,
    string AxisOrder, double MetresPerUnit, string SourceReference, string SourceChecksum,
    CrsTransformOperationFact? Operation = null, CrsControlPointFact[]? IndependentControls = null,
    double? SourcedToleranceMeters = null, string? ToleranceSource = null, bool SampleOnly = true,
    CrsEllipsoidDefinitionFact? Ellipsoid = null, CrsProjectionDefinitionFact? ProjectionDefinition = null);

public sealed record CrsProfileViewFact(Guid Id, Guid ProjectId, string Code, int Revision, string Status,
    CrsProfileInputFact Definition, Guid CreatedBy, DateTimeOffset CreatedAt);

public sealed record CrsProjectionDefinitionFact(IReadOnlyDictionary<string, double> Parameters, string SourceReference);

public sealed record CrsTransformOperationFact(string Method, string Convention, string Direction, double[] Parameters,
    string SourceReference, string? AreaOfUse = null, double? PublishedAccuracyMeters = null, string? AccuracySource = null);

public sealed record GeometryAffectedReferenceFact(string Kind, Guid Id, Guid RouteVersionId, Guid? SegmentSetId,
    string Readiness, string[] MissingReasonCodes);

public sealed record GeometryConfirmInputFact(Guid? ExpectedCurrentVersionId, DateTimeOffset EffectiveFrom, string Reason);

public sealed record GeometryDraftInputFact(string SourceKind, int SourceCrs, double StationOriginMeters,
    string ChangeReason, GeometryPointFact[]? Coordinates, Guid? SourceFileId, int? TrackIndex,
    int? TrackSegmentIndex, WidthProfileItemFact[] WidthProfile, double SurveyWidthMeters,
    string? BranchCode = null, string? RoadCode = null, string? RoadName = null,
    NativeAlignmentInputFact? NativeAlignment = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInputFact? ChainageCalibration = null, double? TessellationToleranceMeters = null,
    CrsProfileInputFact? ResolvedCrsProfile = null);

public sealed record GeometryDraftReadinessFact(bool Ready, string[] MissingFields, string[] Errors,
    string CrsStatus, bool SampleOnly, string[] Warnings);

public sealed record GeometryDraftViewFact(Guid Id, Guid ProjectId, Guid? RoadSectionId, string RoadCode,
    string? RoadName, string Status, string SourceKind, int SourceCrs, Guid? SourceFileId,
    string? SourceChecksum, int? TrackIndex, int? TrackSegmentIndex, GeometryPointFact[] OriginalCoordinates,
    GeometryPointFact[] EditedCoordinates, double StationOriginMeters, WidthProfileItemFact[] WidthProfile,
    double SurveyWidthMeters, Guid CreatedBy, Guid UpdatedBy, string Version,
    NativeAlignmentInputFact? NativeAlignment = null, GeometryDraftReadinessFact? Readiness = null);

public sealed record GeometryImpactDecisionInputFact(Guid TaskId, string Action, string Reason);

public sealed record GeometryImpactInputFact(Guid PreviousRouteVersionId, Guid NewRouteVersionId, string Reason);

public sealed record GeometryMapFeatureFact(string Id, string Layer, GeometryShapeFact Geometry, double[] Bbox, object Properties);

public sealed record GeometryMapLayerFact(string Key, string Status, string FeatureKind, int Count, string[] ReasonCodes,
    int? SpatialSrid = null, Guid? CrsProfileRevisionId = null, bool SampleOnly = false);

public sealed record GeometryMapManifestFact(Guid PublicationId, Guid ProjectId, Guid RouteVersionId, Guid SegmentSetId,
    Guid LayoutRevisionId, Guid? CrsProfileRevisionId, int NativeSrid, string ProfileStatus, bool SampleOnly,
    double DisplayToleranceMeters, string ContentHash, GeometryMapLayerFact[] Layers,
    Guid? RouteSystemId = null, Guid? RoadSectionId = null, double? CanonicalLengthMeters = null,
    double? DeclaredLengthMeters = null, string? ChainageStatus = null);

public sealed record GeometryMapPageFact(Guid PublicationId, string ContentHash, string Layer,
    GeometryMapFeatureFact[] Features, string? NextCursor);

public sealed record GeometryMapPublishInputFact(Guid LayoutRevisionId, string PublicationMode, string Reason);

public sealed record GeometryMapSnapshotFact(GeometryMapManifestFact Manifest, GeometryMapFeatureFact[] Features);

public sealed record GeometryPackageViewFact(string SchemaVersion, Guid ProjectId, RoadGeometryVersionViewFact Route, SegmentSetViewFact SegmentSet);

public sealed record GeometryPointFact(double X, double Y);

public sealed record GeometryPreviewFact(int SourceCrs, int EngineeringSrid, double LengthMeters,
    GeometryShapeFact MetricCenterline, GeometryShapeFact? Wgs84Centerline, GeometryShapeFact RoadSurface,
    GeometryShapeFact SurveyArea, string[] Warnings, Guid? CrsProfileRevisionId = null, bool SampleOnly = false);

public sealed record GeometryShapeFact(string Type, object Coordinates);

public sealed record NativeAlignmentInputFact(Guid CrsProfileRevisionId, NativeAlignmentPrimitiveFact[] Primitives, int SpatialSrid = 0);

public sealed record NativeAlignmentPrimitiveFact(string Kind, GeometryPointFact Start, GeometryPointFact End,
    GeometryPointFact? Center = null, double? Radius = null, double? StartAngleRadians = null, double? SweepRadians = null);

public sealed record PavementGeometryPreviewFact(PavementLayoutPreviewFact? Plan, SlabGeometrySnapshotFact[] Slabs,
    string[] Warnings, double DisplayToleranceMeters, double RenderedGapAreaSquareMeters = 0,
    double RenderedOverlapAreaSquareMeters = 0);

public sealed record PavementLayoutInputFact(decimal StripWidthMeters, decimal SlabLengthMeters, PavementWidthIntervalFact[] WidthProfile);

public sealed record PavementLayoutPreviewFact(decimal AnalyticLengthMeters, decimal StripWidthMeters,
    decimal SlabLengthMeters, PlannedSlabCellFact[] Cells, string[] Warnings);

public sealed record PavementPlanCreateInputFact(PavementLayoutInputFact Layout, double DisplayToleranceMeters);

public sealed record PavementWidthIntervalFact(decimal FromOffsetMeters, decimal ToOffsetMeters, decimal WidthMeters);

public sealed record PlannedSlabCellFact(int Sequence, int LongitudinalRow, int Strip, decimal FromOffsetMeters,
    decimal ToOffsetMeters, decimal FromLateralMeters, decimal ToLateralMeters, bool IsTerminalResidual,
    bool IsWidthTransitionFragment);

public sealed record RoadGeometryVersionViewFact(Guid ProjectId, Guid RoadSectionId, Guid RouteVersionId,
    int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, Guid? SourceDraftId, int? SourceCrs,
    int EngineeringSrid, double? StationOriginMeters, WidthProfileItemFact[]? WidthProfile,
    double? SurveyWidthMeters, GeometryShapeFact MetricCenterline, GeometryShapeFact? Wgs84Centerline,
    double LengthMeters, string MetadataStatus, Guid? ApprovedBy, DateTimeOffset? ApprovedAt, string GeometryHash,
    Guid? CrsProfileRevisionId = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInputFact? ChainageCalibration = null, string? CrsStatus = null, bool SampleOnly = false);

public sealed record RouteSystemInputFact(string Code, string Name);

public sealed record RouteSystemViewFact(Guid Id, Guid ProjectId, string Code, string Name);

public sealed record SegmentDefinitionFact(double TargetLengthMeters = 100, string RemainderMode = "KEEP", double[]? BoundariesMeters = null,
    NativeAlignmentInputFact? NativeAlignment = null, double? TessellationToleranceMeters = null,
    ChainageCalibrationInputFact? ChainageCalibration = null);

public sealed record SegmentGeometryFact(Guid? Id, int Sequence, double FromOffsetMeters, double ToOffsetMeters,
    double StartStationMeters, double EndStationMeters, double LengthMeters, GeometryShapeFact MetricGeometry, GeometryShapeFact? Wgs84Geometry);

public sealed record SegmentGeometryViewFact(Guid Id, int Sequence, double? FromOffsetMeters, double? ToOffsetMeters,
    double? StartStationMeters, double? EndStationMeters, double? LengthMeters, GeometryShapeFact? MetricGeometry,
    GeometryShapeFact? Wgs84Geometry, string[] MissingMetadata);

public sealed record SegmentPreviewFact(Guid RouteVersionId, double TotalLengthMeters, double[] BoundariesMeters,
    SegmentGeometryFact[] Segments, string DefinitionHash);

public sealed record SegmentPublishInputFact(Guid? ExpectedPublishedSetId, string Reason);

public sealed record SegmentSetViewFact(Guid Id, Guid RouteVersionId, string Status, SegmentDefinitionFact? Definition,
    string? GeometryHash, SegmentGeometryViewFact[] Segments, Guid? PublishedBy, DateTimeOffset? PublishedAt, string Version,
    string MetadataStatus, string[] MissingMetadata);

public sealed record SlabGeometrySnapshotFact(string Key, int? PlannedSequence, GeometryPointFact[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null, string ProvenanceStatus = "CLAIMED_CUSTOM");

public sealed record WidthProfileItemFact(double FromOffsetMeters, double ToOffsetMeters, double WidthMeters);
