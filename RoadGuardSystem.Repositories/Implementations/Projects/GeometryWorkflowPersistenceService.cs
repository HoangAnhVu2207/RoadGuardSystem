using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Idempotency;
namespace RoadGuardSystem.Repositories.Projects;

public sealed class GeometryWorkflowPersistenceService(RoadGuardDbContext db, IdempotencyOperationService idempotency) : IGeometryWorkflowRepository
{
    public Task<bool> CanReadAssignedGeometryAsync(Guid actorId, Guid projectId, Guid routeVersionId, Guid setId, CancellationToken cancellationToken)
        => (from scope in db.SurveyRequestScopes
            join task in db.SurveyRequests on scope.SurveyRequestId equals task.Id
            join assignment in db.SurveyAssignments on task.Id equals assignment.SurveyRequestId
            where task.ProjectId == projectId && scope.RouteSectionVersionId == routeVersionId && scope.SegmentSetId == setId
                && assignment.OperatorUserId == actorId && assignment.EndedAt == null
            select task.Id).AnyAsync(cancellationToken);
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
    private static T Decode<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
    private static string Version(byte[] bytes) => Convert.ToBase64String(bytes);
    private sealed class Rejected(int status, string code) : Exception { public GeometryWorkflowResult Result { get; } = new(status, code); }
    private static void Reject(int status, string code) => throw new Rejected(status, code);
    public async Task<GeometryWorkflowResult> ExecuteAsync(GeometryWorkflowCommand command, Func<GeometryDraftInput,int,GeometryPreview> preview,
        Func<Guid,LineString,double,SegmentDefinition,SegmentPreview> segments, CancellationToken cancellationToken)
    {
        var c = command; var ct = cancellationToken;
        try
        {
            var write = c.Action is "draft-create" or "draft-edit" or "confirm" or "set-create" or "set-edit" or "publish";
            if (!write) return await Run(c, preview, segments, ct);
            var receipt = await idempotency.ExecuteAsync(c.ActorId, c.ProjectId, "Geometry:" + c.Action, c.Key!, Hash(new { c.RoadSectionId, c.DraftId, c.RouteVersionId, c.SetId, c.Input, c.ExpectedVersion }), async token => {
                // Project serialization also covers alias URLs whose draft resolves the existing road only inside the handler.
                var resource = $"anh01:geometry:{c.ProjectId}";
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @lockResult int; EXEC @lockResult = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @lockResult < 0 THROW 51000, 'Geometry command lock unavailable', 1;", token);
                var result = await Run(c, preview, segments, token);
                var id = result.Value switch { GeometryDraftView draft => draft.Id, RoadGeometryVersionView route => route.RouteVersionId, SegmentSetView set => set.Id, _ => c.SetId ?? c.DraftId ?? Guid.NewGuid() };
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.ActorId, DateTimeOffset.UtcNow,
                    "geometry_" + c.Action.Replace('-', '_'), "RoadGeometry", id, null,
                    JsonSerializer.Serialize(new { project_id = c.ProjectId, operation = c.Action }), "GEOMETRY_WORKFLOW", "PROJECT_API", null,
                    snapshotAllowedPropertyNames: ["project_id", "operation"]));
                await db.SaveChangesAsync(token);
                return (id, JsonSerializer.Serialize(result));
            }, ct);
            if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "duplicate_request");
            var restored = Decode<GeometryWorkflowResult>(receipt.OutcomeJson);
            if (restored.Value is JsonElement json) restored = restored with { Value = c.Action switch {
                "draft-create" or "draft-edit" => json.Deserialize<GeometryDraftView>(),
                "confirm" => json.Deserialize<RoadGeometryVersionView>(),
                _ => json.Deserialize<SegmentSetView>() } };
            return restored;
        }
        catch (Rejected e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(412, "concurrency_conflict"); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) {
            db.ChangeTracker.Clear();
            return new(409, c.Action == "confirm"
                ? ((GeometryConfirmInput)c.Input!).ExpectedCurrentVersionId is null ? "road_section_code_conflict" : "road_section_concurrency_conflict"
                : "segment_publication_conflict");
        }
    }
    private async Task<GeometryWorkflowResult> Run(GeometryWorkflowCommand c, Func<GeometryDraftInput,int,GeometryPreview> preview,
        Func<Guid,LineString,double,SegmentDefinition,SegmentPreview> segments, CancellationToken ct)
    {
        var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == c.ProjectId, ct);
        if (project is null) Reject(404, "project_not_found");
        if (project!.Status == RoadGuardSystem.aBusinessObjects.Commons.ProjectStatus.Closed && c.Action is "draft-create" or "draft-edit" or "confirm" or "set-create" or "set-edit" or "publish") Reject(409, "project_closed");
        var srid = project!.EngineeringUtmSrid ?? 0;
        RoadSection? section = null;
        if (c.RoadSectionId is Guid sid) {
            section = await db.RoadSections.SingleOrDefaultAsync(x => x.Id == sid && x.ProjectId == c.ProjectId, ct);
            if (section is null) Reject(404, "road_section_not_found");
        }
        if (c.Action.StartsWith("draft", StringComparison.Ordinal) || c.Action == "confirm")
        {
            RoadGeometryDraft? draft = null;
            if (c.DraftId is Guid did) {
                draft = await db.Set<RoadGeometryDraft>().SingleOrDefaultAsync(x => x.Id == did && x.ProjectId == c.ProjectId && (c.RoadSectionId == null || x.RoadSectionId == c.RoadSectionId), ct);
                if (draft is null) Reject(404, "road_geometry_draft_not_found");
                if (section is null && draft!.RoadSectionId is Guid existingSectionId)
                {
                    section = await db.RoadSections.SingleOrDefaultAsync(x => x.Id == existingSectionId && x.ProjectId == c.ProjectId, ct);
                    if (section is null) Reject(404, "road_section_not_found");
                }
            }
            if (c.Action is "draft-create" or "draft-edit")
            {
                var input = (GeometryDraftInput)c.Input!; preview(input, srid);
                var code = section?.Code ?? input.RoadCode?.Trim();
                if (string.IsNullOrWhiteSpace(code) || code.Length > 80 || input.RoadName?.Length > 255 || (input.BranchCode is not null && input.BranchCode != code)) Reject(422, "geometry_validation_failed");
                if (draft is null) { draft = RoadGeometryDraft.Create(c.ProjectId, c.RoadSectionId, code!, input.RoadName, JsonSerializer.Serialize(input), JsonSerializer.Serialize(input.Coordinates), null, c.ActorId); db.Add(draft); }
                else { Match(draft.RowVersion, c.ExpectedVersion); if (draft.Status != "DRAFT") Reject(409,"road_geometry_invalid_state");
                    var previousInput=Decode<GeometryDraftInput>(draft.InputJson);
                    var original=previousInput.SourceKind==input.SourceKind && previousInput.SourceCrs==input.SourceCrs && previousInput.SourceFileId==input.SourceFileId ? draft.OriginalCoordinatesJson : JsonSerializer.Serialize(input.Coordinates);
                    draft.Edit(JsonSerializer.Serialize(input),original,null,code!,input.RoadName,c.ActorId); }
                await db.SaveChangesAsync(ct);
                return new(c.Action == "draft-create" ? 201 : 200, Value: DraftView(draft), Version: Version(draft.RowVersion));
            }
            if (c.ExpectedVersion is not null) Match(draft!.RowVersion,c.ExpectedVersion);
            var stored = Decode<GeometryDraftInput>(draft!.InputJson);
            if (c.Action == "draft-get") return new(200,Value:DraftView(draft),Version:Version(draft.RowVersion));
            if (c.Action == "draft-preview") return new(200,Value:preview(stored,srid),Version:Version(draft.RowVersion));
            if (draft.Status != "DRAFT") Reject(409,"road_geometry_invalid_state");
            var confirm = (GeometryConfirmInput)c.Input!;
            if (string.IsNullOrWhiteSpace(confirm.Reason) || confirm.EffectiveFrom == default) Reject(422,"geometry_validation_failed");
            var prepared = preview(stored,srid);
            RoadSectionVersion? current = section is null ? null : await db.RoadSectionVersions.SingleOrDefaultAsync(x => x.RoadSectionId == section.Id && x.IsCurrent,ct);
            if (current?.Id != confirm.ExpectedCurrentVersionId) Reject(409,"road_section_concurrency_conflict");
            if (section is null) { section = RoadSection.Create(Guid.NewGuid(),c.ProjectId,draft.RoadCode,draft.RoadName); db.Add(section); }
            if (current is not null) { current.ClearCurrent(); await db.SaveChangesAsync(ct); }
            var line = new GeometryFactory(new PrecisionModel(),srid).CreateLineString(stored.Coordinates!.Select(p=>new Coordinate(p.X,p.Y)).ToArray());
            var version = RoadSectionVersion.Create(Guid.NewGuid(),section.Id,(current?.VersionNo ?? 0)+1,true,line,confirm.EffectiveFrom,confirm.Reason);
            db.Add(version); draft.Confirm(section.Id);
            db.Add(RoadGeometryMetadata.Create(version.Id,draft.Id,draft.InputJson,Hash(new { srid, coordinates=stored.Coordinates, stored.WidthProfile, stored.StationOriginMeters, stored.SurveyWidthMeters }),c.ActorId));
            await db.SaveChangesAsync(ct);
            var confirmed=await RouteView(c.ProjectId,section.Id,version,ct);
            return new(201,Value:confirmed,Version:Hash(confirmed));
        }
        var route = await db.RoadSectionVersions.SingleOrDefaultAsync(x => x.Id == c.RouteVersionId && (c.RoadSectionId == null || x.RoadSectionId == c.RoadSectionId),ct);
        if (route is null) Reject(404,"road_section_version_not_found");
        if (section is null) section = await db.RoadSections.SingleOrDefaultAsync(x => x.Id == route!.RoadSectionId && x.ProjectId == c.ProjectId,ct);
        if (section is null) Reject(404,"road_section_version_not_found");
        var view = await RouteView(c.ProjectId,section!.Id,route!,ct);
        if (c.Action == "geometry-get") return new(200,Value:view,Version:Hash(view));
        RoadSegmentSet? set = null;
        if (c.SetId is Guid setId) { set = await db.RoadSegmentSets.SingleOrDefaultAsync(x => x.Id == setId && x.RoadSectionVersionId == route!.Id,ct); if (set is null) Reject(404,"segment_set_not_found"); }
        if (c.Action is "set-get" or "package") {
            var setView = await SetView(set!,ct);
            if (c.Action == "package")
            {
                var package = new GeometryPackageView("anh01.geometry.v1",c.ProjectId,view,setView);
                return new(200, Value:package, Version:Hash(package));
            }
            return new(200,Value:setView,Version:setView.Version);
        }
        if (view.MetadataStatus != "COMPLETE") Reject(422,"geometry_metadata_incomplete");
        if (c.Action is "set-preview" or "set-create" or "set-edit") {
            var definition = (SegmentDefinition)c.Input!;
            var computed = segments(route!.Id,route.Geometry,view.StationOriginMeters!.Value,definition);
            if (c.Action == "set-preview") return new(200,Value:computed);
            if (set is null) { set = RoadSegmentSet.Create(Guid.NewGuid(),route.Id,"DRAFT"); db.Add(set); }
            else { Match(set.RowVersion,c.ExpectedVersion); if (set.Status != "DRAFT") Reject(409,"road_geometry_invalid_state"); db.RoadSegments.RemoveRange(await db.RoadSegments.Where(x=>x.SegmentSetId==set.Id).ToListAsync(ct)); await db.SaveChangesAsync(ct); }
            set.Define(JsonSerializer.Serialize(definition),computed.DefinitionHash);
            var indexed = new LengthIndexedLine(route.Geometry);
            foreach(var part in computed.Segments) { var row = RoadSegment.Create(Guid.NewGuid(),set.Id,route.Id,part.Sequence); row.SetGeometry(part.FromOffsetMeters,part.ToOffsetMeters,view.StationOriginMeters.Value,(LineString)indexed.ExtractLine(part.FromOffsetMeters,part.ToOffsetMeters)); db.Add(row); }
            await db.SaveChangesAsync(ct); var result=await SetView(set,ct); return new(c.Action=="set-create"?201:200,Value:result,Version:result.Version);
        }
        Match(set!.RowVersion,c.ExpectedVersion); if(set.Status!="DRAFT") Reject(409,"road_geometry_invalid_state");
        var publication=(SegmentPublishInput)c.Input!;
        if(string.IsNullOrWhiteSpace(publication.Reason)) Reject(422,"segment_validation_failed");
        if(!route!.IsCurrent) Reject(409,"road_section_concurrency_conflict");
        var previous=await db.RoadSegmentSets.SingleOrDefaultAsync(x=>x.RoadSectionVersionId==route.Id && x.Status=="PUBLISHED",ct);
        if(previous?.Id!=publication.ExpectedPublishedSetId) Reject(409,"segment_publication_conflict");
        if(previous is not null) { previous.Supersede(); await db.SaveChangesAsync(ct); }
        set.Publish(c.ActorId); await db.SaveChangesAsync(ct); var published=await SetView(set,ct); return new(200,Value:published,Version:published.Version);
    }
    private static void Match(byte[] version,string? expected) { if(Version(version)!=expected) Reject(412,"concurrency_conflict"); }
    private static GeometryDraftView DraftView(RoadGeometryDraft d) { var i=Decode<GeometryDraftInput>(d.InputJson); return new(d.Id,d.ProjectId,d.RoadSectionId,d.RoadCode,d.RoadName,d.Status,i.SourceKind,i.SourceCrs,i.SourceFileId,d.SourceChecksum,i.TrackIndex,i.TrackSegmentIndex,Decode<GeometryPoint[]>(d.OriginalCoordinatesJson),i.Coordinates!,i.StationOriginMeters,i.WidthProfile,i.SurveyWidthMeters,d.CreatedBy,d.UpdatedBy,Version(d.RowVersion)); }
    private static GeometryShape Shape(LineString line)=>new("LineString",line.Coordinates.Select(p=>new[]{p.X,p.Y}).ToArray());
    private async Task<RoadGeometryVersionView> RouteView(Guid project,Guid section,RoadSectionVersion route,CancellationToken ct) {
        var m=await db.Set<RoadGeometryMetadata>().AsNoTracking().SingleOrDefaultAsync(x=>x.RoadSectionVersionId==route.Id,ct); var i=m is null?null:Decode<GeometryDraftInput>(m.InputJson);
        return new(project,section,route.Id,route.VersionNo,route.IsCurrent,route.EffectiveFrom,m?.SourceDraftId,i?.SourceCrs,route.Geometry.SRID,i?.StationOriginMeters,i?.WidthProfile,i?.SurveyWidthMeters,Shape(route.Geometry),null,route.Geometry.Length,m is null?"LEGACY_INCOMPLETE":"COMPLETE",m?.ApprovedBy,m?.ApprovedAt,m?.GeometryHash??Hash(route.Geometry.AsText()));
    }
    private async Task<SegmentSetView> SetView(RoadSegmentSet set,CancellationToken ct) {
        var rows=await db.RoadSegments.AsNoTracking().Where(x=>x.SegmentSetId==set.Id).OrderBy(x=>x.Sequence).ToListAsync(ct);
        if(set.DefinitionJson is null || rows.Any(x=>x.Geometry is null || x.FromOffsetMeters is null)) Reject(422,"geometry_metadata_incomplete");
        return new(set.Id,set.RoadSectionVersionId,set.Status,Decode<SegmentDefinition>(set.DefinitionJson!),set.GeometryHash!,rows.Select(x=>new SegmentGeometry(x.Id,x.Sequence,x.FromOffsetMeters!.Value,x.ToOffsetMeters!.Value,x.StartStationMeters!.Value,x.EndStationMeters!.Value,x.ToOffsetMeters.Value-x.FromOffsetMeters.Value,Shape(x.Geometry!),null)).ToArray(),set.PublishedBy,set.PublishedAt,Version(set.RowVersion));
    }
}
