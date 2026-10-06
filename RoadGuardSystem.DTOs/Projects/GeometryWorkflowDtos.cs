namespace RoadGuardSystem.DTOs.Projects;

public sealed record GeometryPoint(double X, double Y);
public sealed record WidthProfileItem(double FromOffsetMeters, double ToOffsetMeters, double WidthMeters);
public sealed record GeometryDraftInput(string SourceKind, int SourceCrs, double StationOriginMeters,
    string ChangeReason, GeometryPoint[]? Coordinates, Guid? SourceFileId, int? TrackIndex,
    int? TrackSegmentIndex, WidthProfileItem[] WidthProfile, double SurveyWidthMeters,
    string? BranchCode = null, string? RoadCode = null, string? RoadName = null,
    NativeAlignmentInput? NativeAlignment = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null, double? TessellationToleranceMeters = null,
    CrsProfileInput? ResolvedCrsProfile = null);
public sealed record GeometryDraftView(Guid Id, Guid ProjectId, Guid? RoadSectionId, string RoadCode,
    string? RoadName, string Status, string SourceKind, int SourceCrs, Guid? SourceFileId,
    string? SourceChecksum, int? TrackIndex, int? TrackSegmentIndex, GeometryPoint[] OriginalCoordinates,
    GeometryPoint[] EditedCoordinates, double StationOriginMeters, WidthProfileItem[] WidthProfile,
    double SurveyWidthMeters, Guid CreatedBy, Guid UpdatedBy, string Version,
    NativeAlignmentInput? NativeAlignment = null, GeometryDraftReadiness? Readiness = null);
public sealed record GeometryShape(string Type, object Coordinates);
public sealed record GeometryPreview(int SourceCrs, int EngineeringSrid, double LengthMeters,
    GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline, GeometryShape RoadSurface,
    GeometryShape SurveyArea, string[] Warnings, Guid? CrsProfileRevisionId = null, bool SampleOnly = false);
public sealed record RoadGeometryVersionView(Guid ProjectId, Guid RoadSectionId, Guid RouteVersionId,
    int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, Guid? SourceDraftId, int? SourceCrs,
    int EngineeringSrid, double? StationOriginMeters, WidthProfileItem[]? WidthProfile,
    double? SurveyWidthMeters, GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline,
    double LengthMeters, string MetadataStatus, Guid? ApprovedBy, DateTimeOffset? ApprovedAt, string GeometryHash,
    Guid? CrsProfileRevisionId = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null, string? CrsStatus = null, bool SampleOnly = false);
public sealed record GeometryConfirmInput(Guid? ExpectedCurrentVersionId, DateTimeOffset EffectiveFrom, string Reason);
public sealed record SegmentDefinition(double TargetLengthMeters = 100, string RemainderMode = "KEEP", double[]? BoundariesMeters = null,
    NativeAlignmentInput? NativeAlignment = null, double? TessellationToleranceMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null);
public sealed record SegmentGeometry(Guid? Id, int Sequence, double FromOffsetMeters, double ToOffsetMeters,
    double StartStationMeters, double EndStationMeters, double LengthMeters, GeometryShape MetricGeometry, GeometryShape? Wgs84Geometry);
public sealed record SegmentPreview(Guid RouteVersionId, double TotalLengthMeters, double[] BoundariesMeters,
    SegmentGeometry[] Segments, string DefinitionHash);
public sealed record SegmentGeometryView(Guid Id, int Sequence, double? FromOffsetMeters, double? ToOffsetMeters,
    double? StartStationMeters, double? EndStationMeters, double? LengthMeters, GeometryShape? MetricGeometry,
    GeometryShape? Wgs84Geometry, string[] MissingMetadata);
public sealed record SegmentSetView(Guid Id, Guid RouteVersionId, string Status, SegmentDefinition? Definition,
    string? GeometryHash, SegmentGeometryView[] Segments, Guid? PublishedBy, DateTimeOffset? PublishedAt, string Version,
    string MetadataStatus, string[] MissingMetadata);
public sealed record SegmentPublishInput(Guid? ExpectedPublishedSetId, string Reason);
public sealed record GeometryPackageView(string SchemaVersion, Guid ProjectId, RoadGeometryVersionView Route, SegmentSetView SegmentSet);
