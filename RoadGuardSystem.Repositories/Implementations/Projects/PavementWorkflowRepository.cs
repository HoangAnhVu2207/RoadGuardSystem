using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Projects;

public sealed class PavementWorkflowRepository(RoadGuardDbContext db, IdempotencyOperationService idempotency,
    TimeProvider timeProvider) : IPavementWorkflowRepository
{
    private static readonly JsonSerializerOptions PublicJson = new(JsonSerializerDefaults.Web);
    private sealed class Rejected(int status, string code) : Exception
    { public GeometryWorkflowResult Result { get; } = new(status, code); }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Reject(int status, string code) => throw new Rejected(status, code);
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
    private static T Decode<T>(string json) => JsonSerializer.Deserialize<T>(json, PublicJson) ?? throw new InvalidOperationException("Invalid persisted pavement snapshot.");

    private sealed class CommittedReceipt : Exception { }
    private sealed record SourceCapture(ReporterFileFacts File, Guid ScopeId, Guid? TargetId, DateTimeOffset ScopeCreatedAt, Guid? UploadedByUserId);
    public async Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, PavementWorkflowCommand command,
        Func<CancellationToken, Task<bool>> scopeGuard,
        Func<GeometryDraftInputFact?, LineString, PavementPlanCreateInputFact, PavementGeometryPreviewFact> plan,
        Func<PavementGeometryPreviewFact, AsBuiltLayoutInputFact, PavementGeometryPreviewFact> asBuilt,
        Func<GeometryDraftInputFact, int, GeometryPreviewFact> geometryPreview,
        Func<GeometryMapSnapshotFact, PavementLayerQuery, string, GeometryMapPageFact> page, CancellationToken cancellationToken)
    {
        var c = command; var ct = cancellationToken;
        async Task Guard(CancellationToken token)
        {
            await Anh02ReceiptAuthority.LockAsync(db, c.ActorId, c.ProjectId, token);
            var read = c.Action is "layout-get" or "manifest" or "page" or "impact-get" or "impact-list";
            if (read ? role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) : role != UserRoleCode.ProjectManager)
                Reject(403, "access_forbidden");
            if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(c.ActorId, role, token) ||
                !await db.Projects.AsNoTracking().AnyAsync(x => x.Id == c.ProjectId, token) ||
                !await scopeGuard(token)) Reject(403, "access_forbidden");
            // Protect resource scope before both new effects and receipt/conflict recovery.
            // Historical receipt access deliberately does not require the old workflow status.
            if (c.Action == "asbuilt-create" && c.Input is AsBuiltLayoutInputFact built) await Layout(c.ProjectId, built.SourcePlanId, token);
            if (c.Action == "publish" && c.Input is GeometryMapPublishInputFact publication) await Layout(c.ProjectId, publication.LayoutRevisionId, token);
            if (c.Action == "plan-create")
            {
                var route = await Route(c.ProjectId, c.RouteVersionId, token);
                if (!await db.RoadSegmentSets.AnyAsync(x => x.Id == c.SegmentSetId && x.RoadSectionVersionId == route.Id, token))
                    Reject(409, "geometry_version_mismatch");
            }
            if (c.Action == "impact-decide" && !await db.Set<GeometryLocationImpact>().AnyAsync(x => x.Id == c.ResourceId && x.ProjectId == c.ProjectId, token))
                Reject(404, "not_found");
            if (c.Action == "impact-create" && c.Input is GeometryImpactInputFact impact)
            { await Route(c.ProjectId, impact.PreviousRouteVersionId, token); await Route(c.ProjectId, impact.NewRouteVersionId, token); }
        }
        try
        {
            if (c.Action is "layout-get" or "manifest" or "page" or "impact-get" or "impact-list")
            {
                if (db.Database.CurrentTransaction is not null)
                { await Guard(ct); return await Read(c, page, ct); }
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    await Guard(ct); var result = await Read(c, page, ct);
                    await transaction.CommitAsync(ct); return result;
                });
            }
            var fingerprint = Hash(new { c.ProjectId, c.Action, c.RouteVersionId, c.SegmentSetId, c.ResourceId, c.Input, c.ExpectedContentHash });
            var outcome = await idempotency.ExecuteSerializableAsync(c.ActorId, c.ProjectId, $"h2.pavement.{c.Action}.v1", c.Key!, fingerprint,
                async token =>
                {
                    await Guard(token);
                    var existing = await db.IdempotencyRecords.AsNoTracking().AnyAsync(x => x.ActorUserId == c.ActorId &&
                        x.ProjectId == c.ProjectId && x.Operation == $"h2.pavement.{c.Action}.v1" && x.IdempotencyKey == c.Key, token);
                    if (existing) throw new CommittedReceipt();
                    if (!await db.Projects.AsNoTracking().AnyAsync(x => x.Id == c.ProjectId && x.Status == ProjectStatus.Active, token))
                        Reject(409, "project_not_active");
                    var value = await Write(c, plan, asBuilt, geometryPreview, token);
                    var now = timeProvider.GetUtcNow();
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.ActorId, now, $"pavement_{c.Action}", "PavementWorkflow",
                        value.Id, null, null, "Versioned pavement workflow", "h2.pavement", null));
                    db.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), $"h2.pavement.{c.Action}.v1", now, null,
                        JsonSerializer.Serialize(new { c.ProjectId, resourceId = value.Id })));
                    await db.SaveChangesAsync(token);
                    return (value.Id, JsonSerializer.Serialize(value.Value, PublicJson));
                }, ct, receiptAccessGuard: Guard);
            return outcome.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_conflict")
                : new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: Decode<JsonElement>(outcome.OutcomeJson));
        }
        catch (Rejected e) { return e.Result; }
        catch (CommittedReceipt)
        {
            db.ChangeTracker.Clear();
            // Project authority locks serialized a competing writer. Its receipt is durable;
            // re-enter the shared guarded lookup after rollback without a second effect.
            return await ExecuteAsync(role, command, scopeGuard, plan, asBuilt, geometryPreview, page, cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        { return new(409, "resource_conflict"); }
    }

    private async Task<GeometryWorkflowResult> Read(PavementWorkflowCommand c,
        Func<GeometryMapSnapshotFact, PavementLayerQuery, string, GeometryMapPageFact> page, CancellationToken ct)
    {
        if (c.Action == "layout-get") return new(200, Value: View(await Layout(c.ProjectId, c.ResourceId, ct)));
        if (c.Action == "impact-list")
        {
            var rows = await db.Set<GeometryLocationImpact>().AsNoTracking().Where(x => x.ProjectId == c.ProjectId &&
                (c.RouteVersionId == null || x.PreviousRouteVersionId == c.RouteVersionId) &&
                (c.SegmentSetId == null || x.NewRouteVersionId == c.SegmentSetId))
                .OrderByDescending(x => x.RecordedAt).ThenBy(x => x.Id).Take(100).ToArrayAsync(ct);
            return new(200, Value: rows.Select(ImpactView).ToArray());
        }
        if (c.Action == "impact-get")
        {
            var impact = await db.Set<GeometryLocationImpact>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.ResourceId && x.ProjectId == c.ProjectId, ct);
            if (impact is null) throw new Rejected(404, "not_found");
            return new(200, Value: ImpactView(impact));
        }
        var publication = await db.Set<GeometryMapPublication>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.ResourceId && x.ProjectId == c.ProjectId, ct)
            ?? throw new Rejected(404, "not_found");
        var snapshot = Decode<GeometryMapSnapshotFact>(publication.SnapshotJson);
        if (c.RouteVersionId is { } route && route != publication.RouteVersionId ||
            c.SegmentSetId is { } set && set != publication.SegmentSetId) Reject(409, "geometry_version_mismatch");
        return new(200, Value: c.Action == "manifest" ? snapshot.Manifest
            : page(snapshot, c.Input as PavementLayerQuery ?? throw new ArgumentException("A layer query is required."), c.ExpectedContentHash!));
    }

    private async Task<(Guid Id, object Value)> Write(PavementWorkflowCommand c,
        Func<GeometryDraftInputFact?, LineString, PavementPlanCreateInputFact, PavementGeometryPreviewFact> plan,
        Func<PavementGeometryPreviewFact, AsBuiltLayoutInputFact, PavementGeometryPreviewFact> asBuilt,
        Func<GeometryDraftInputFact, int, GeometryPreviewFact> geometryPreview, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        if (c.Action == "plan-create")
        {
            var (route, set, metadata, native) = await Pins(c.ProjectId, c.RouteVersionId, c.SegmentSetId, ct);
            var input = c.Input as PavementPlanCreateInputFact ?? throw new ArgumentException("A plan is required.");
            var geometry = plan(metadata is null ? null : Decode<GeometryDraftInputFact>(metadata.InputJson), route.Geometry, input);
            var definition = new
            {
                input,
                RouteHash = metadata?.GeometryHash ?? Hash(route.Geometry.AsText()),
                SetHash = set.GeometryHash,
                SetVersion = Convert.ToBase64String(set.RowVersion),
                Native = native
            };
            var layout = new PavementLayoutRevision
            {
                Id = Guid.NewGuid(),
                ProjectId = c.ProjectId,
                RouteVersionId = route.Id,
                SegmentSetId = set.Id,
                CrsProfileRevisionId = native?.CrsProfileRevisionId,
                DefinitionJson = JsonSerializer.Serialize(definition),
                SnapshotJson = JsonSerializer.Serialize(geometry),
                ContentHash = Hash(new { definition, geometry }),
                CreatedBy = c.ActorId,
                CreatedAt = now
            };
            db.Add(layout); return (layout.Id, View(layout));
        }
        if (c.Action == "asbuilt-create")
        {
            var input = c.Input as AsBuiltLayoutInputFact ?? throw new ArgumentException("As-built evidence is required.");
            var source = await Layout(c.ProjectId, input.SourcePlanId, ct);
            if (source.Kind != "PLANNED") Reject(409, "planned_source_required");
            if (source.ContentHash != c.ExpectedContentHash) Reject(412, "content_hash_mismatch");
            await Pins(c.ProjectId, source.RouteVersionId, source.SegmentSetId, ct);
            var geometry = asBuilt(Decode<PavementGeometryPreviewFact>(source.SnapshotJson), input);
            var files = new List<SourceCapture>();
            foreach (var fileId in input.Slabs.Where(x => x.SourceFileId.HasValue).Select(x => x.SourceFileId!.Value).Distinct().Order())
            {
                await db.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}").AsNoTracking().ToListAsync(ct);
                await db.FileScopes.FromSqlInterpolated($"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}").AsNoTracking().ToListAsync(ct);
                await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}").AsNoTracking().ToListAsync(ct);
                var file = await new AnhHuyFactsRepository(db).GetFileAsync(fileId, ct);
                if (file is null || file.ProjectId != c.ProjectId || file.State != "VERIFIED") Reject(409, "source_file_not_ready");
                var fileScope = await db.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == fileId, ct);
                var uploader = await db.Files.AsNoTracking().Where(x => x.Id == fileId).Select(x => x.UploadedByUserId).SingleAsync(ct);
                if (uploader != file!.OwnerId) Reject(409, "source_file_provenance_unresolved");
                if (file!.Purpose is not ("ROUTE_SOURCE" or "DOCUMENT" or "SURVEY_VIDEO" or "TELEMETRY")) Reject(403, "source_file_purpose_forbidden");
                if (file.Purpose is "SURVEY_VIDEO" or "TELEMETRY")
                {
                    if (!await db.SurveyRequests.AnyAsync(x => x.Id == fileScope.TargetId && x.ProjectId == c.ProjectId, ct))
                        Reject(403, "source_file_purpose_forbidden");
                }
                files.Add(new(file, fileScope.Id, fileScope.TargetId, fileScope.CreatedAt, uploader));
            }
            var definition = new { input, SourcePlanHash = source.ContentHash, SourceFiles = files };
            var layout = new PavementLayoutRevision
            {
                Id = Guid.NewGuid(),
                ProjectId = c.ProjectId,
                RouteVersionId = source.RouteVersionId,
                SegmentSetId = source.SegmentSetId,
                SourcePlanId = source.Id,
                CrsProfileRevisionId = source.CrsProfileRevisionId,
                Kind = "AS_BUILT",
                DefinitionJson = JsonSerializer.Serialize(definition),
                SnapshotJson = JsonSerializer.Serialize(geometry),
                ContentHash = Hash(new { definition, geometry }),
                CreatedBy = c.ActorId,
                CreatedAt = now
            };
            db.Add(layout);
            foreach (var file in files)
                db.Add(new PavementSourceFileReference
                {
                    Id = Guid.NewGuid(),
                    LayoutRevisionId = layout.Id,
                    FileId = file.File.FileId,
                    ContentChecksum = file.File.Checksum,
                    CaptureFactsJson = JsonSerializer.Serialize(file)
                });
            return (layout.Id, View(layout));
        }
        if (c.Action == "publish")
        {
            var input = c.Input as GeometryMapPublishInputFact ?? throw new ArgumentException("A publication input is required.");
            Reason(input.Reason);
            if (input.PublicationMode == "OFFICIAL") Reject(409, "official_crs_acceptance_pending");
            if (input.PublicationMode != "SAMPLE") Reject(422, "publication_mode_invalid");
            var layout = await Layout(c.ProjectId, input.LayoutRevisionId, ct);
            if (layout.ContentHash != c.ExpectedContentHash) Reject(412, "content_hash_mismatch");
            var (route, set, metadata, native) = await Pins(c.ProjectId, layout.RouteVersionId, layout.SegmentSetId, ct);
            if (layout.CrsProfileRevisionId != native?.CrsProfileRevisionId) Reject(409, "geometry_version_mismatch");
            var profile = native is null ? null : await db.Set<CrsProfileRevision>().AsNoTracking().SingleAsync(x => x.Id == native.CrsProfileRevisionId && x.ProjectId == c.ProjectId, ct);
            var id = Guid.NewGuid();
            var snapshot = await Snapshot(id, layout, route, set, metadata, profile, geometryPreview, ct);
            var publication = new GeometryMapPublication
            {
                Id = id,
                ProjectId = c.ProjectId,
                RouteVersionId = route.Id,
                SegmentSetId = set.Id,
                LayoutRevisionId = layout.Id,
                CrsProfileRevisionId = layout.CrsProfileRevisionId,
                ContentHash = snapshot.Manifest.ContentHash,
                SnapshotJson = JsonSerializer.Serialize(snapshot, PublicJson),
                PublicationMode = "SAMPLE",
                PublishedBy = c.ActorId,
                PublishedAt = now
            };
            db.Add(publication); return (id, snapshot.Manifest);
        }
        if (c.Action == "impact-create")
        {
            var input = c.Input as GeometryImpactInputFact ?? throw new ArgumentException("Impact versions are required."); Reason(input.Reason);
            if (input.PreviousRouteVersionId == input.NewRouteVersionId) Reject(422, "impact_versions_invalid");
            var previous = await Route(c.ProjectId, input.PreviousRouteVersionId, ct);
            var next = await Route(c.ProjectId, input.NewRouteVersionId, ct);
            if (previous.RoadSectionId != next.RoadSectionId) Reject(409, "impact_road_section_mismatch");
            var references = await ImpactReferences(c.ProjectId, input.PreviousRouteVersionId, ct);
            var impact = new GeometryLocationImpact
            {
                Id = Guid.NewGuid(),
                ProjectId = c.ProjectId,
                PreviousRouteVersionId = input.PreviousRouteVersionId,
                NewRouteVersionId = input.NewRouteVersionId,
                AffectedReferencesJson = JsonSerializer.Serialize(references),
                RecordedBy = c.ActorId,
                RecordedAt = now
            };
            db.Add(impact); return (impact.Id, ImpactView(impact));
        }
        if (c.Action == "impact-decide")
        {
            var input = c.Input as GeometryImpactDecisionInputFact ?? throw new ArgumentException("An impact decision is required."); Reason(input.Reason);
            if (input.Action is not ("VERIFY" or "CONTINUE" or "STOP" or "REASSIGN")) Reject(422, "impact_action_invalid");
            var impact = await db.Set<GeometryLocationImpact>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == c.ResourceId && x.ProjectId == c.ProjectId, ct)
                ?? throw new Rejected(404, "not_found");
            var reference = Decode<GeometryAffectedReferenceFact[]>(impact.AffectedReferencesJson)
                .FirstOrDefault(x => x.Id == input.TaskId && x.Kind is "FIELD_TASK" or "SURVEY_TASK");
            if (reference is null) Reject(409, "task_not_in_impact");
            var decision = new GeometryLocationImpactDecision
            {
                Id = Guid.NewGuid(),
                ImpactId = impact.Id,
                TaskId = input.TaskId,
                Action = input.Action,
                Reason = input.Reason.Trim(),
                ActorId = c.ActorId,
                OccurredAt = now
            };
            db.Add(decision); return (decision.Id, new GeometryImpactDecisionView(decision.Id, impact.Id, input.TaskId, decision.Action, decision.Reason, now));
        }
        throw new ArgumentException("Unsupported pavement operation.");
    }

    private static void Reason(string reason)
    { if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) throw new ArgumentException("A bounded reason is required."); }
    private async Task<RoadSectionVersion> Route(Guid project, Guid? routeId, CancellationToken ct)
    {
        if (routeId is null || routeId == Guid.Empty) throw new Rejected(422, "route_version_required");
        await db.RoadSectionVersions.FromSqlInterpolated($"SELECT * FROM [RoadSectionVersions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={routeId}").AsNoTracking().ToListAsync(ct);
        return await (from route in db.RoadSectionVersions.AsNoTracking()
                      join road in db.RoadSections.AsNoTracking() on route.RoadSectionId equals road.Id
                      where route.Id == routeId && road.ProjectId == project
                      select route).SingleOrDefaultAsync(ct) ?? throw new Rejected(404, "not_found");
    }
    private async Task<(RoadSectionVersion Route, RoadSegmentSet Set, RoadGeometryMetadata? Metadata, NativeRouteVersionFacts? Native)> Pins(
        Guid project, Guid? routeId, Guid? setId, CancellationToken ct)
    {
        var route = await Route(project, routeId, ct);
        await db.RoadSegmentSets.FromSqlInterpolated($"SELECT * FROM [RoadSegmentSets] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={setId}").AsNoTracking().ToListAsync(ct);
        var set = await db.RoadSegmentSets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == setId && x.RoadSectionVersionId == route.Id, ct)
            ?? throw new Rejected(409, "geometry_version_mismatch");
        if (set.Status != "PUBLISHED") Reject(409, "segment_set_not_published");
        var metadata = await db.Set<RoadGeometryMetadata>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
        var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
        if (native is not null && (native.ProjectId != project || !await db.Set<CrsProfileRevision>().AnyAsync(x => x.Id == native.CrsProfileRevisionId && x.ProjectId == project, ct)))
            Reject(409, "geometry_version_mismatch");
        if (metadata is not null)
        {
            var input = Decode<GeometryDraftInputFact>(metadata.InputJson);
            if (input.NativeAlignment is { } alignment &&
                (native is null || alignment.CrsProfileRevisionId != native.CrsProfileRevisionId || alignment.SpatialSrid != route.Geometry.SRID))
                Reject(409, "geometry_version_mismatch");
            if (native is not null && input.NativeAlignment is null) Reject(409, "geometry_version_mismatch");
        }
        else if (native is not null) Reject(409, "native_route_metadata_missing");
        return (route, set, metadata, native);
    }
    private async Task<PavementLayoutRevision> Layout(Guid project, Guid? id, CancellationToken ct)
        => await db.Set<PavementLayoutRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ProjectId == project, ct)
            ?? throw new Rejected(404, "not_found");
    private static PavementLayoutView View(PavementLayoutRevision x) => new(x.Id, x.ProjectId, x.RouteVersionId, x.SegmentSetId,
        x.SourcePlanId, x.CrsProfileRevisionId, x.Kind, x.ContentHash, Decode<PavementGeometryPreviewFact>(x.SnapshotJson), x.CreatedAt);
    private static GeometryImpactView ImpactView(GeometryLocationImpact x) => new(x.Id, x.ProjectId, x.PreviousRouteVersionId,
        x.NewRouteVersionId, Decode<GeometryAffectedReferenceFact[]>(x.AffectedReferencesJson), x.RecordedAt);

    private async Task<GeometryAffectedReferenceFact[]> ImpactReferences(Guid project, Guid route, CancellationToken ct)
    {
        var result = new List<GeometryAffectedReferenceFact>();
        var field = await db.FieldInspectionTasks.AsNoTracking().Where(x => x.ProjectId == project && x.RoadSectionVersionId == route).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
        result.AddRange(field.Select(x => new GeometryAffectedReferenceFact("FIELD_TASK", x.Id, route, x.SegmentSetId,
            x.SegmentSetId.HasValue ? "PINNED_REFERENCE" : "INCOMPLETE", x.SegmentSetId.HasValue ? [] : ["LEGACY_SEGMENT_SET_UNPINNED"])));
        var survey = await (from scope in db.SurveyRequestScopes.AsNoTracking()
                            join task in db.SurveyRequests.AsNoTracking() on scope.SurveyRequestId equals task.Id
                            where task.ProjectId == project && scope.RouteSectionVersionId == route
                            select new { task.Id, scope.SegmentSetId }).Distinct().ToArrayAsync(ct);
        result.AddRange(survey.Select(x => new GeometryAffectedReferenceFact("SURVEY_TASK", x.Id, route, x.SegmentSetId, "READY", [])));
        var defects = await db.Defects.AsNoTracking().Where(x => x.ProjectId == project && x.RoadSectionVersionId == route).Select(x => x.Id).ToArrayAsync(ct);
        result.AddRange(defects.Select(x => new GeometryAffectedReferenceFact("DEFECT", x, route, null, "INCOMPLETE", ["SEGMENT_SET_NOT_PINNED"])));
        var layouts = await db.Set<PavementLayoutRevision>().AsNoTracking().Where(x => x.ProjectId == project && x.RouteVersionId == route).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
        result.AddRange(layouts.Select(x => new GeometryAffectedReferenceFact("LAYOUT", x.Id, route, x.SegmentSetId, "PINNED_REFERENCE", [])));
        var maps = await db.Set<GeometryMapPublication>().AsNoTracking().Where(x => x.ProjectId == project && x.RouteVersionId == route).Select(x => new { x.Id, x.SegmentSetId }).ToArrayAsync(ct);
        result.AddRange(maps.Select(x => new GeometryAffectedReferenceFact("MAP", x.Id, route, x.SegmentSetId, "PINNED_REFERENCE", [])));
        var branches = await db.Set<NativeRouteVersionFacts>().AsNoTracking().Where(x => x.ProjectId == project && x.ParentRouteVersionId == route).Select(x => x.RoadSectionVersionId).ToArrayAsync(ct);
        result.AddRange(branches.Select(x => new GeometryAffectedReferenceFact("BRANCH", x, route, null, "PINNED_REFERENCE", [])));
        return result.DistinctBy(x => (x.Kind, x.Id, x.RouteVersionId, x.SegmentSetId)).OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id).ThenBy(x => x.SegmentSetId).ToArray();
    }

    private async Task<GeometryMapSnapshotFact> Snapshot(Guid id, PavementLayoutRevision layout, RoadSectionVersion route,
        RoadSegmentSet set, RoadGeometryMetadata? metadata, CrsProfileRevision? profile,
        Func<GeometryDraftInputFact, int, GeometryPreviewFact> geometryPreview, CancellationToken ct)
    {
        var preview = Decode<PavementGeometryPreviewFact>(layout.SnapshotJson);
        var features = new List<GeometryMapFeatureFact>();
        static double[] Bbox(IEnumerable<GeometryPointFact> points)
        { var a = points.ToArray(); return [a.Min(x => x.X), a.Min(x => x.Y), a.Max(x => x.X), a.Max(x => x.Y)]; }
        var routePoints = route.Geometry.Coordinates.Select(x => new GeometryPointFact(x.X, x.Y)).ToArray();
        features.Add(new($"route:{route.Id:N}", "route", new("LineString", routePoints.Select(x => new[] { x.X, x.Y }).ToArray()), Bbox(routePoints), new { route.VersionNo }));
        var segments = await db.RoadSegments.AsNoTracking().Where(x => x.SegmentSetId == set.Id && x.RoadSectionVersionId == route.Id).OrderBy(x => x.Sequence).ToArrayAsync(ct);
        var missingSegments = segments.Length == 0 || segments.Any(x => x.Geometry is null);
        if (!missingSegments)
            foreach (var s in segments)
            {
                var points = s.Geometry!.Coordinates.Select(x => new GeometryPointFact(x.X, x.Y)).ToArray();
                features.Add(new($"segment:{s.Id:N}", "segments", new("LineString", points.Select(x => new[] { x.X, x.Y }).ToArray()), Bbox(points),
                    new { s.Sequence, s.FromOffsetMeters, s.ToOffsetMeters, s.StartStationMeters, s.EndStationMeters }));
            }
        foreach (var slab in preview.Slabs)
            features.Add(new($"slab:{layout.Id:N}:{slab.Key}", "slabs", new("Polygon", new[] { slab.Footprint.Select(x => new[] { x.X, x.Y }).ToArray() }), Bbox(slab.Footprint), slab));
        var layers = new List<GeometryMapLayerFact> { new("route", "READY", "LineString", 1, [], route.Geometry.SRID, layout.CrsProfileRevisionId, true),
            new GeometryMapLayerFact("segments", missingSegments ? "INCOMPLETE" : "READY", "LineString", missingSegments ? 0 : segments.Length, missingSegments ? ["SEGMENT_GEOMETRY_MISSING"] : []),
            new GeometryMapLayerFact("slabs", "READY", "Polygon", preview.Slabs.Length, []),
            new GeometryMapLayerFact("wgs84", "UNAVAILABLE", "Geometry", 0, ["OFFICIAL_CRS_ACCEPTANCE_PENDING"], 4326, layout.CrsProfileRevisionId, true) };
        var planned = layout.Kind == "PLANNED" ? preview : Decode<PavementGeometryPreviewFact>((await Layout(layout.ProjectId, layout.SourcePlanId, ct)).SnapshotJson);
        void SlabLayer(string key, PavementGeometryPreviewFact? geometry, Guid sourceId)
        {
            foreach (var slab in geometry?.Slabs ?? [])
                features.Add(new($"{key}:{sourceId:N}:{slab.Key}", key, new("Polygon", new[] { slab.Footprint.Select(x => new[] { x.X, x.Y }).ToArray() }), Bbox(slab.Footprint), slab));
            layers.Add(new(key, geometry is null ? "UNAVAILABLE" : "READY", "Polygon", geometry?.Slabs.Length ?? 0,
                geometry is null ? ["AS_BUILT_NOT_CAPTURED"] : [], route.Geometry.SRID, layout.CrsProfileRevisionId, true));
        }
        SlabLayer("plannedSlabs", planned, layout.SourcePlanId ?? layout.Id);
        SlabLayer("asBuiltSlabs", layout.Kind == "AS_BUILT" ? preview : null, layout.Id);
        var shapeFactory = new GeometryFactory(new PrecisionModel(), route.Geometry.SRID);
        if (planned.Plan is not null)
        {
            foreach (var strip in planned.Plan.Cells.GroupBy(x => x.Strip).OrderBy(x => x.Key))
            {
                var sequences = strip.Select(x => x.Sequence).ToHashSet();
                var polygons = planned.Slabs.Where(x => x.PlannedSequence is { } sequence && sequences.Contains(sequence))
                    .Select(x => (Geometry)shapeFactory.CreatePolygon(x.Footprint.Select(p => new Coordinate(p.X, p.Y)).ToArray())).ToList();
                var union = UnaryUnionOp.Union(polygons);
                features.Add(new($"strip:{layout.SourcePlanId ?? layout.Id:N}:{strip.Key}", "strips", Shape(union), Envelope(union), new { strip = strip.Key }));
            }
        }
        layers.Add(new("strips", planned.Plan is null ? "INCOMPLETE" : "READY", "PolygonOrMultiPolygon", features.Count(x => x.Layer == "strips"),
            planned.Plan is null ? ["PLANNED_GRID_MISSING"] : [], route.Geometry.SRID, layout.CrsProfileRevisionId, true));
        if (metadata is not null)
        {
            var input = Decode<GeometryDraftInputFact>(metadata.InputJson) with
            { ResolvedCrsProfile = profile is null ? null : Decode<CrsProfileInputFact>(profile.PayloadJson) };
            var actual = geometryPreview(input, route.Geometry.SRID);
            AddShape("roadSurface", actual.RoadSurface, route.Geometry.SRID);
            AddShape("surveyArea", actual.SurveyArea, route.Geometry.SRID);
            if (actual.Wgs84Centerline is not null) AddShape("wgs84Sample", actual.Wgs84Centerline, 4326);
            else layers.Add(new("wgs84Sample", "UNAVAILABLE", "LineString", 0, ["WGS84_TRANSFORM_NOT_CONFIGURED"], 4326, layout.CrsProfileRevisionId, true));
        }
        else
            foreach (var key in new[] { "roadSurface", "surveyArea", "wgs84Sample" })
                layers.Add(new(key, "INCOMPLETE", "Geometry", 0, ["LEGACY_ROUTE_METADATA_MISSING"], key == "wgs84Sample" ? 4326 : route.Geometry.SRID, null, true));
        var defects = await db.Defects.AsNoTracking().Where(x => x.ProjectId == layout.ProjectId && x.RoadSectionVersionId == route.Id).OrderBy(x => x.Id).ToArrayAsync(ct);
        foreach (var pair in new[] { (Key: "defects", Srid: route.Geometry.SRID), (Key: "defectsWgs84", Srid: 4326) })
        {
            foreach (var defect in defects.Where(x => x.Geometry is { IsEmpty: false } && x.Geometry.SRID == pair.Srid))
                features.Add(new($"defect:{defect.Id:N}", pair.Key, Shape(defect.Geometry!), Envelope(defect.Geometry!),
                    new { defect.Id, defect.DefectTypeCode, defect.Severity, defect.Status }));
            layers.Add(new(pair.Key, "READY", "Geometry", features.Count(x => x.Layer == pair.Key), [], pair.Srid,
                pair.Key == "defects" ? layout.CrsProfileRevisionId : null, true));
        }
        void AddShape(string key, GeometryShapeFact shape, int srid)
        {
            var coordinates = JsonSerializer.SerializeToElement(shape.Coordinates);
            var points = new List<GeometryPointFact>();
            static void Visit(JsonElement value, List<GeometryPointFact> points)
            {
                if (value.ValueKind != JsonValueKind.Array) return;
                var values = value.EnumerateArray().ToArray();
                if (values.Length >= 2 && values[0].ValueKind == JsonValueKind.Number && values[1].ValueKind == JsonValueKind.Number)
                    points.Add(new(values[0].GetDouble(), values[1].GetDouble()));
                else foreach (var child in values) Visit(child, points);
            }
            Visit(coordinates, points);
            if (points.Count == 0) throw new ArgumentException("Produced map geometry is empty.");
            features.Add(new($"{key}:{route.Id:N}", key, shape, Bbox(points), new { routeVersionId = route.Id }));
            layers.Add(new(key, "READY", shape.Type, 1, [], srid, layout.CrsProfileRevisionId, true));
        }
        for (var index = 0; index < layers.Count; index++)
            if (layers[index].SpatialSrid is null) layers[index] = layers[index] with
            { SpatialSrid = route.Geometry.SRID, CrsProfileRevisionId = layout.CrsProfileRevisionId, SampleOnly = true };
        var manifest = new GeometryMapManifestFact(id, layout.ProjectId, route.Id, set.Id, layout.Id, layout.CrsProfileRevisionId,
            route.Geometry.SRID, profile?.Status ?? "LEGACY_UNKNOWN", true, preview.DisplayToleranceMeters, "", layers.ToArray());
        var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
        manifest = manifest with
        {
            RouteSystemId = native?.RouteSystemId,
            RoadSectionId = route.RoadSectionId,
            CanonicalLengthMeters = native?.CanonicalLengthMeters ?? route.Geometry.Length,
            DeclaredLengthMeters = native?.DeclaredLengthMeters,
            ChainageStatus = native is null ? "LEGACY" : native.CalibrationJson is null ? "GEOMETRIC" : "CALIBRATED"
        };
        var hash = Hash(new
        {
            manifest,
            features,
            LayoutHash = layout.ContentHash,
            RouteHash = metadata?.GeometryHash,
            SetVersion = Convert.ToBase64String(set.RowVersion),
            SetHash = set.GeometryHash,
            ProfileHash = profile is null ? null : Hash(profile.PayloadJson)
        });
        return new(manifest with { ContentHash = hash }, features.ToArray());
    }
    private static double[] Envelope(Geometry geometry)
        => [geometry.EnvelopeInternal.MinX, geometry.EnvelopeInternal.MinY, geometry.EnvelopeInternal.MaxX, geometry.EnvelopeInternal.MaxY];
    private static GeometryShapeFact Shape(Geometry geometry) => geometry switch
    {
        Point point => new("Point", new[] { point.X, point.Y }),
        LineString line => new("LineString", line.Coordinates.Select(x => new[] { x.X, x.Y }).ToArray()),
        Polygon polygon => new("Polygon", new[] { polygon.ExteriorRing }.Concat(Enumerable.Range(0, polygon.NumInteriorRings).Select(polygon.GetInteriorRingN))
            .Select(x => x.Coordinates.Select(p => new[] { p.X, p.Y }).ToArray()).ToArray()),
        MultiPolygon multi => new("MultiPolygon", Enumerable.Range(0, multi.NumGeometries).Select(x => Shape(multi.GetGeometryN(x)).Coordinates).ToArray()),
        MultiPoint points => new("MultiPoint", Enumerable.Range(0, points.NumGeometries).Select(x => Shape(points.GetGeometryN(x)).Coordinates).ToArray()),
        MultiLineString lines => new("MultiLineString", Enumerable.Range(0, lines.NumGeometries).Select(x => Shape(lines.GetGeometryN(x)).Coordinates).ToArray()),
        _ => throw new ArgumentException("Unsupported persisted map shape.")
    };
}
