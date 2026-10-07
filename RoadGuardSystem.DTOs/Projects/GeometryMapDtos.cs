namespace RoadGuardSystem.DTOs.Projects;

public sealed record GeometryMapLayer(string Key, string Status, string FeatureKind, int Count, string[] ReasonCodes,
    int? SpatialSrid = null, Guid? CrsProfileRevisionId = null, bool SampleOnly = false)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapLayerFact?(GeometryMapLayer? value)
        => value is null ? null! : new(value.Key, value.Status, value.FeatureKind, value.Count, value.ReasonCodes, value.SpatialSrid, value.CrsProfileRevisionId, value.SampleOnly);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapLayer?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapLayerFact? value)
        => value is null ? null! : new(value.Key, value.Status, value.FeatureKind, value.Count, value.ReasonCodes, value.SpatialSrid, value.CrsProfileRevisionId, value.SampleOnly);
}
public sealed record GeometryMapManifest(Guid PublicationId, Guid ProjectId, Guid RouteVersionId, Guid SegmentSetId,
    Guid LayoutRevisionId, Guid? CrsProfileRevisionId, int NativeSrid, string ProfileStatus, bool SampleOnly,
    double DisplayToleranceMeters, string ContentHash, GeometryMapLayer[] Layers,
    Guid? RouteSystemId = null, Guid? RoadSectionId = null, double? CanonicalLengthMeters = null,
    double? DeclaredLengthMeters = null, string? ChainageStatus = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapManifestFact?(GeometryMapManifest? value)
        => value is null ? null! : new(value.PublicationId, value.ProjectId, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.CrsProfileRevisionId, value.NativeSrid, value.ProfileStatus, value.SampleOnly, value.DisplayToleranceMeters, value.ContentHash, value.Layers?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapLayerFact)item).ToArray()!, value.RouteSystemId, value.RoadSectionId, value.CanonicalLengthMeters, value.DeclaredLengthMeters, value.ChainageStatus);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapManifest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapManifestFact? value)
        => value is null ? null! : new(value.PublicationId, value.ProjectId, value.RouteVersionId, value.SegmentSetId, value.LayoutRevisionId, value.CrsProfileRevisionId, value.NativeSrid, value.ProfileStatus, value.SampleOnly, value.DisplayToleranceMeters, value.ContentHash, value.Layers?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryMapLayer)item).ToArray()!, value.RouteSystemId, value.RoadSectionId, value.CanonicalLengthMeters, value.DeclaredLengthMeters, value.ChainageStatus);
}
public sealed record GeometryMapFeature(string Id, string Layer, GeometryShape Geometry, double[] Bbox, object Properties)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapFeatureFact?(GeometryMapFeature? value)
        => value is null ? null! : new(value.Id, value.Layer, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact)value.Geometry, value.Bbox, BoundaryFactMappings.ToFacts(value.Properties)!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapFeature?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapFeatureFact? value)
        => value is null ? null! : new(value.Id, value.Layer, (global::RoadGuardSystem.DTOs.Projects.GeometryShape)value.Geometry, value.Bbox, BoundaryFactMappings.ToWire(value.Properties)!);
}
public sealed record GeometryMapSnapshot(GeometryMapManifest Manifest, GeometryMapFeature[] Features)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapSnapshotFact?(GeometryMapSnapshot? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapManifestFact)value.Manifest, value.Features?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapFeatureFact)item).ToArray()!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapSnapshot?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapSnapshotFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Projects.GeometryMapManifest)value.Manifest, value.Features?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryMapFeature)item).ToArray()!);
}
public sealed record GeometryMapPage(Guid PublicationId, string ContentHash, string Layer,
    GeometryMapFeature[] Features, string? NextCursor)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapPageFact?(GeometryMapPage? value)
        => value is null ? null! : new(value.PublicationId, value.ContentHash, value.Layer, value.Features?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapFeatureFact)item).ToArray()!, value.NextCursor);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapPage?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapPageFact? value)
        => value is null ? null! : new(value.PublicationId, value.ContentHash, value.Layer, value.Features?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryMapFeature)item).ToArray()!, value.NextCursor);
}
public sealed record GeometryMapPublishInput(Guid LayoutRevisionId, string PublicationMode, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapPublishInputFact?(GeometryMapPublishInput? value)
        => value is null ? null! : new(value.LayoutRevisionId, value.PublicationMode, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryMapPublishInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryMapPublishInputFact? value)
        => value is null ? null! : new(value.LayoutRevisionId, value.PublicationMode, value.Reason);
}
public sealed record GeometryImpactInput(Guid PreviousRouteVersionId, Guid NewRouteVersionId, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryImpactInputFact?(GeometryImpactInput? value)
        => value is null ? null! : new(value.PreviousRouteVersionId, value.NewRouteVersionId, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryImpactInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryImpactInputFact? value)
        => value is null ? null! : new(value.PreviousRouteVersionId, value.NewRouteVersionId, value.Reason);
}
public sealed record GeometryImpactDecisionInput(Guid TaskId, string Action, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryImpactDecisionInputFact?(GeometryImpactDecisionInput? value)
        => value is null ? null! : new(value.TaskId, value.Action, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryImpactDecisionInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryImpactDecisionInputFact? value)
        => value is null ? null! : new(value.TaskId, value.Action, value.Reason);
}
public sealed record GeometryAffectedReference(string Kind, Guid Id, Guid RouteVersionId, Guid? SegmentSetId,
    string Readiness, string[] MissingReasonCodes)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryAffectedReferenceFact?(GeometryAffectedReference? value)
        => value is null ? null! : new(value.Kind, value.Id, value.RouteVersionId, value.SegmentSetId, value.Readiness, value.MissingReasonCodes);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryAffectedReference?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryAffectedReferenceFact? value)
        => value is null ? null! : new(value.Kind, value.Id, value.RouteVersionId, value.SegmentSetId, value.Readiness, value.MissingReasonCodes);
}
