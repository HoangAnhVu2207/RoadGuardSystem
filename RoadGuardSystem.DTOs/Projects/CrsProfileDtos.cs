namespace RoadGuardSystem.DTOs.Projects;

public sealed record CrsControlPoint(GeometryPoint Native, GeometryPoint Wgs84, string IndependentSource)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsControlPointFact?(CrsControlPoint? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)value.Native, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)value.Wgs84, value.IndependentSource);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsControlPoint?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsControlPointFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Projects.GeometryPoint)value.Native, (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)value.Wgs84, value.IndependentSource);
}
public sealed record CrsEllipsoidDefinition(double SemiMajorAxisMeters, double InverseFlattening, string SourceReference)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsEllipsoidDefinitionFact?(CrsEllipsoidDefinition? value)
        => value is null ? null! : new(value.SemiMajorAxisMeters, value.InverseFlattening, value.SourceReference);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsEllipsoidDefinition?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsEllipsoidDefinitionFact? value)
        => value is null ? null! : new(value.SemiMajorAxisMeters, value.InverseFlattening, value.SourceReference);
}
public sealed record CrsProjectionDefinition(IReadOnlyDictionary<string, double> Parameters, string SourceReference)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProjectionDefinitionFact?(CrsProjectionDefinition? value)
        => value is null ? null! : new(value.Parameters, value.SourceReference);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsProjectionDefinition?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProjectionDefinitionFact? value)
        => value is null ? null! : new(value.Parameters, value.SourceReference);
}
public sealed record CrsTransformOperation(string Method, string Convention, string Direction, double[] Parameters,
    string SourceReference, string? AreaOfUse = null, double? PublishedAccuracyMeters = null, string? AccuracySource = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsTransformOperationFact?(CrsTransformOperation? value)
        => value is null ? null! : new(value.Method, value.Convention, value.Direction, value.Parameters, value.SourceReference, value.AreaOfUse, value.PublishedAccuracyMeters, value.AccuracySource);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsTransformOperation?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsTransformOperationFact? value)
        => value is null ? null! : new(value.Method, value.Convention, value.Direction, value.Parameters, value.SourceReference, value.AreaOfUse, value.PublishedAccuracyMeters, value.AccuracySource);
}
public sealed record CrsProfileInput(string Code, int SourceSrid, string Datum, string Projection,
    string AxisOrder, double MetresPerUnit, string SourceReference, string SourceChecksum,
    CrsTransformOperation? Operation = null, CrsControlPoint[]? IndependentControls = null,
    double? SourcedToleranceMeters = null, string? ToleranceSource = null, bool SampleOnly = true,
    CrsEllipsoidDefinition? Ellipsoid = null, CrsProjectionDefinition? ProjectionDefinition = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileInputFact?(CrsProfileInput? value)
        => value is null ? null! : new(value.Code, value.SourceSrid, value.Datum, value.Projection, value.AxisOrder, value.MetresPerUnit, value.SourceReference, value.SourceChecksum, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsTransformOperationFact?)value.Operation, value.IndependentControls?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsControlPointFact)item).ToArray(), value.SourcedToleranceMeters, value.ToleranceSource, value.SampleOnly, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsEllipsoidDefinitionFact?)value.Ellipsoid, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProjectionDefinitionFact?)value.ProjectionDefinition);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsProfileInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileInputFact? value)
        => value is null ? null! : new(value.Code, value.SourceSrid, value.Datum, value.Projection, value.AxisOrder, value.MetresPerUnit, value.SourceReference, value.SourceChecksum, (global::RoadGuardSystem.DTOs.Projects.CrsTransformOperation?)value.Operation, value.IndependentControls?.Select(item => (global::RoadGuardSystem.DTOs.Projects.CrsControlPoint)item).ToArray(), value.SourcedToleranceMeters, value.ToleranceSource, value.SampleOnly, (global::RoadGuardSystem.DTOs.Projects.CrsEllipsoidDefinition?)value.Ellipsoid, (global::RoadGuardSystem.DTOs.Projects.CrsProjectionDefinition?)value.ProjectionDefinition);
}
public sealed record CrsProfileView(Guid Id, Guid ProjectId, string Code, int Revision, string Status,
    CrsProfileInput Definition, Guid CreatedBy, DateTimeOffset CreatedAt)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileViewFact?(CrsProfileView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Code, value.Revision, value.Status, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileInputFact)value.Definition, value.CreatedBy, value.CreatedAt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CrsProfileView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.CrsProfileViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Code, value.Revision, value.Status, (global::RoadGuardSystem.DTOs.Projects.CrsProfileInput)value.Definition, value.CreatedBy, value.CreatedAt);
}
public sealed record RouteSystemInput(string Code, string Name)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RouteSystemInputFact?(RouteSystemInput? value)
        => value is null ? null! : new(value.Code, value.Name);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RouteSystemInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RouteSystemInputFact? value)
        => value is null ? null! : new(value.Code, value.Name);
}
public sealed record RouteSystemView(Guid Id, Guid ProjectId, string Code, string Name)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RouteSystemViewFact?(RouteSystemView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Code, value.Name);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator RouteSystemView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RouteSystemViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Code, value.Name);
}
public sealed record ChainageControl(double GeometricOffsetMeters, double StationMeters)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageControlFact?(ChainageControl? value)
        => value is null ? null! : new(value.GeometricOffsetMeters, value.StationMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ChainageControl?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageControlFact? value)
        => value is null ? null! : new(value.GeometricOffsetMeters, value.StationMeters);
}
public sealed record ChainageCalibrationInput(string SourceReference, string SourceChecksum,
    ChainageControl[] Controls, string SelectedReason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageCalibrationInputFact?(ChainageCalibrationInput? value)
        => value is null ? null! : new(value.SourceReference, value.SourceChecksum, value.Controls?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageControlFact)item).ToArray()!, value.SelectedReason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ChainageCalibrationInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.ChainageCalibrationInputFact? value)
        => value is null ? null! : new(value.SourceReference, value.SourceChecksum, value.Controls?.Select(item => (global::RoadGuardSystem.DTOs.Projects.ChainageControl)item).ToArray()!, value.SelectedReason);
}
public sealed record GeometryDraftReadiness(bool Ready, string[] MissingFields, string[] Errors,
    string CrsStatus, bool SampleOnly, string[] Warnings)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftReadinessFact?(GeometryDraftReadiness? value)
        => value is null ? null! : new(value.Ready, value.MissingFields, value.Errors, value.CrsStatus, value.SampleOnly, value.Warnings);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator GeometryDraftReadiness?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftReadinessFact? value)
        => value is null ? null! : new(value.Ready, value.MissingFields, value.Errors, value.CrsStatus, value.SampleOnly, value.Warnings);
}
