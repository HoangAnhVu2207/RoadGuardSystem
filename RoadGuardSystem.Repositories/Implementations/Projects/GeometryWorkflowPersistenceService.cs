using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Projects;

public sealed class GeometryWorkflowPersistenceService(RoadGuardDbContext db, IdempotencyOperationService idempotency, TimeProvider? timeProvider = null) : IGeometryWorkflowRepository
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
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
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Reject(int status, string code) => throw new Rejected(status, code);
    public async Task<GeometryWorkflowResult> ExecuteAsync(GeometryWorkflowCommand command, Func<GeometryDraftInputFact, int, GeometryPreviewFact> preview,
        Func<Guid, LineString, double, SegmentDefinitionFact, SegmentPreviewFact> segments, CancellationToken cancellationToken)
    {
        var c = command; var ct = cancellationToken;
        try
        {
            var write = c.Action is "draft-create" or "draft-edit" or "confirm" or "set-create" or "set-edit" or "publish" or "profile-create" or "system-create";
            if (!write && db.Database.CurrentTransaction is not null)
            {
                await GuardAsync(c, true, ct);
                return await Run(c, preview, segments, ct);
            }
            if (!write) return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                await GuardAsync(c, true, ct);
                var result = await Run(c, preview, segments, ct);
                await transaction.CommitAsync(ct);
                return result;
            });
            var receipt = await idempotency.ExecuteAsync(c.ActorId, c.ProjectId, "Geometry:" + c.Action, c.Key!, Hash(new { c.RoadSectionId, c.DraftId, c.RouteVersionId, c.SetId, c.Input, c.ExpectedVersion }), async token =>
            {
                // Project serialization also covers alias URLs whose draft resolves the existing road only inside the handler.
                var resource = $"anh01:geometry:{c.ProjectId}";
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @lockResult int; EXEC @lockResult = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @lockResult < 0 THROW 51000, 'Geometry command lock unavailable', 1;", token);
                await GuardAsync(c, true, token);
                var result = await Run(c, preview, segments, token);
                var id = result.Value switch { GeometryDraftViewFact draft => draft.Id, RoadGeometryVersionViewFact route => route.RouteVersionId, SegmentSetViewFact set => set.Id, _ => c.SetId ?? c.DraftId ?? Guid.NewGuid() };
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.ActorId, clock.GetUtcNow(),
                    "geometry_" + c.Action.Replace('-', '_'), "RoadGeometry", id, null,
                    JsonSerializer.Serialize(new { project_id = c.ProjectId, operation = c.Action }), "GEOMETRY_WORKFLOW", "PROJECT_API", null,
                    snapshotAllowedPropertyNames: ["project_id", "operation"]));
                await db.SaveChangesAsync(token);
                return (id, JsonSerializer.Serialize(result));
            }, ct, receiptAccessGuard: token => GuardAsync(c, true, token));
            if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "duplicate_request");
            var restored = Decode<GeometryWorkflowResult>(receipt.OutcomeJson);
            if (restored.Value is JsonElement json) restored = restored with
            {
                Value = c.Action switch
                {
                    "draft-create" or "draft-edit" => json.Deserialize<GeometryDraftViewFact>(),
                    "confirm" => json.Deserialize<RoadGeometryVersionViewFact>(),
                    "profile-create" => json.Deserialize<CrsProfileViewFact>(),
                    "system-create" => json.Deserialize<RouteSystemViewFact>(),
                    _ => json.Deserialize<SegmentSetViewFact>()
                }
            };
            return restored;
        }
        catch (Rejected e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(412, "concurrency_conflict"); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            return new(409, c.Action == "confirm"
                ? ((GeometryConfirmInputFact)c.Input!).ExpectedCurrentVersionId is null ? "road_section_code_conflict" : "road_section_concurrency_conflict"
                : "segment_publication_conflict");
        }
    }
    private async Task<GeometryWorkflowResult> Run(GeometryWorkflowCommand c, Func<GeometryDraftInputFact, int, GeometryPreviewFact> preview,
        Func<Guid, LineString, double, SegmentDefinitionFact, SegmentPreviewFact> segments, CancellationToken ct)
    {
        var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == c.ProjectId, ct);
        if (project is null) Reject(404, "project_not_found");
        if (project!.Status == RoadGuardSystem.aBusinessObjects.Commons.ProjectStatus.Closed && c.Action is "draft-create" or "draft-edit" or "confirm" or "set-create" or "set-edit" or "publish" or "profile-create" or "system-create") Reject(409, "project_closed");
        var srid = project!.EngineeringUtmSrid ?? 0;
        if (c.Action == "profile-create")
        {
            var input = (CrsProfileInputFact)c.Input!;
            var next = (await db.Set<CrsProfileRevision>().Where(x => x.ProjectId == c.ProjectId && x.Code == input.Code.Trim()).Select(x => (int?)x.Revision).MaxAsync(ct) ?? 0) + 1;
            var profile = new CrsProfileRevision { Id = Guid.NewGuid(), ProjectId = c.ProjectId, Code = input.Code.Trim(), Revision = next, SourceSrid = input.SourceSrid, Status = "CANDIDATE", SampleOnly = input.SampleOnly, PayloadJson = JsonSerializer.Serialize(input), CreatedBy = c.ActorId, CreatedAt = clock.GetUtcNow() };
            db.Add(profile); await db.SaveChangesAsync(ct); return new(201, Value: ProfileView(profile), Version: Hash(profile.PayloadJson));
        }
        if (c.Action == "profile-get")
        {
            var profile = await db.Set<CrsProfileRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.DraftId && x.ProjectId == c.ProjectId, ct);
            if (profile is null) Reject(404, "crs_profile_not_found");
            return new(200, Value: ProfileView(profile!), Version: Hash(profile!.PayloadJson));
        }
        if (c.Action == "profile-list") return new(200, Value: (await db.Set<CrsProfileRevision>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId).OrderBy(x => x.Code).ThenBy(x => x.Revision).ToArrayAsync(ct)).Select(ProfileView).ToArray());
        if (c.Action == "system-create")
        {
            var input = (RouteSystemInputFact)c.Input!;
            if (string.IsNullOrWhiteSpace(input.Code) || input.Code.Length > 80 || string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 255) Reject(422, "route_system_invalid");
            var system = new RoadRouteSystem { Id = Guid.NewGuid(), ProjectId = c.ProjectId, Code = input.Code.Trim(), Name = input.Name.Trim() };
            db.Add(system); await db.SaveChangesAsync(ct); return new(201, Value: new RouteSystemViewFact(system.Id, system.ProjectId, system.Code, system.Name));
        }
        if (c.Action == "system-list") return new(200, Value: await db.Set<RoadRouteSystem>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId).OrderBy(x => x.Code).Select(x => new RouteSystemViewFact(x.Id, x.ProjectId, x.Code, x.Name)).ToArrayAsync(ct));
        RoadSection? section = null;
        if (c.RoadSectionId is Guid sid)
        {
            section = await db.RoadSections.SingleOrDefaultAsync(x => x.Id == sid && x.ProjectId == c.ProjectId, ct);
            if (section is null) Reject(404, "road_section_not_found");
        }
        if (c.Action.StartsWith("draft", StringComparison.Ordinal) || c.Action == "confirm")
        {
            RoadGeometryDraft? draft = null;
            if (c.DraftId is Guid did)
            {
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
                var input = (GeometryDraftInputFact)c.Input!;
                if (input.SourceKind == "NATIVE_ALIGNMENT")
                {
                    input = await ResolveNativeInputAsync(input, c.ProjectId, ct);
                    if (NativeMissing(input).Length == 0) preview(input, input.SourceCrs);
                }
                else preview(input, srid);
                var code = section?.Code ?? input.RoadCode?.Trim();
                if (string.IsNullOrWhiteSpace(code) || code.Length > 80 || input.RoadName?.Length > 255 || (input.BranchCode is not null && input.BranchCode != code)) Reject(422, "geometry_validation_failed");
                if (draft is null) { draft = RoadGeometryDraft.Create(c.ProjectId, c.RoadSectionId, code!, input.RoadName, JsonSerializer.Serialize(input), JsonSerializer.Serialize(input.Coordinates), null, c.ActorId, clock.GetUtcNow()); db.Add(draft); }
                else
                {
                    Match(draft.RowVersion, c.ExpectedVersion); if (draft.Status != "DRAFT") Reject(409, "road_geometry_invalid_state");
                    var previousInput = Decode<GeometryDraftInputFact>(draft.InputJson);
                    var original = previousInput.SourceKind == input.SourceKind && previousInput.SourceCrs == input.SourceCrs && previousInput.SourceFileId == input.SourceFileId ? draft.OriginalCoordinatesJson : JsonSerializer.Serialize(input.Coordinates);
                    draft.Edit(JsonSerializer.Serialize(input), original, null, code!, input.RoadName, c.ActorId, clock.GetUtcNow());
                }
                await db.SaveChangesAsync(ct);
                return new(c.Action == "draft-create" ? 201 : 200, Value: DraftView(draft), Version: Version(draft.RowVersion));
            }
            if (c.ExpectedVersion is not null) Match(draft!.RowVersion, c.ExpectedVersion);
            var stored = Decode<GeometryDraftInputFact>(draft!.InputJson);
            if (stored.SourceKind == "NATIVE_ALIGNMENT") stored = await ResolveNativeInputAsync(stored, c.ProjectId, ct);
            if (c.Action == "draft-get") return new(200, Value: DraftView(draft), Version: Version(draft.RowVersion));
            if (c.Action == "draft-readiness") return new(200, Value: stored, Version: Version(draft.RowVersion));
            if (c.Action == "draft-preview") return new(200, Value: preview(stored, srid), Version: Version(draft.RowVersion));
            if (draft.Status != "DRAFT") Reject(409, "road_geometry_invalid_state");
            var confirm = (GeometryConfirmInputFact)c.Input!;
            if (string.IsNullOrWhiteSpace(confirm.Reason) || confirm.EffectiveFrom == default) Reject(422, "geometry_validation_failed");
            var prepared = preview(stored, srid);
            if (stored.SourceKind == "NATIVE_ALIGNMENT") await ValidateTopologyAsync(stored, c.ProjectId, section?.Id, prepared, preview, segments, ct);
            RoadSectionVersion? current = section is null ? null : await db.RoadSectionVersions.SingleOrDefaultAsync(x => x.RoadSectionId == section.Id && x.IsCurrent, ct);
            if (current?.Id != confirm.ExpectedCurrentVersionId) Reject(409, "road_section_concurrency_conflict");
            if (section is null) { section = RoadSection.Create(Guid.NewGuid(), c.ProjectId, draft.RoadCode, draft.RoadName); db.Add(section); }
            if (current is not null) { current.ClearCurrent(); await db.SaveChangesAsync(ct); }
            var line = stored.SourceKind == "NATIVE_ALIGNMENT"
                ? new GeometryFactory(new PrecisionModel(), stored.SourceCrs).CreateLineString(((double[][])prepared.MetricCenterline.Coordinates).Select(p => new Coordinate(p[0], p[1])).ToArray())
                : new GeometryFactory(new PrecisionModel(), srid).CreateLineString(stored.Coordinates!.Select(p => new Coordinate(p.X, p.Y)).ToArray());
            var version = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, (current?.VersionNo ?? 0) + 1, true, line, confirm.EffectiveFrom, confirm.Reason);
            if (stored.NativeAlignment is { } native)
            {
                version.PinNativeProfile(native.CrsProfileRevisionId);
                db.Add(new NativeRouteVersionFacts { RoadSectionVersionId = version.Id, ProjectId = c.ProjectId, CrsProfileRevisionId = native.CrsProfileRevisionId, RouteSystemId = stored.RouteSystemId!.Value, RouteKind = stored.RouteKind!, ParentRouteVersionId = stored.ParentRouteVersionId, JunctionOffsetMeters = stored.JunctionOffsetMeters, CanonicalLengthMeters = prepared.LengthMeters, DeclaredLengthMeters = stored.DeclaredLengthMeters, CalibrationJson = stored.ChainageCalibration is null ? null : JsonSerializer.Serialize(stored.ChainageCalibration), SampleOnly = prepared.SampleOnly });
            }
            db.Add(version); draft.Confirm(section.Id);
            db.Add(RoadGeometryMetadata.Create(version.Id, draft.Id, JsonSerializer.Serialize(stored), Hash(new { srid, coordinates = stored.Coordinates, stored.NativeAlignment, stored.WidthProfile, stored.StationOriginMeters, stored.SurveyWidthMeters }), c.ActorId, prepared.Wgs84Centerline is null ? null : JsonSerializer.Serialize(prepared.Wgs84Centerline), clock.GetUtcNow()));
            if (current is not null)
            {
                var refs = new List<GeometryAffectedReferenceFact>();
                var field = await db.FieldInspectionTasks.AsNoTracking().Where(x => x.ProjectId == c.ProjectId && x.RoadSectionVersionId == current.Id).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
                refs.AddRange(field.Select(x => new GeometryAffectedReferenceFact("FIELD_TASK", x.Id, current.Id, x.SegmentSetId,
                    x.SegmentSetId.HasValue ? "PINNED_REFERENCE" : "INCOMPLETE", x.SegmentSetId.HasValue ? [] : ["LEGACY_SEGMENT_SET_UNPINNED"])));
                var surveys = await (from scope in db.SurveyRequestScopes.AsNoTracking() join task in db.SurveyRequests.AsNoTracking() on scope.SurveyRequestId equals task.Id where task.ProjectId == c.ProjectId && scope.RouteSectionVersionId == current.Id select new { task.Id, scope.SegmentSetId }).Distinct().ToArrayAsync(ct);
                refs.AddRange(surveys.Select(x => new GeometryAffectedReferenceFact("SURVEY_TASK", x.Id, current.Id, x.SegmentSetId, "READY", [])));
                var defects = await db.Defects.AsNoTracking().Where(x => x.ProjectId == c.ProjectId && x.RoadSectionVersionId == current.Id).Select(x => x.Id).ToArrayAsync(ct);
                refs.AddRange(defects.Select(x => new GeometryAffectedReferenceFact("DEFECT", x, current.Id, null, "INCOMPLETE", ["SEGMENT_SET_NOT_PINNED"])));
                var layouts = await db.Set<PavementLayoutRevision>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId && x.RouteVersionId == current.Id).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
                refs.AddRange(layouts.Select(x => new GeometryAffectedReferenceFact("LAYOUT", x.Id, current.Id, x.SegmentSetId, "PINNED_REFERENCE", [])));
                var maps = await db.Set<GeometryMapPublication>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId && x.RouteVersionId == current.Id).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
                refs.AddRange(maps.Select(x => new GeometryAffectedReferenceFact("MAP", x.Id, current.Id, x.SegmentSetId, "PINNED_REFERENCE", [])));
                var branches = await db.Set<NativeRouteVersionFacts>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId && x.ParentRouteVersionId == current.Id).Select(x => x.RoadSectionVersionId).ToArrayAsync(ct);
                refs.AddRange(branches.Select(x => new GeometryAffectedReferenceFact("BRANCH", x, current.Id, null, "PINNED_REFERENCE", [])));
                db.Add(new GeometryLocationImpact { Id = Guid.NewGuid(), ProjectId = c.ProjectId, PreviousRouteVersionId = current.Id, NewRouteVersionId = version.Id, AffectedReferencesJson = JsonSerializer.Serialize(refs.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id).ThenBy(x => x.SegmentSetId).ToArray()), RecordedBy = c.ActorId, RecordedAt = clock.GetUtcNow() });
            }
            await db.SaveChangesAsync(ct);
            var confirmed = await RouteView(c.ProjectId, section.Id, version, ct);
            return new(201, Value: confirmed, Version: Hash(confirmed));
        }
        var readGeometry = c.Action is "package" or "set-get" or "geometry-get";
        var routeQuery = readGeometry ? db.RoadSectionVersions.AsNoTracking() : db.RoadSectionVersions;
        var route = await routeQuery.SingleOrDefaultAsync(x => x.Id == c.RouteVersionId && (c.RoadSectionId == null || x.RoadSectionId == c.RoadSectionId), ct);
        if (route is null) Reject(404, "road_section_version_not_found");
        if (section is null) section = await db.RoadSections.SingleOrDefaultAsync(x => x.Id == route!.RoadSectionId && x.ProjectId == c.ProjectId, ct);
        if (section is null) Reject(404, "road_section_version_not_found");
        var view = await RouteView(c.ProjectId, section!.Id, route!, ct);
        if (c.Action == "geometry-get") return new(200, Value: view, Version: Hash(view));
        RoadSegmentSet? set = null;
        if (c.SetId is Guid setId) { var setQuery = readGeometry ? db.RoadSegmentSets.AsNoTracking() : db.RoadSegmentSets; set = await setQuery.SingleOrDefaultAsync(x => x.Id == setId && x.RoadSectionVersionId == route!.Id, ct); if (set is null) Reject(404, "segment_set_not_found"); }
        if (c.Action is "set-get" or "package")
        {
            var setView = await SetView(set!, ct);
            if (c.Action == "package")
            {
                var package = new GeometryPackageViewFact("anh01.geometry.v1", c.ProjectId, view, setView);
                return new(200, Value: package, Version: Hash(package));
            }
            return new(200, Value: setView, Version: setView.Version);
        }
        if (view.MetadataStatus != "COMPLETE") Reject(422, "geometry_metadata_incomplete");
        if (c.Action is "set-preview" or "set-create" or "set-edit")
        {
            var definition = (SegmentDefinitionFact)c.Input!;
            var metadata = await db.Set<RoadGeometryMetadata>().AsNoTracking().SingleAsync(x => x.RoadSectionVersionId == route!.Id, ct);
            var nativeInput = Decode<GeometryDraftInputFact>(metadata.InputJson);
            definition = definition with { NativeAlignment = nativeInput.NativeAlignment, TessellationToleranceMeters = nativeInput.TessellationToleranceMeters, ChainageCalibration = nativeInput.ChainageCalibration };
            var computed = segments(route!.Id, route.Geometry, view.StationOriginMeters!.Value, definition);
            if (c.Action == "set-preview") return new(200, Value: computed);
            if (set is null) { set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id, "DRAFT"); db.Add(set); }
            else
            {
                Match(set.RowVersion, c.ExpectedVersion);
                if (set.Status != "DRAFT") Reject(409, "road_geometry_invalid_state");
                // A no-op must not replace referenced children without updating the parent's rowversion.
                if (set.DefinitionJson == JsonSerializer.Serialize(definition) && set.GeometryHash == computed.DefinitionHash)
                {
                    var unchanged = await SetView(set, ct);
                    return new(200, Value: unchanged, Version: unchanged.Version);
                }
                db.RoadSegments.RemoveRange(await db.RoadSegments.Where(x => x.SegmentSetId == set.Id).ToListAsync(ct));
                await db.SaveChangesAsync(ct);
            }
            set.Define(JsonSerializer.Serialize(definition), computed.DefinitionHash);
            var indexed = new LengthIndexedLine(route.Geometry);
            foreach (var part in computed.Segments)
            {
                var row = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, part.Sequence);
                var geometry = definition.NativeAlignment is null ? (LineString)indexed.ExtractLine(part.FromOffsetMeters, part.ToOffsetMeters)
                    : new GeometryFactory(new PrecisionModel(), route.Geometry.SRID).CreateLineString(((double[][])part.MetricGeometry.Coordinates).Select(p => new Coordinate(p[0], p[1])).ToArray());
                row.SetGeometry(part.FromOffsetMeters, part.ToOffsetMeters, view.StationOriginMeters.Value, geometry, part.StartStationMeters, part.EndStationMeters); db.Add(row);
            }
            await db.SaveChangesAsync(ct); var result = await SetView(set, ct); return new(c.Action == "set-create" ? 201 : 200, Value: result, Version: result.Version);
        }
        Match(set!.RowVersion, c.ExpectedVersion); if (set.Status != "DRAFT") Reject(409, "road_geometry_invalid_state");
        if ((await SetView(set, ct)).MetadataStatus != "COMPLETE") Reject(422, "geometry_metadata_incomplete");
        var publication = (SegmentPublishInputFact)c.Input!;
        if (string.IsNullOrWhiteSpace(publication.Reason)) Reject(422, "segment_validation_failed");
        if (!route!.IsCurrent) Reject(409, "road_section_concurrency_conflict");
        var previous = await db.RoadSegmentSets.SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id && x.Status == "PUBLISHED", ct);
        if (previous?.Id != publication.ExpectedPublishedSetId) Reject(409, "segment_publication_conflict");
        if (previous is not null) { previous.Supersede(); await db.SaveChangesAsync(ct); }
        set.Publish(c.ActorId, clock.GetUtcNow()); await db.SaveChangesAsync(ct); var published = await SetView(set, ct); return new(200, Value: published, Version: published.Version);
    }
    private static void Match(byte[] version, string? expected) { if (Version(version) != expected) Reject(412, "concurrency_conflict"); }
    private static GeometryDraftViewFact DraftView(RoadGeometryDraft d) { var i = Decode<GeometryDraftInputFact>(d.InputJson); return new(d.Id, d.ProjectId, d.RoadSectionId, d.RoadCode, d.RoadName, d.Status, i.SourceKind, i.SourceCrs, i.SourceFileId, d.SourceChecksum, i.TrackIndex, i.TrackSegmentIndex, Decode<GeometryPointFact[]?>(d.OriginalCoordinatesJson) ?? [], i.Coordinates ?? [], i.StationOriginMeters, i.WidthProfile, i.SurveyWidthMeters, d.CreatedBy, d.UpdatedBy, Version(d.RowVersion), i.NativeAlignment); }
    private static GeometryShapeFact Shape(LineString line) => new("LineString", line.Coordinates.Select(p => new[] { p.X, p.Y }).ToArray());
    private async Task<RoadGeometryVersionViewFact> RouteView(Guid project, Guid section, RoadSectionVersion route, CancellationToken ct)
    {
        var m = await db.Set<RoadGeometryMetadata>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct); var i = m is null ? null : Decode<GeometryDraftInputFact>(m.InputJson);
        var facts = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
        var profile = facts is null ? null : await db.Set<CrsProfileRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == facts.CrsProfileRevisionId, ct);
        return new(project, section, route.Id, route.VersionNo, route.IsCurrent, route.EffectiveFrom, m?.SourceDraftId, i?.SourceCrs, route.Geometry.SRID, i?.StationOriginMeters, i?.WidthProfile, i?.SurveyWidthMeters, Shape(route.Geometry), m?.Wgs84GeometryJson is null ? null : Decode<GeometryShapeFact>(m.Wgs84GeometryJson), facts?.CanonicalLengthMeters ?? route.Geometry.Length, m is null ? "LEGACY_INCOMPLETE" : "COMPLETE", m?.ApprovedBy, m?.ApprovedAt, m?.GeometryHash ?? Hash(route.Geometry.AsText()), facts?.CrsProfileRevisionId, facts?.RouteSystemId, facts?.RouteKind, facts?.ParentRouteVersionId, facts?.JunctionOffsetMeters, facts?.DeclaredLengthMeters, i?.ChainageCalibration, profile?.Status, profile?.SampleOnly ?? false);
    }
    private static CrsProfileViewFact ProfileView(CrsProfileRevision profile) => new(profile.Id, profile.ProjectId, profile.Code, profile.Revision, profile.Status, Decode<CrsProfileInputFact>(profile.PayloadJson), profile.CreatedBy, profile.CreatedAt);
    private async Task<GeometryDraftInputFact> ResolveNativeInputAsync(GeometryDraftInputFact input, Guid project, CancellationToken ct)
    {
        // Never trust a client-supplied profile snapshot; pin the immutable project-owned revision.
        input = input with { ResolvedCrsProfile = null };
        if (input.NativeAlignment is null) return input;
        var profile = await db.Set<CrsProfileRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.NativeAlignment.CrsProfileRevisionId && x.ProjectId == project, ct);
        if (profile is null) Reject(422, "crs_profile_scope_mismatch");
        if (profile!.SourceSrid != input.SourceCrs || input.NativeAlignment.SpatialSrid != profile.SourceSrid) Reject(422, "crs_profile_srid_mismatch");
        return input with { ResolvedCrsProfile = Decode<CrsProfileInputFact>(profile.PayloadJson) };
    }
    private static string[] NativeMissing(GeometryDraftInputFact input)
    {
        var missing = new List<string>();
        if (input.NativeAlignment is null) missing.Add("nativeAlignment");
        if (input.ResolvedCrsProfile is null) missing.Add("crsProfileRevision");
        if (input.RouteSystemId is null || input.RouteSystemId == Guid.Empty) missing.Add("routeSystemId");
        if (input.RouteKind is not ("MAIN" or "BRANCH")) missing.Add("routeKind");
        if (input.WidthProfile is null || input.WidthProfile.Length == 0 || input.SurveyWidthMeters <= 0) missing.Add("widthProfile");
        if (input.TessellationToleranceMeters is null) missing.Add("tessellationToleranceMeters");
        return missing.ToArray();
    }
    private async Task ValidateTopologyAsync(GeometryDraftInputFact input, Guid project, Guid? section, GeometryPreviewFact child,
        Func<GeometryDraftInputFact, int, GeometryPreviewFact> preview, Func<Guid, LineString, double, SegmentDefinitionFact, SegmentPreviewFact> segments, CancellationToken ct)
    {
        if (!await db.Set<RoadRouteSystem>().AnyAsync(x => x.Id == input.RouteSystemId && x.ProjectId == project, ct)) Reject(422, "route_system_scope_mismatch");
        if (input.RouteKind == "MAIN") { if (input.ParentRouteVersionId is not null || input.JunctionOffsetMeters is not null) Reject(422, "route_topology_invalid"); return; }
        var junction = input.JunctionOffsetMeters ?? double.NaN;
        if (input.RouteKind != "BRANCH" || input.ParentRouteVersionId is null || !double.IsFinite(junction)) Reject(422, "route_topology_invalid");
        var parent = await db.RoadSectionVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.ParentRouteVersionId, ct);
        var parentFacts = parent is null ? null : await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == parent.Id, ct);
        if (parent is null || parentFacts is null || parentFacts.ProjectId != project || parentFacts.RouteSystemId != input.RouteSystemId || parentFacts.CrsProfileRevisionId != input.NativeAlignment!.CrsProfileRevisionId || !parent.IsCurrent || junction < 0 || junction > parentFacts.CanonicalLengthMeters) Reject(422, "branch_parent_or_junction_invalid");
        var seen = new HashSet<Guid>(); var seenRoads = new HashSet<Guid>();
        if (section is Guid childSection) seenRoads.Add(childSection);
        var ancestor = parent;
        while (ancestor is not null)
        {
            if (seen.Count >= 100000 || !seen.Add(ancestor.Id) || !seenRoads.Add(ancestor.RoadSectionId)) Reject(422, "route_topology_cycle");
            var facts = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == ancestor.Id, ct);
            ancestor = facts?.ParentRouteVersionId is Guid next ? await db.RoadSectionVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == next, ct) : null;
        }
        var metadata = await db.Set<RoadGeometryMetadata>().AsNoTracking().SingleAsync(x => x.RoadSectionVersionId == parent!.Id, ct);
        var parentInput = Decode<GeometryDraftInputFact>(metadata.InputJson);
        var boundaries = junction == 0 || junction == parentFacts!.CanonicalLengthMeters ? new[] { 0d, parentFacts.CanonicalLengthMeters } : new[] { 0d, junction, parentFacts.CanonicalLengthMeters };
        var computed = segments(parent.Id, parent.Geometry, parentInput.StationOriginMeters, new(100, "KEEP", boundaries, parentInput.NativeAlignment, parentInput.TessellationToleranceMeters, parentInput.ChainageCalibration));
        var point = junction == parentFacts.CanonicalLengthMeters ? ((double[][])computed.Segments[^1].MetricGeometry.Coordinates)[^1]
            : ((double[][])computed.Segments[junction == 0 ? 0 : 1].MetricGeometry.Coordinates)[0];
        var start = ((double[][])child.MetricCenterline.Coordinates)[0];
        if (Math.Abs(point[0] - start[0]) > 1e-9 * Math.Max(1, Math.Abs(point[0])) || Math.Abs(point[1] - start[1]) > 1e-9 * Math.Max(1, Math.Abs(point[1]))) Reject(422, "branch_junction_mismatch");
    }
    private async Task GuardAsync(GeometryWorkflowCommand c, bool locked, CancellationToken ct)
    {
        if (locked) await Anh02ReceiptAuthority.LockAsync(db, c.ActorId, c.ProjectId, ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.ActorId, ct);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword || (c.Role is not null && c.Role != user.RoleCode) || !await db.Roles.AsNoTracking().AnyAsync(x => x.Code == user.RoleCode && x.IsActive, ct)) Reject(403, "access_forbidden");
        var read = c.Action is "draft-get" or "draft-readiness" or "draft-preview" or "geometry-get" or "set-get" or "set-preview" or "package" or "profile-get" or "profile-list" or "system-list";
        if (c.Action == "confirm" ? user.RoleCode != UserRoleCode.Supervisor : !read ? user.RoleCode != UserRoleCode.ProjectManager : user.RoleCode is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator)) Reject(403, "access_forbidden");
        if (user.RoleCode != UserRoleCode.Supervisor)
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (!await db.ProjectMembers.AsNoTracking().AnyAsync(x => x.ProjectId == c.ProjectId && x.UserId == c.ActorId && x.RoleCode == user.RoleCode && x.Status == ProjectMemberStatus.Active && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today), ct)) Reject(403, "access_forbidden");
        }
        if (user.RoleCode == UserRoleCode.DroneOperator && (c.Action != "package" || c.RouteVersionId is not Guid route || c.SetId is not Guid set || !await CanReadAssignedGeometryAsync(c.ActorId, c.ProjectId, route, set, ct))) Reject(403, "access_forbidden");
        if (c.RoadSectionId is Guid section && !await db.RoadSections.AsNoTracking().AnyAsync(x => x.Id == section && x.ProjectId == c.ProjectId, ct)) Reject(404, "road_section_not_found");
        if (c.DraftId is Guid draft && !c.Action.StartsWith("profile", StringComparison.Ordinal) && !await db.Set<RoadGeometryDraft>().AsNoTracking().AnyAsync(x => x.Id == draft && x.ProjectId == c.ProjectId && (c.RoadSectionId == null || x.RoadSectionId == c.RoadSectionId), ct)) Reject(404, "road_geometry_draft_not_found");
        if (c.RouteVersionId is Guid version && !await (from v in db.RoadSectionVersions.AsNoTracking() join s in db.RoadSections.AsNoTracking() on v.RoadSectionId equals s.Id where v.Id == version && s.ProjectId == c.ProjectId && (c.RoadSectionId == null || v.RoadSectionId == c.RoadSectionId) select v.Id).AnyAsync(ct)) Reject(404, "road_section_version_not_found");
        if (c.SetId is Guid setId && !await db.RoadSegmentSets.AsNoTracking().AnyAsync(x => x.Id == setId && x.RoadSectionVersionId == c.RouteVersionId, ct)) Reject(404, "segment_set_not_found");
    }
    private async Task<SegmentSetViewFact> SetView(RoadSegmentSet set, CancellationToken ct)
    {
        var rows = await db.RoadSegments.AsNoTracking().Where(x => x.SegmentSetId == set.Id).OrderBy(x => x.Sequence).ToListAsync(ct);
        var parts = rows.Select(x =>
        {
            var missing = new List<string>();
            if (x.FromOffsetMeters is null) missing.Add("fromOffsetMeters");
            if (x.ToOffsetMeters is null) missing.Add("toOffsetMeters");
            if (x.StartStationMeters is null) missing.Add("startStationMeters");
            if (x.EndStationMeters is null) missing.Add("endStationMeters");
            if (x.Geometry is null) missing.Add("metricGeometry");
            return new SegmentGeometryViewFact(x.Id, x.Sequence, x.FromOffsetMeters, x.ToOffsetMeters, x.StartStationMeters, x.EndStationMeters,
                x.ToOffsetMeters - x.FromOffsetMeters, x.Geometry is null ? null : Shape(x.Geometry), null, missing.ToArray());
        }).ToArray();
        var setMissing = new List<string>();
        if (set.DefinitionJson is null) setMissing.Add("definition");
        if (set.GeometryHash is null) setMissing.Add("geometryHash");
        if (parts.Length == 0) setMissing.Add("segments");
        if (parts.Any(x => x.MissingMetadata.Length > 0)) setMissing.Add("segments.metadata");
        return new(set.Id, set.RoadSectionVersionId, set.Status, set.DefinitionJson is null ? null : Decode<SegmentDefinitionFact>(set.DefinitionJson), set.GeometryHash,
            parts, set.PublishedBy, set.PublishedAt, Version(set.RowVersion), setMissing.Count == 0 ? "COMPLETE" : "LEGACY_INCOMPLETE", setMissing.ToArray());
    }
}
