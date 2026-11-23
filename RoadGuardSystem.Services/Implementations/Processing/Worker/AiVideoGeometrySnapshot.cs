using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Projects;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed record AiVideoGeometrySnapshot(Guid RouteVersionId, Guid SegmentSetId, string GeometryVersion, Guid[] SegmentIds)
{
    // A video has no accepted per-scope assignment. Never choose the first scope or infer ground position from aircraft telemetry.
    public static async Task<AiVideoGeometrySnapshot?> CaptureAsync(AiVideoSource source, IGeometryWorkflowRepository? geometry, CancellationToken ct)
    {
        if (geometry is null || string.IsNullOrWhiteSpace(source.ScopeManifest)) return null;
        try
        {
            using var doc = JsonDocument.Parse(source.ScopeManifest);
            var scopes = doc.RootElement.EnumerateArray().Select(s => new {
                Route = Field(s, "routeVersionId").GetGuid(), Set = Field(s, "segmentSetId").GetGuid(),
                Segments = Field(s, "segmentIds").EnumerateArray().Select(x => x.GetGuid()).ToArray()
            }).ToArray();
            if (scopes.Length == 0 || scopes.Any(s => s.Route == Guid.Empty || s.Set == Guid.Empty || s.Segments.Length == 0 || s.Segments.Contains(Guid.Empty)) ||
                scopes.Select(s => (s.Route, s.Set)).Distinct().Count() != 1) return null;
            var scope = scopes[0];
            var result = await geometry.ExecuteAsync(new(source.OwnerId, source.ProjectId, "package", null, null, scope.Route, scope.Set, null, null, null, UserRoleCode.DroneOperator),
                (input, srid) => Projects.GeometryEngine.Preview(input, srid),
                (id, line, origin, definition) => Projects.GeometryEngine.Segments(id, line, origin, definition), ct);
            if (result.Status != 200 || result.Value is not GeometryPackageViewFact p || string.IsNullOrEmpty(result.Version) ||
                p.ProjectId != source.ProjectId || p.Route.RouteVersionId != scope.Route || p.SegmentSet.Id != scope.Set || p.SegmentSet.RouteVersionId != scope.Route ||
                p.Route.MetadataStatus != "COMPLETE" || p.SegmentSet.MetadataStatus != "COMPLETE" || !p.Route.IsCurrent || p.SegmentSet.Status != "PUBLISHED") return null;
            var ids = scopes.SelectMany(s => s.Segments).Distinct().Order().ToArray();
            if (ids.Any(id => !p.SegmentSet.Segments.Any(s => s.Id == id))) return null;
            return new(scope.Route, scope.Set, result.Version, ids);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException) { return null; }
    }
    private static JsonElement Field(JsonElement value, string name)
        => value.EnumerateObject().Single(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
}
