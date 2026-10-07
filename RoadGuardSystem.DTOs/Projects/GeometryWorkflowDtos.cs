namespace RoadGuardSystem.DTOs.Projects;

public sealed record GeometryPoint(double X, double Y)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact?(GeometryPoint? value)
        => value is null ? null! : new(value.X, value.Y);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryPoint?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact? value)
        => value is null ? null! : new(value.X, value.Y);
}
public sealed record WidthProfileItem(double FromOffsetMeters, double ToOffsetMeters, double WidthMeters)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.WidthProfileItemFact?(WidthProfileItem? value)
        => value is null ? null! : new(value.FromOffsetMeters, value.ToOffsetMeters, value.WidthMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator WidthProfileItem?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.WidthProfileItemFact? value)
        => value is null ? null! : new(value.FromOffsetMeters, value.ToOffsetMeters, value.WidthMeters);
}
public sealed record GeometryDraftInput(string SourceKind, int SourceCrs, double StationOriginMeters,
    string ChangeReason, GeometryPoint[]? Coordinates, Guid? SourceFileId, int? TrackIndex,
    int? TrackSegmentIndex, WidthProfileItem[] WidthProfile, double SurveyWidthMeters,
    string? BranchCode = null, string? RoadCode = null, string? RoadName = null,
    NativeAlignmentInput? NativeAlignment = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null, double? TessellationToleranceMeters = null,
    CrsProfileInput? ResolvedCrsProfile = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftInputFact?(GeometryDraftInput? value)
        => value is null ? null! : new(value.SourceKind, value.SourceCrs, value.StationOriginMeters, value.ChangeReason, value.Coordinates?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)item).ToArray(), value.SourceFileId, value.TrackIndex, value.TrackSegmentIndex, value.WidthProfile?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.WidthProfileItemFact)item).ToArray()!, value.SurveyWidthMeters, value.BranchCode, value.RoadCode, value.RoadName, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentInputFact?)value.NativeAlignment, value.RouteSystemId, value.RouteKind, value.ParentRouteVersionId, value.JunctionOffsetMeters, value.DeclaredLengthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageCalibrationInputFact?)value.ChainageCalibration, value.TessellationToleranceMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileInputFact?)value.ResolvedCrsProfile);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryDraftInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftInputFact? value)
        => value is null ? null! : new(value.SourceKind, value.SourceCrs, value.StationOriginMeters, value.ChangeReason, value.Coordinates?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)item).ToArray(), value.SourceFileId, value.TrackIndex, value.TrackSegmentIndex, value.WidthProfile?.Select(item => (global::RoadGuardSystem.DTOs.Projects.WidthProfileItem)item).ToArray()!, value.SurveyWidthMeters, value.BranchCode, value.RoadCode, value.RoadName, (global::RoadGuardSystem.DTOs.Projects.NativeAlignmentInput?)value.NativeAlignment, value.RouteSystemId, value.RouteKind, value.ParentRouteVersionId, value.JunctionOffsetMeters, value.DeclaredLengthMeters, (global::RoadGuardSystem.DTOs.Projects.ChainageCalibrationInput?)value.ChainageCalibration, value.TessellationToleranceMeters, (global::RoadGuardSystem.DTOs.Projects.CrsProfileInput?)value.ResolvedCrsProfile);
}
public sealed record GeometryDraftView(Guid Id, Guid ProjectId, Guid? RoadSectionId, string RoadCode,
    string? RoadName, string Status, string SourceKind, int SourceCrs, Guid? SourceFileId,
    string? SourceChecksum, int? TrackIndex, int? TrackSegmentIndex, GeometryPoint[] OriginalCoordinates,
    GeometryPoint[] EditedCoordinates, double StationOriginMeters, WidthProfileItem[] WidthProfile,
    double SurveyWidthMeters, Guid CreatedBy, Guid UpdatedBy, string Version,
    NativeAlignmentInput? NativeAlignment = null, GeometryDraftReadiness? Readiness = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftViewFact?(GeometryDraftView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.RoadSectionId, value.RoadCode, value.RoadName, value.Status, value.SourceKind, value.SourceCrs, value.SourceFileId, value.SourceChecksum, value.TrackIndex, value.TrackSegmentIndex, value.OriginalCoordinates?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)item).ToArray()!, value.EditedCoordinates?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)item).ToArray()!, value.StationOriginMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.WidthProfileItemFact)item).ToArray()!, value.SurveyWidthMeters, value.CreatedBy, value.UpdatedBy, value.Version, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentInputFact?)value.NativeAlignment, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftReadinessFact?)value.Readiness);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryDraftView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.RoadSectionId, value.RoadCode, value.RoadName, value.Status, value.SourceKind, value.SourceCrs, value.SourceFileId, value.SourceChecksum, value.TrackIndex, value.TrackSegmentIndex, value.OriginalCoordinates?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)item).ToArray()!, value.EditedCoordinates?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)item).ToArray()!, value.StationOriginMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.DTOs.Projects.WidthProfileItem)item).ToArray()!, value.SurveyWidthMeters, value.CreatedBy, value.UpdatedBy, value.Version, (global::RoadGuardSystem.DTOs.Projects.NativeAlignmentInput?)value.NativeAlignment, (global::RoadGuardSystem.DTOs.Projects.GeometryDraftReadiness?)value.Readiness);
}
public sealed record GeometryShape(string Type, object Coordinates)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?(GeometryShape? value)
        => value is null ? null! : new(value.Type, BoundaryFactMappings.ToFacts(value.Coordinates)!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryShape?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact? value)
        => value is null ? null! : new(value.Type, BoundaryFactMappings.ToWire(value.Coordinates)!);
}
public sealed record GeometryPreview(int SourceCrs, int EngineeringSrid, double LengthMeters,
    GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline, GeometryShape RoadSurface,
    GeometryShape SurveyArea, string[] Warnings, Guid? CrsProfileRevisionId = null, bool SampleOnly = false)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPreviewFact?(GeometryPreview? value)
        => value is null ? null! : new(value.SourceCrs, value.EngineeringSrid, value.LengthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.MetricCenterline, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?)value.Wgs84Centerline, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.RoadSurface, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.SurveyArea, value.Warnings, value.CrsProfileRevisionId, value.SampleOnly);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryPreview?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPreviewFact? value)
        => value is null ? null! : new(value.SourceCrs, value.EngineeringSrid, value.LengthMeters, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.MetricCenterline, (global::RoadGuardSystem.DTOs.Projects.GeometryShape?)value.Wgs84Centerline, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.RoadSurface, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.SurveyArea, value.Warnings, value.CrsProfileRevisionId, value.SampleOnly);
}
public sealed record RoadGeometryVersionView(Guid ProjectId, Guid RoadSectionId, Guid RouteVersionId,
    int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom, Guid? SourceDraftId, int? SourceCrs,
    int EngineeringSrid, double? StationOriginMeters, WidthProfileItem[]? WidthProfile,
    double? SurveyWidthMeters, GeometryShape MetricCenterline, GeometryShape? Wgs84Centerline,
    double LengthMeters, string MetadataStatus, Guid? ApprovedBy, DateTimeOffset? ApprovedAt, string GeometryHash,
    Guid? CrsProfileRevisionId = null, Guid? RouteSystemId = null, string? RouteKind = null,
    Guid? ParentRouteVersionId = null, double? JunctionOffsetMeters = null, double? DeclaredLengthMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null, string? CrsStatus = null, bool SampleOnly = false)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RoadGeometryVersionViewFact?(RoadGeometryVersionView? value)
        => value is null ? null! : new(value.ProjectId, value.RoadSectionId, value.RouteVersionId, value.VersionNo, value.IsCurrent, value.EffectiveFrom, value.SourceDraftId, value.SourceCrs, value.EngineeringSrid, value.StationOriginMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.WidthProfileItemFact)item).ToArray(), value.SurveyWidthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.MetricCenterline, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?)value.Wgs84Centerline, value.LengthMeters, value.MetadataStatus, value.ApprovedBy, value.ApprovedAt, value.GeometryHash, value.CrsProfileRevisionId, value.RouteSystemId, value.RouteKind, value.ParentRouteVersionId, value.JunctionOffsetMeters, value.DeclaredLengthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageCalibrationInputFact?)value.ChainageCalibration, value.CrsStatus, value.SampleOnly);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RoadGeometryVersionView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RoadGeometryVersionViewFact? value)
        => value is null ? null! : new(value.ProjectId, value.RoadSectionId, value.RouteVersionId, value.VersionNo, value.IsCurrent, value.EffectiveFrom, value.SourceDraftId, value.SourceCrs, value.EngineeringSrid, value.StationOriginMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.DTOs.Projects.WidthProfileItem)item).ToArray(), value.SurveyWidthMeters, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.MetricCenterline, (global::RoadGuardSystem.DTOs.Projects.GeometryShape?)value.Wgs84Centerline, value.LengthMeters, value.MetadataStatus, value.ApprovedBy, value.ApprovedAt, value.GeometryHash, value.CrsProfileRevisionId, value.RouteSystemId, value.RouteKind, value.ParentRouteVersionId, value.JunctionOffsetMeters, value.DeclaredLengthMeters, (global::RoadGuardSystem.DTOs.Projects.ChainageCalibrationInput?)value.ChainageCalibration, value.CrsStatus, value.SampleOnly);
}
public sealed record GeometryConfirmInput(Guid? ExpectedCurrentVersionId, DateTimeOffset EffectiveFrom, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryConfirmInputFact?(GeometryConfirmInput? value)
        => value is null ? null! : new(value.ExpectedCurrentVersionId, value.EffectiveFrom, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryConfirmInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryConfirmInputFact? value)
        => value is null ? null! : new(value.ExpectedCurrentVersionId, value.EffectiveFrom, value.Reason);
}
public sealed record SegmentDefinition(double TargetLengthMeters = 100, string RemainderMode = "KEEP", double[]? BoundariesMeters = null,
    NativeAlignmentInput? NativeAlignment = null, double? TessellationToleranceMeters = null,
    ChainageCalibrationInput? ChainageCalibration = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentDefinitionFact?(SegmentDefinition? value)
        => value is null ? null! : new(value.TargetLengthMeters, value.RemainderMode, value.BoundariesMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentInputFact?)value.NativeAlignment, value.TessellationToleranceMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageCalibrationInputFact?)value.ChainageCalibration);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentDefinition?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentDefinitionFact? value)
        => value is null ? null! : new(value.TargetLengthMeters, value.RemainderMode, value.BoundariesMeters, (global::RoadGuardSystem.DTOs.Projects.NativeAlignmentInput?)value.NativeAlignment, value.TessellationToleranceMeters, (global::RoadGuardSystem.DTOs.Projects.ChainageCalibrationInput?)value.ChainageCalibration);
}
public sealed record SegmentGeometry(Guid? Id, int Sequence, double FromOffsetMeters, double ToOffsetMeters,
    double StartStationMeters, double EndStationMeters, double LengthMeters, GeometryShape MetricGeometry, GeometryShape? Wgs84Geometry)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryFact?(SegmentGeometry? value)
        => value is null ? null! : new(value.Id, value.Sequence, value.FromOffsetMeters, value.ToOffsetMeters, value.StartStationMeters, value.EndStationMeters, value.LengthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.MetricGeometry, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?)value.Wgs84Geometry);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentGeometry?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryFact? value)
        => value is null ? null! : new(value.Id, value.Sequence, value.FromOffsetMeters, value.ToOffsetMeters, value.StartStationMeters, value.EndStationMeters, value.LengthMeters, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.MetricGeometry, (global::RoadGuardSystem.DTOs.Projects.GeometryShape?)value.Wgs84Geometry);
}
public sealed record SegmentPreview(Guid RouteVersionId, double TotalLengthMeters, double[] BoundariesMeters,
    SegmentGeometry[] Segments, string DefinitionHash)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPreviewFact?(SegmentPreview? value)
        => value is null ? null! : new(value.RouteVersionId, value.TotalLengthMeters, value.BoundariesMeters, value.Segments?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryFact)item).ToArray()!, value.DefinitionHash);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentPreview?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPreviewFact? value)
        => value is null ? null! : new(value.RouteVersionId, value.TotalLengthMeters, value.BoundariesMeters, value.Segments?.Select(item => (global::RoadGuardSystem.DTOs.Projects.SegmentGeometry)item).ToArray()!, value.DefinitionHash);
}
public sealed record SegmentGeometryView(Guid Id, int Sequence, double? FromOffsetMeters, double? ToOffsetMeters,
    double? StartStationMeters, double? EndStationMeters, double? LengthMeters, GeometryShape? MetricGeometry,
    GeometryShape? Wgs84Geometry, string[] MissingMetadata)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryViewFact?(SegmentGeometryView? value)
        => value is null ? null! : new(value.Id, value.Sequence, value.FromOffsetMeters, value.ToOffsetMeters, value.StartStationMeters, value.EndStationMeters, value.LengthMeters, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?)value.MetricGeometry, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact?)value.Wgs84Geometry, value.MissingMetadata);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentGeometryView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryViewFact? value)
        => value is null ? null! : new(value.Id, value.Sequence, value.FromOffsetMeters, value.ToOffsetMeters, value.StartStationMeters, value.EndStationMeters, value.LengthMeters, (global::RoadGuardSystem.DTOs.Projects.GeometryShape?)value.MetricGeometry, (global::RoadGuardSystem.DTOs.Projects.GeometryShape?)value.Wgs84Geometry, value.MissingMetadata);
}
public sealed record SegmentSetView(Guid Id, Guid RouteVersionId, string Status, SegmentDefinition? Definition,
    string? GeometryHash, SegmentGeometryView[] Segments, Guid? PublishedBy, DateTimeOffset? PublishedAt, string Version,
    string MetadataStatus, string[] MissingMetadata)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentSetViewFact?(SegmentSetView? value)
        => value is null ? null! : new(value.Id, value.RouteVersionId, value.Status, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentDefinitionFact?)value.Definition, value.GeometryHash, value.Segments?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryViewFact)item).ToArray()!, value.PublishedBy, value.PublishedAt, value.Version, value.MetadataStatus, value.MissingMetadata);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentSetView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentSetViewFact? value)
        => value is null ? null! : new(value.Id, value.RouteVersionId, value.Status, (global::RoadGuardSystem.DTOs.Projects.SegmentDefinition?)value.Definition, value.GeometryHash, value.Segments?.Select(item => (global::RoadGuardSystem.DTOs.Projects.SegmentGeometryView)item).ToArray()!, value.PublishedBy, value.PublishedAt, value.Version, value.MetadataStatus, value.MissingMetadata);
}
public sealed record SegmentPublishInput(Guid? ExpectedPublishedSetId, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPublishInputFact?(SegmentPublishInput? value)
        => value is null ? null! : new(value.ExpectedPublishedSetId, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SegmentPublishInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPublishInputFact? value)
        => value is null ? null! : new(value.ExpectedPublishedSetId, value.Reason);
}
public sealed record GeometryPackageView(string SchemaVersion, Guid ProjectId, RoadGeometryVersionView Route, SegmentSetView SegmentSet)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPackageViewFact?(GeometryPackageView? value)
        => value is null ? null! : new(value.SchemaVersion, value.ProjectId, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RoadGeometryVersionViewFact)value.Route, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentSetViewFact)value.SegmentSet);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryPackageView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPackageViewFact? value)
        => value is null ? null! : new(value.SchemaVersion, value.ProjectId, (global::RoadGuardSystem.DTOs.Projects.RoadGeometryVersionView)value.Route, (global::RoadGuardSystem.DTOs.Projects.SegmentSetView)value.SegmentSet);
}
