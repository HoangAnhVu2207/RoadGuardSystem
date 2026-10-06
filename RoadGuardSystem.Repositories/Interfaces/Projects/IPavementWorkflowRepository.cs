using NetTopologySuite.Geometries;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record PavementWorkflowCommand(Guid ActorId, Guid ProjectId, string Action,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid? ResourceId = null,
    object? Input = null, string? Key = null, string? ExpectedContentHash = null);
public sealed record PavementLayerQuery(string Layer, int Limit, double[]? Bbox, string? Cursor);
public sealed record PavementLayoutView(Guid Id, Guid ProjectId, Guid RouteVersionId, Guid SegmentSetId,
    Guid? SourcePlanId, Guid? CrsProfileRevisionId, string Kind, string ContentHash,
    PavementGeometryPreview Geometry, DateTimeOffset CreatedAt);
public sealed record GeometryImpactView(Guid Id, Guid ProjectId, Guid PreviousRouteVersionId,
    Guid NewRouteVersionId, GeometryAffectedReference[] References, DateTimeOffset RecordedAt);
public sealed record GeometryImpactDecisionView(Guid Id, Guid ImpactId, Guid TaskId, string Action,
    string Reason, DateTimeOffset OccurredAt, string ExecutionStatus = "RECORDED_ONLY");
public interface IPavementWorkflowRepository
{
    Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, PavementWorkflowCommand command,
        Func<CancellationToken, Task<bool>> scopeGuard,
        Func<GeometryDraftInput?, LineString, PavementPlanCreateInput, PavementGeometryPreview> plan,
        Func<PavementGeometryPreview, AsBuiltLayoutInput, PavementGeometryPreview> asBuilt,
        Func<GeometryDraftInput, int, GeometryPreview> geometryPreview,
        Func<GeometryMapSnapshot, PavementLayerQuery, string, GeometryMapPage> page,
        CancellationToken cancellationToken);
}
