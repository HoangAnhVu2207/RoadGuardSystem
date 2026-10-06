namespace RoadGuardSystem.DTOs.Projects;

public sealed record CrsControlPoint(GeometryPoint Native, GeometryPoint Wgs84, string IndependentSource);
public sealed record CrsEllipsoidDefinition(double SemiMajorAxisMeters, double InverseFlattening, string SourceReference);
public sealed record CrsProjectionDefinition(IReadOnlyDictionary<string,double> Parameters, string SourceReference);
public sealed record CrsTransformOperation(string Method, string Convention, string Direction, double[] Parameters,
    string SourceReference, string? AreaOfUse = null, double? PublishedAccuracyMeters = null, string? AccuracySource = null);
public sealed record CrsProfileInput(string Code, int SourceSrid, string Datum, string Projection,
    string AxisOrder, double MetresPerUnit, string SourceReference, string SourceChecksum,
    CrsTransformOperation? Operation = null, CrsControlPoint[]? IndependentControls = null,
    double? SourcedToleranceMeters = null, string? ToleranceSource = null, bool SampleOnly = true,
    CrsEllipsoidDefinition? Ellipsoid = null, CrsProjectionDefinition? ProjectionDefinition = null);
public sealed record CrsProfileView(Guid Id, Guid ProjectId, string Code, int Revision, string Status,
    CrsProfileInput Definition, Guid CreatedBy, DateTimeOffset CreatedAt);
public sealed record RouteSystemInput(string Code, string Name);
public sealed record RouteSystemView(Guid Id, Guid ProjectId, string Code, string Name);
public sealed record ChainageControl(double GeometricOffsetMeters, double StationMeters);
public sealed record ChainageCalibrationInput(string SourceReference, string SourceChecksum,
    ChainageControl[] Controls, string SelectedReason);
public sealed record GeometryDraftReadiness(bool Ready, string[] MissingFields, string[] Errors,
    string CrsStatus, bool SampleOnly, string[] Warnings);
