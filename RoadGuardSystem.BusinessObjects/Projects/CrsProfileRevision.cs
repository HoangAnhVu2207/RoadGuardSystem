namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class CrsProfileRevision
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = "";
    public int Revision { get; set; }
    public int SourceSrid { get; set; }
    public string Status { get; set; } = "CANDIDATE";
    public bool SampleOnly { get; set; }
    public string PayloadJson { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RoadRouteSystem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class NativeRouteVersionFacts
{
    public Guid RoadSectionVersionId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CrsProfileRevisionId { get; set; }
    public Guid RouteSystemId { get; set; }
    public string RouteKind { get; set; } = "";
    public Guid? ParentRouteVersionId { get; set; }
    public double? JunctionOffsetMeters { get; set; }
    public double CanonicalLengthMeters { get; set; }
    public double? DeclaredLengthMeters { get; set; }
    public string? CalibrationJson { get; set; }
    public bool SampleOnly { get; set; }
}
