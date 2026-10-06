namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class PavementLayoutRevision
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid SegmentSetId { get; set; }
    public Guid? SourcePlanId { get; set; }
    public Guid? CrsProfileRevisionId { get; set; }
    public string Kind { get; set; } = "PLANNED";
    public string DefinitionJson { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GeometryMapPublication
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid SegmentSetId { get; set; }
    public Guid LayoutRevisionId { get; set; }
    public Guid? CrsProfileRevisionId { get; set; }
    public string SnapshotJson { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string PublicationMode { get; set; } = "SAMPLE";
    public Guid PublishedBy { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}

public sealed class GeometryLocationImpact
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PreviousRouteVersionId { get; set; }
    public Guid NewRouteVersionId { get; set; }
    public string AffectedReferencesJson { get; set; } = "";
    public Guid RecordedBy { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class GeometryLocationImpactDecision
{
    public Guid Id { get; set; }
    public Guid ImpactId { get; set; }
    public Guid TaskId { get; set; }
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class PavementSourceFileReference
{
    public Guid Id { get; set; }
    public Guid LayoutRevisionId { get; set; }
    public Guid FileId { get; set; }
    public string ContentChecksum { get; set; } = "";
    public string CaptureFactsJson { get; set; } = "";
}
