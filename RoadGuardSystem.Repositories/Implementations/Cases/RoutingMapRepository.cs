using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Spatial;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed partial class RoutingMapRepository(RoadGuardDbContext db, ICaseWorkflowRepository cases,
    IAnhHuyFactsRepository files, IRoutingCursorCodec codec) : IRoutingMapRepository
{
    private const string Algorithm = "ROUTING_WGS84_UTM_GRID_V1";
    private const int MaxVertices = 4096;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task GuardAsync(Guid actor, UserRoleCode role, Guid? caseId,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token)
    {
        if (caseId is Guid id)
            await cases.GuardAsync(actor, role, [id], null, projectAccess, token);
        else
        {
            if (role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
            // Same current-user/current-role lock used by the existing Case authority, without project membership.
            await cases.GuardAsync(actor, role, [], null, projectAccess, token);
        }
    }

    public Task<RoutingMapView> MapAsync(Guid actor, UserRoleCode role, string? bbox, int limit,
        string? cursor, bool includeHistorical, CancellationToken token) => InReadAsync<RoutingMapView>(async () =>
    {
        await GuardAsync(actor, role, null, (_, _) => Task.FromResult(false), token);
        CheckLimit(limit);
        var bounds = Bounds(bbox);
        var filter = Hash(new { bounds, includeHistorical, limit });
        var snapshot = await RegistryVersionAsync(token);
        var after = DecodeCursor(cursor, actor, "map", filter, snapshot);
        var rows = await HeadsAsync(null, includeHistorical, after, limit + 1, token);
        var page = rows.Take(limit).ToArray();
        var ids = page.Where(x => x.Points <= MaxVertices).Select(x => x.Id).ToArray();
        var shapes = await db.RoadSectionVersions.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Geometry }).ToDictionaryAsync(x => x.Id, x => x.Geometry, token);
        var features = new List<RoutingFeature>();
        var unavailable = new List<RoutingUnavailableRoute>();
        var vertexBudget = 20000;
        foreach (var row in page)
        {
            var line = shapes.GetValueOrDefault(row.Id);
            var reason = GeometryReason(row, line);
            if (reason is not null)
            {
                unavailable.Add(new(row.Id, row.ProjectId, row.SectionId, [reason]));
                continue;
            }
            var projected = Wgs84DisplayProjection.LineString(line, line!.SRID)!;
            var coordinates = (double[][])projected.Coordinates;
            if (bounds is not null && !Intersects(coordinates, bounds)) continue;
            if (coordinates.Length > vertexBudget)
            {
                unavailable.Add(new(row.Id, row.ProjectId, row.SectionId, ["DISPLAY_PAGE_VERTEX_BUDGET"]));
                continue;
            }
            vertexBudget -= coordinates.Length;
            var properties = await PropertiesAsync(row, token, false);
            var historical = await (from obligation in db.Set<RepairObligation>().AsNoTracking()
                                    join defect in db.Defects.AsNoTracking() on obligation.DefectId equals defect.Id
                                    where defect.RoadSectionVersionId == row.Id
                                    orderby obligation.Id
                                    select new RoutingHistoricalScopePin(obligation.Id, row.Id, obligation.Scope.LocationVersion))
                .Take(101).ToArrayAsync(token);
            features.Add(new(row.Id, new("LineString", coordinates), new(properties.ProjectId, properties.ProjectName,
                properties.ProjectStatus, properties.RoadSectionId, properties.RoadSectionCode, properties.RoadSectionName,
                properties.RouteVersionId, properties.VersionNo, properties.IsCurrent, properties.EffectiveFrom,
                properties.GeometryHash, properties.CrsProfileRevisionId, properties.SegmentSets.Take(100).ToArray(),
                historical.Take(100).ToArray(), historical.Length > 100 || properties.SegmentSets.Count > 100)));
        }
        // Pages are over the ordered registry, not only drawable routes; an empty page may have nextCursor.
        return new(features, unavailable,
            rows.Length > limit ? EncodeCursor(actor, "map", filter, snapshot, page[^1].Id) : null,
            Hash(new { snapshot, filter, after }), page.Length, includeHistorical ? "ALL_ROUTE_VERSIONS" : "CURRENT_AND_OBLIGATION_PINNED");
    }, token);

    private sealed record RouteHead(Guid Id, Guid ProjectId, string ProjectName, ProjectStatus ProjectStatus,
        Guid SectionId, string Code, string? Name, int VersionNo, bool IsCurrent, DateTimeOffset EffectiveFrom,
        Guid? ProfileId, int Points);

    private Task<RouteHead[]> HeadsAsync(Guid? project, bool history, Guid? after, int take, CancellationToken token)
    {
        var query = from route in db.RoadSectionVersions.AsNoTracking()
                    join section in db.RoadSections.AsNoTracking() on route.RoadSectionId equals section.Id
                    join owner in db.Projects.AsNoTracking() on section.ProjectId equals owner.Id
                    where (project == null || section.ProjectId == project) &&
                        (history || route.IsCurrent || db.Defects.Any(d => d.RoadSectionVersionId == route.Id &&
                            db.Set<RepairObligation>().Any(o => o.DefectId == d.Id))) &&
                        (after == null || route.Id.CompareTo(after.Value) > 0)
                    orderby route.Id
                    select new RouteHead(route.Id, owner.Id, owner.Name, owner.Status, section.Id, section.Code,
                        section.Name, route.VersionNo, route.IsCurrent, route.EffectiveFrom, route.CrsProfileRevisionId,
                        route.Geometry.NumPoints);
        return query.Take(take).ToArrayAsync(token);
    }

    private async Task<RoutingRouteProperties> PropertiesAsync(RouteHead row, CancellationToken token, bool withResponsibility = true)
    {
        var sets = await db.RoadSegmentSets.AsNoTracking().Where(s => s.RoadSectionVersionId == row.Id)
            .OrderBy(s => s.Id).Select(s => new { s.Id, s.Status, s.RowVersion, s.GeometryHash }).ToArrayAsync(token);
        var pins = withResponsibility ? await db.Set<RoadCoverageMapping>().AsNoTracking().Where(m => m.RoadSectionId == row.SectionId &&
            db.Set<RepairObligation>().Any(o => o.Id == m.ScopeObligationId &&
                db.Defects.Any(d => d.Id == o.DefectId && d.RoadSectionVersionId == row.Id)) &&
            !db.Set<RoadCoverageMapping>().Any(next => next.SupersedesId == m.Id))
            .OrderBy(m => m.Id).ToArrayAsync(token) : [];
        var version = await db.Set<RoadGeometryMetadata>().Where(m => m.RoadSectionVersionId == row.Id)
            .Select(m => m.GeometryHash).SingleOrDefaultAsync(token);
        if (version is null)
            version = (await db.Database.SqlQuery<string>($"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',[Geometry].STAsBinary()),2) AS [Value] FROM [RoadSectionVersions] WHERE [Id]={row.Id}")
                .SingleAsync(token)).ToLowerInvariant();
        var coverage = new List<RoutingCoveragePin>();
        foreach (var mapping in pins)
        {
            var obligation = await db.Set<RepairObligation>().AsNoTracking().SingleAsync(o => o.Id == mapping.ScopeObligationId, token);
            var responsible = await ObligationResponsibilityScope.ResolveAsync(db, obligation.Id, obligation.ProjectId, token);
            coverage.Add(new(mapping.Id, mapping.ScopeObligationId, mapping.LocationVersion, mapping.Provenance,
                mapping.SourceKind, mapping.SourceId, mapping.SourceVersion, mapping.ApplicableFromUtc, mapping.ApplicableToUtc,
                Hash(mapping), obligation.ProjectId, responsible, obligation.CurrentRepairItemId));
        }
        var policies = withResponsibility ? await db.Set<RepairPolicyRevision>().AsNoTracking().Include(p => p.Revocations)
            .Where(p => p.ProjectId == row.ProjectId).OrderBy(p => p.Id).ToArrayAsync(token) : [];
        return new(row.ProjectId, row.ProjectName, row.ProjectStatus.ToString().ToUpperInvariant(), row.SectionId,
            row.Code, row.Name, row.Id, row.VersionNo, row.IsCurrent, row.EffectiveFrom, version, row.ProfileId,
            sets.Select(s => new RoutingSegmentSetPin(s.Id, s.Status, Convert.ToBase64String(s.RowVersion), s.GeometryHash)).ToArray(),
            coverage, policies.Select(p => new RoutingPolicyPin(p.Id, p.Revision, p.DefectTypeCode, p.PublishedAt,
                p.IsRevoked, Hash(new { p.Id, p.Revision, revocations = p.Revocations.OrderBy(r => r.Id).ToArray() }))).ToArray());
    }

    private static string? GeometryReason(RouteHead head, LineString? line)
    {
        if (head.Points > MaxVertices) return "GEOMETRY_VERTEX_LIMIT";
        if (line is null || line.IsEmpty) return "GEOMETRY_MISSING";
        if (!line.IsValid || line.NumPoints < 2) return "GEOMETRY_INVALID";
        if (head.ProfileId is not null) return "NATIVE_CRS_TRANSFORM_UNAVAILABLE";
        if (!SpatialConstants.IsAllowedProjectUtmSrid(line.SRID)) return "CRS_UNSUPPORTED";
        return Wgs84DisplayProjection.LineString(line, line.SRID) is null ? "COORDINATES_OUTSIDE_CRS_AREA" : null;
    }

    private static double[]? Bounds(string? bbox)
    {
        if (bbox is null) return null;
        var parts = bbox.Split(',');
        if (parts.Length != 4) throw new CaseWorkflowException(400, "validation_error");
        var values = new double[4];
        for (var i = 0; i < 4; i++)
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) || !double.IsFinite(values[i]))
                Fail(400, "validation_error");
        if (values[0] < -180 || values[2] > 180 || values[1] < -90 || values[3] > 90 ||
            values[0] >= values[2] || values[1] >= values[3]) Fail(400, "validation_error");
        return values;
    }

    private static bool Intersects(double[][] points, double[] bounds)
    {
        var line = new LineString(points.Select(x => new Coordinate(x[0], x[1])).ToArray());
        var box = new GeometryFactory().ToGeometry(new Envelope(bounds[0], bounds[2], bounds[1], bounds[3]));
        return line.Intersects(box);
    }

    private sealed record PageCursor(Guid Actor, string Kind, string Filter, string Snapshot, Guid After);
    private string EncodeCursor(Guid actor, string kind, string filter, string snapshot, Guid after)
        => codec.Protect(JsonSerializer.Serialize(new PageCursor(actor, kind, filter, snapshot, after), Json));
    private Guid? DecodeCursor(string? cursor, Guid actor, string kind, string filter, string snapshot)
    {
        if (cursor is null) return null;
        PageCursor? decoded;
        try { decoded = JsonSerializer.Deserialize<PageCursor>(codec.Unprotect(cursor), Json); }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException or ArgumentException)
        { throw new CaseWorkflowException(400, "invalid_cursor"); }
        if (decoded is null || decoded.Actor != actor || decoded.Kind != kind || decoded.Filter != filter || decoded.After == Guid.Empty)
            throw new CaseWorkflowException(400, "invalid_cursor");
        if (decoded.Snapshot != snapshot) Fail(409, "routing_cursor_stale");
        return decoded.After;
    }

    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json))).ToLowerInvariant();
    private static void CheckLimit(int limit) { if (limit is < 1 or > 100) Fail(400, "validation_error"); }
    private static void Fail(int status, string code) => throw new CaseWorkflowException(status, code);
    private Task<T> InReadAsync<T>(Func<Task<T>> action, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is not null) return action();
        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await action();
            await transaction.CommitAsync(token);
            return result;
        });
    }
}
