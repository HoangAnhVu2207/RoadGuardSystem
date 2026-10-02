using RoadGuardSystem.DTOs.Projects;
namespace RoadGuardSystem.Repositories.Projects;

public sealed record GeometryWorkflowResult(int Status, string? Code = null, object? Value = null, string? Version = null);
public sealed record GeometryWorkflowCommand(Guid ActorId, Guid ProjectId, string Action, Guid? RoadSectionId,
    Guid? DraftId, Guid? RouteVersionId, Guid? SetId, object? Input, string? Key, string? ExpectedVersion);
public interface IGeometryWorkflowRepository
{
    Task<bool> CanReadAssignedGeometryAsync(Guid actorId, Guid projectId, Guid routeVersionId, Guid setId, CancellationToken cancellationToken);
    Task<GeometryWorkflowResult> ExecuteAsync(GeometryWorkflowCommand command,
        Func<GeometryDraftInput, int, GeometryPreview> preview,
        Func<Guid, NetTopologySuite.Geometries.LineString, double, SegmentDefinition, SegmentPreview> segments,
        CancellationToken cancellationToken);
}
