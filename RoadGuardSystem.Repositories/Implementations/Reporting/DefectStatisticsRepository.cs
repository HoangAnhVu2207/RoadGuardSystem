using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Reporting;
namespace RoadGuardSystem.Repositories.Implementations.Reporting;

public sealed class DefectStatisticsRepository(RoadGuardDbContext db, TimeProvider clock) : IDefectStatisticsRepository
{
    private const string Operation = "ld08a.defect-statistics.confirm.v1";
    private sealed class Rejected(int status, string code) : Exception
    { internal DefectStatisticsResult Result { get; } = new(status, code); }
    private sealed class ReceiptRace : Exception { }
    private async Task Authority(Guid actor, UserRoleCode role, Guid project, bool write, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (actor == Guid.Empty || project == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) ||
            write && role != UserRoleCode.ProjectManager || !await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token) ||
            !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == project && row.UserId == actor && row.RoleCode == role &&
                row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day && (row.ValidTo == null || row.ValidTo >= day), token))
            throw new Rejected(403, "access_forbidden");
    }
    public async Task<DefectStatisticsResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, ReportingFiltersFact filters, CancellationToken token)
    {
        if (filters.SegmentIds?.Length > 500 || filters.SegmentIds?.Any(id => id == Guid.Empty) == true || filters.RouteVersionId == Guid.Empty || filters.SegmentSetId == Guid.Empty)
            return new(400, "validation_error");
        try
        {
            return await new ReportingRepository(db).ReadConsistentlyAsync(async ct =>
            {
                await Authority(actor, role, project, false, ct);
                return new DefectStatisticsResult(200, Value: await ReadCore(project, filters, ct));
            }, token);
        }
        catch (Rejected rejected) { return rejected.Result; }
    }
    private async Task<DefectStatisticsRead> ReadCore(Guid project, ReportingFiltersFact filters, CancellationToken token)
    {
        var defects = await db.Defects.AsNoTracking().Where(row => row.ProjectId == project)
            .Select(row => new { row.Id, row.RoadSectionVersionId, Version = EF.Property<byte[]>(row, "RowVersion") }).ToArrayAsync(token);
        var obligations = await db.RepairObligations.AsNoTracking().Include(row => row.Scope).Where(row => row.ProjectId == project &&
            (db.Set<ObligationResponsibility>().Where(owner => owner.ObligationId == row.Id).Select(owner => (Guid?)owner.CurrentProjectId).SingleOrDefault() ?? row.ProjectId) == project)
            .OrderBy(row => row.Id).ToArrayAsync(token);
        var scopes = obligations.Where(row => defects.Any(defect => defect.Id == row.DefectId && defect.RoadSectionVersionId.HasValue)).Select(row =>
        {
            var defect = defects.Single(defect => defect.Id == row.DefectId); var scope = row.Scope;
            return new StatisticsScope(row.Id, row.DefectId, Convert.ToBase64String(defect.Version), LD06LifecycleAction.HashScope(row),
                scope.PhysicalRoadId, defect.RoadSectionVersionId!.Value, scope.LocationVersion, scope.From, scope.To, scope.OffsetFrom, scope.OffsetTo);
        }).ToArray();
        var segments = await (from segment in db.RoadSegments.AsNoTracking()
                              join set in db.RoadSegmentSets.AsNoTracking() on segment.SegmentSetId equals set.Id
                              join route in db.RoadSectionVersions.AsNoTracking() on segment.RoadSectionVersionId equals route.Id
                              join road in db.RoadSections.AsNoTracking() on route.RoadSectionId equals road.Id
                              where road.ProjectId == project && set.Status == "PUBLISHED" && set.RoadSectionVersionId == route.Id
                              select new StatisticsSegment(segment.Id, route.Id, set.Id, Convert.ToBase64String(set.RowVersion))).ToArrayAsync(token);
        if (filters.RouteVersionId is Guid routeFilter && !defects.Any(row => row.RoadSectionVersionId == routeFilter) && !segments.Any(row => row.RouteVersionId == routeFilter) ||
            filters.SegmentSetId is Guid setFilter && !segments.Any(row => row.SegmentSetId == setFilter && (!filters.RouteVersionId.HasValue || row.RouteVersionId == filters.RouteVersionId)) ||
            (filters.SegmentIds ?? []).Any(id => !segments.Any(row => row.Id == id && (!filters.RouteVersionId.HasValue || row.RouteVersionId == filters.RouteVersionId) &&
                (!filters.SegmentSetId.HasValue || row.SegmentSetId == filters.SegmentSetId)))) throw new Rejected(404, "not_found");
        var ids = defects.Select(row => row.Id).ToArray();
        var values = await db.GroundTruthMeasurements.AsNoTracking().Where(row => row.DefectId.HasValue && ids.Contains(row.DefectId.Value)).OrderBy(row => row.Id).ToArrayAsync(token);
        var measurements = values.Select(row => new StatisticsMeasurement(row.Id, row.DefectId!.Value, row.RoadSectionVersionId, row.MeasurementType.ToString(),
            row.Value, row.ValueState, row.Unit, RoadCoverageResolver.Hash(new
            {
                row.Id,
                row.DefectId,
                row.FieldInspectionSessionId,
                row.RoadSectionVersionId,
                row.MeasurementType,
                row.Value,
                row.ValueState,
                row.Unit,
                row.Dimension,
                row.MeasuredAt,
                row.UnknownReason,
                row.EvidenceFileId,
                row.Notes
            }), row.UnknownReason, row.EvidenceFileId)).ToArray();
        var history = await db.Set<DefectStatisticsSource>().AsNoTracking().Where(row => row.ProjectId == project).OrderBy(row => row.Id).ToArrayAsync(token);
        var active = history.Where(row => !history.Any(next => next.SupersedesId == row.Id)).ToArray();
        var valid = new List<DefectStatisticsSource>(); var missing = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in active)
        {
            var scope = scopes.SingleOrDefault(row => row.ObligationId == source.ObligationId);
            var linked = JsonSerializer.Deserialize<Guid[]>(source.SegmentIdsJson, RoadCoverageResolver.Json)!;
            var quantities = JsonSerializer.Deserialize<StatisticsQuantityInput[]>(source.QuantitiesJson, RoadCoverageResolver.Json)!;
            if (scope is null || scope.DefectId != source.DefectId || scope.DefectVersion != source.DefectVersion || scope.ScopeHash != source.ScopeHash ||
                scope.RouteVersionId != source.RouteVersionId || linked.Any(id => !segments.Any(row => row.Id == id && row.RouteVersionId == source.RouteVersionId)) ||
                quantities.Any(pin => !measurements.Any(row => row.Id == pin.MeasurementId && row.Version == pin.SourceVersion)))
            { missing.Add("STATISTICS_SOURCE_STALE"); continue; }
            var currentFacts = JsonSerializer.Serialize(new
            {
                scope,
                segments = segments.Where(row => linked.Contains(row.Id)).OrderBy(row => row.Id),
                measurements = measurements.Where(row => quantities.Any(pin => pin.MeasurementId == row.Id)).OrderBy(row => row.Id)
            }, RoadCoverageResolver.Json);
            if (currentFacts != source.SourceFactsJson) { missing.Add("STATISTICS_SOURCE_STALE"); continue; }
            valid.Add(source);
        }
        bool SelectedSegment(Guid id) => segments.Any(row => row.Id == id && (!filters.RouteVersionId.HasValue || row.RouteVersionId == filters.RouteVersionId) &&
            (!filters.SegmentSetId.HasValue || row.SegmentSetId == filters.SegmentSetId) && ((filters.SegmentIds ?? []).Length == 0 || filters.SegmentIds!.Contains(id)));
        var spatial = filters.RouteVersionId.HasValue || filters.SegmentSetId.HasValue || (filters.SegmentIds ?? []).Length > 0;
        var selected = valid.Where(row => (!filters.RouteVersionId.HasValue || row.RouteVersionId == filters.RouteVersionId) &&
            (!spatial || JsonSerializer.Deserialize<Guid[]>(row.SegmentIdsJson, RoadCoverageResolver.Json)!.Any(SelectedSegment))).ToArray();
        var unmapped = defects.LongCount(row => !valid.Any(source => source.DefectId == row.Id));
        if (unmapped > 0) missing.Add("DEFECT_SHARED_IDENTITY_OR_SCOPE_UNKNOWN");
        var counts = segments.Where(row => SelectedSegment(row.Id)).OrderBy(row => row.Id).Select(segment => new StatisticsSegmentCount(segment.Id,
            unmapped > 0 ? null : selected.Where(row => JsonSerializer.Deserialize<Guid[]>(row.SegmentIdsJson, RoadCoverageResolver.Json)!.Contains(segment.Id)).Select(row => row.DefectId).Distinct().LongCount(),
            selected.Where(row => JsonSerializer.Deserialize<Guid[]>(row.SegmentIdsJson, RoadCoverageResolver.Json)!.Contains(segment.Id)).Select(row => row.DefectId).Distinct().LongCount(),
            unmapped > 0 ? "PARTIAL" : "AVAILABLE")).ToArray();
        var output = new List<StatisticsQuantity>();
        foreach (var group in selected.GroupBy(row => row.SharedPartId))
        {
            var pins = group.SelectMany(row => JsonSerializer.Deserialize<StatisticsQuantityInput[]>(row.QuantitiesJson, RoadCoverageResolver.Json)!).Distinct().ToArray();
            if (pins.Length == 0)
            {
                missing.Add("QUANTITY_SOURCE_UNKNOWN"); output.Add(new(group.Key, Guid.Empty, "", "UNKNOWN", "UNKNOWN", null, "UNKNOWN", null,
                group.Any(row => row.Provenance == "TEST_ONLY") ? "TEST_ONLY" : "REAL_SOURCE", "QUANTITY_SOURCE_UNKNOWN"));
            }
            foreach (var pin in pins)
            {
                var measurement = measurements.Single(row => row.Id == pin.MeasurementId);
                if (spatial && pin.SegmentId.HasValue && !SelectedSegment(pin.SegmentId.Value)) continue;
                output.Add(new(group.Key, pin.MeasurementId, pin.SourceVersion, measurement.Type, measurement.Unit, measurement.Value, measurement.ValueState,
                    pin.SegmentId, group.Any(row => row.Provenance == "TEST_ONLY") ? "TEST_ONLY" : "REAL_SOURCE", measurement.UnknownReason));
                if (measurement.ValueState != "KNOWN") missing.Add("QUANTITY_VALUE_UNKNOWN");
            }
        }
        // These are current stock facts. Public period/cohort allocation remains a separate owner gate.
        var snapshot = new DefectStatisticsSnapshot(project, defects.OrderBy(row => row.Id).Select(row => new StatisticsDefect(row.Id, Convert.ToBase64String(row.Version))).ToArray(), defects.Select(row => row.Id).Distinct().LongCount(),
            spatial ? selected.Select(row => row.DefectId).Distinct().LongCount() : defects.Select(row => row.Id).Distinct().LongCount(),
            selected.Select(row => row.SharedPartId).Distinct().LongCount(), unmapped, counts, output.OrderBy(row => row.MeasurementId).ToArray(),
            missing.Order(StringComparer.Ordinal).ToArray(), selected, " ");
        snapshot = snapshot with { Version = RoadCoverageResolver.Hash(snapshot with { Version = "" }) };
        var read = new DefectStatisticsRead(scopes, segments, measurements, history, snapshot, ""); return read with { Version = RoadCoverageResolver.Hash(read) };
    }
    public async Task<DefectStatisticsResult> ConfirmAsync(DefectStatisticsCommand command, CancellationToken token)
    {
        var input = command.Input;
        if (input is null || input.ObligationId == Guid.Empty || input.SharedPartId == Guid.Empty || input.SupersedesId == Guid.Empty ||
            input.ScopeHash?.Length != 64 || string.IsNullOrWhiteSpace(input.DefectVersion) || input.DefectVersion.Length > 100 || input.Provenance is not ("TEST_ONLY" or "REAL_SOURCE") || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000 ||
            input.SegmentIds is not { Length: > 0 and <= 100 } || input.SegmentIds.Any(id => id == Guid.Empty) || input.SegmentIds.Distinct().Count() != input.SegmentIds.Length ||
            input.Quantities is null || input.Quantities.Length > 100 || input.Quantities.Any(row => row is null || row.MeasurementId == Guid.Empty || row.SourceVersion?.Length != 64 || row.SegmentId == Guid.Empty) ||
            input.Quantities.Select(row => row.MeasurementId).Distinct().Count() != input.Quantities.Length || string.IsNullOrWhiteSpace(command.Key) || command.Key.Length > 150 ||
            command.Key.Any(c => c < 33 || c > 126) || command.ExpectedVersion?.Length != 64) return new(400, "validation_error");
        async Task Guard(CancellationToken ct)
        {
            await Authority(command.ActorId, command.Role, command.ProjectId, true, ct);
            var view = await ReadCore(command.ProjectId, new(), ct);
            var receipt = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row => row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId && row.Operation == Operation && row.IdempotencyKey == command.Key, ct);
            if (receipt is not null)
            {
                var stored = view.History.Single(row => row.Id == receipt.OperationId);
                var storedScope = view.Scopes.SingleOrDefault(row => row.ObligationId == stored.ObligationId);
                var storedSegments = JsonSerializer.Deserialize<Guid[]>(stored.SegmentIdsJson, RoadCoverageResolver.Json)!;
                var storedQuantities = JsonSerializer.Deserialize<StatisticsQuantityInput[]>(stored.QuantitiesJson, RoadCoverageResolver.Json)!;
                var currentFacts = JsonSerializer.Serialize(new
                {
                    scope = storedScope,
                    segments = view.Segments.Where(row => storedSegments.Contains(row.Id)).OrderBy(row => row.Id),
                    measurements = view.Measurements.Where(row => storedQuantities.Any(pin => pin.MeasurementId == row.Id)).OrderBy(row => row.Id)
                }, RoadCoverageResolver.Json);
                if (currentFacts != stored.SourceFactsJson) throw new Rejected(409, "statistics_source_stale");
            }
            var scope = view.Scopes.SingleOrDefault(row => row.ObligationId == input.ObligationId);
            if (scope is null) throw new Rejected(403, "scope_forbidden");
            if (scope.ScopeHash != input.ScopeHash || scope.DefectVersion != input.DefectVersion) throw new Rejected(409, "scope_stale");
            if (input.SegmentIds.Any(id => !view.Segments.Any(row => row.Id == id && row.RouteVersionId == scope.RouteVersionId))) throw new Rejected(409, "segment_source_mismatch");
            if (input.Quantities.Any(pin => !view.Measurements.Any(row => row.Id == pin.MeasurementId && row.Version == pin.SourceVersion && row.RouteVersionId == scope.RouteVersionId &&
                (row.DefectId == scope.DefectId || view.History.Any(source => source.SharedPartId == input.SharedPartId && source.DefectId == row.DefectId))) ||
                pin.SegmentId.HasValue && !input.SegmentIds.Contains(pin.SegmentId.Value))) throw new Rejected(409, "quantity_source_mismatch");
            var shared = view.History.Where(row => row.SharedPartId == input.SharedPartId).ToArray();
            if (shared.Any(row => row.RoadSectionId != scope.RoadSectionId || row.LocationVersion != scope.LocationVersion ||
                row.From != scope.From || row.To != scope.To || row.OffsetFrom != scope.OffsetFrom || row.OffsetTo != scope.OffsetTo || row.Provenance != input.Provenance))
                throw new Rejected(409, "shared_identity_scope_conflict");
            if (view.History.Any(source => source.SharedPartId != input.SharedPartId &&
                JsonSerializer.Deserialize<StatisticsQuantityInput[]>(source.QuantitiesJson, RoadCoverageResolver.Json)!.Any(old => input.Quantities.Any(pin => pin.MeasurementId == old.MeasurementId))))
                throw new Rejected(409, "quantity_shared_identity_conflict");
            var pinIds = input.Quantities.Select(pin => pin.MeasurementId).ToArray();
            var synthetic = await db.GroundTruthMeasurements.AsNoTracking().AnyAsync(row => pinIds.Contains(row.Id) && row.Notes != null && row.Notes.StartsWith("TEST_ONLY"), ct);
            if (input.Provenance == "REAL_SOURCE" && (view.History.Any(row => row.Provenance == "TEST_ONLY" && (row.ObligationId == input.ObligationId ||
                JsonSerializer.Deserialize<StatisticsQuantityInput[]>(row.QuantitiesJson, RoadCoverageResolver.Json)!.Any(old => input.Quantities.Any(pin => pin.MeasurementId == old.MeasurementId)))) ||
                synthetic))
                throw new Rejected(409, "synthetic_source_cannot_activate");
        }
        try
        {
            var outcome = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId, command.ProjectId, Operation, command.Key,
                RoadCoverageResolver.Hash(command), async ct =>
                {
                    await Guard(ct);
                    if (await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId && row.Operation == Operation && row.IdempotencyKey == command.Key, ct))
                        throw new ReceiptRace();
                    var view = await ReadCore(command.ProjectId, new(), ct);
                    if (view.Version != command.ExpectedVersion) throw new Rejected(409, "concurrency_conflict");
                    var scope = view.Scopes.Single(row => row.ObligationId == input.ObligationId);
                    var active = view.History.Where(row => !view.History.Any(next => next.SupersedesId == row.Id)).ToArray();
                    var previous = input.SupersedesId.HasValue ? active.SingleOrDefault(row => row.Id == input.SupersedesId) : null;
                    if (input.SupersedesId.HasValue && (previous is null || previous.ObligationId != input.ObligationId || previous.SharedPartId != input.SharedPartId || previous.Provenance != input.Provenance) ||
                        active.Any(row => row.ObligationId == input.ObligationId && row.Id != previous?.Id)) throw new Rejected(409, "statistics_supersession_conflict");
                    var existing = active.Where(row => row.SharedPartId == input.SharedPartId && row.Id != previous?.Id).SelectMany(row =>
                        JsonSerializer.Deserialize<StatisticsQuantityInput[]>(row.QuantitiesJson, RoadCoverageResolver.Json)!).ToArray();
                    foreach (var pin in input.Quantities)
                    {
                        var measurement = view.Measurements.Single(row => row.Id == pin.MeasurementId);
                        if (existing.Any(old => old.MeasurementId == pin.MeasurementId && old != pin || view.Measurements.Any(row => row.Id == old.MeasurementId &&
                            row.Type == measurement.Type && row.Unit == measurement.Unit && row.Id != measurement.Id))) throw new Rejected(409, "quantity_allocation_conflict");
                    }
                    if (input.Quantities.GroupBy(pin => { var row = view.Measurements.Single(row => row.Id == pin.MeasurementId); return (row.Type, row.Unit); }).Any(group => group.Count() > 1))
                        throw new Rejected(409, "quantity_source_ambiguous");
                    // One source owns each shared-part measurement/allocation. Repeated identical pins reference that owner.
                    // Its explicit supersession can then revise the shared allocation without conflicting duplicate copies.
                    var ownedQuantities = input.Quantities.Where(pin => !existing.Contains(pin)).ToArray();
                    if (previous is not null && JsonSerializer.Deserialize<StatisticsQuantityInput[]>(previous.QuantitiesJson, RoadCoverageResolver.Json)!.Length > 0)
                        ownedQuantities = input.Quantities;
                    var id = Guid.NewGuid(); var now = clock.GetUtcNow();
                    var facts = JsonSerializer.Serialize(new
                    {
                        scope,
                        segments = view.Segments.Where(row => input.SegmentIds.Contains(row.Id)).OrderBy(row => row.Id),
                        measurements = view.Measurements.Where(row => ownedQuantities.Any(pin => pin.MeasurementId == row.Id)).OrderBy(row => row.Id)
                    }, RoadCoverageResolver.Json);
                    var source = new DefectStatisticsSource(id, command.ProjectId, scope.DefectId, scope.DefectVersion, scope.ObligationId, scope.ScopeHash,
                        scope.RoadSectionId, scope.RouteVersionId, scope.LocationVersion, scope.From, scope.To, scope.OffsetFrom, scope.OffsetTo, input.SharedPartId,
                        JsonSerializer.Serialize(input.SegmentIds.Order().ToArray(), RoadCoverageResolver.Json), JsonSerializer.Serialize(ownedQuantities.OrderBy(row => row.MeasurementId).ToArray(), RoadCoverageResolver.Json),
                        facts, input.Provenance, command.ActorId, now, input.Reason, input.SupersedesId);
                    db.Add(source); db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "defect_statistics_confirmed", "DefectStatisticsSource", id, null,
                        JsonSerializer.Serialize(new { sourceId = id }, RoadCoverageResolver.Json), input.Reason, Operation, id, ["sourceId"]));
                    db.OutboxMessages.Add(OutboxMessage.Create(id, "defect.statistics.confirmed.v1", now, id,
                        JsonSerializer.Serialize(new { sourceId = id, projectId = command.ProjectId }, RoadCoverageResolver.Json)));
                    await db.SaveChangesAsync(ct); await Guard(ct);
                    return (id, JsonSerializer.Serialize(await ReadCore(command.ProjectId, new(), ct), RoadCoverageResolver.Json));
                }, token, receiptAccessGuard: Guard);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: JsonSerializer.Deserialize<DefectStatisticsRead>(outcome.OutcomeJson, RoadCoverageResolver.Json));
        }
        catch (Rejected rejected) { db.ChangeTracker.Clear(); return rejected.Result; }
        catch (ReceiptRace) { db.ChangeTracker.Clear(); return await ConfirmAsync(command, token); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
}
