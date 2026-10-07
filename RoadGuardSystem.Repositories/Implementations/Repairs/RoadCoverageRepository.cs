using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed class RoadCoverageRepository(RoadGuardDbContext db, TimeProvider clock) : IRoadCoverageRepository
{
    private const string Operation = "ld07.road-coverage.confirm.v1";
    private sealed class ReceiptRace : Exception { }
    private sealed class Rejected(int status, string code) : Exception
    { public RoadCoverageResult Result { get; } = new(status, code); }
    private async Task Authority(Guid actor, UserRoleCode role, Guid project, bool write, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || write && role != UserRoleCode.Supervisor ||
            !await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token) ||
            !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == project && row.UserId == actor &&
                row.RoleCode == role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day &&
                (row.ValidTo == null || row.ValidTo >= day), token)) throw new Rejected(403, "access_forbidden");
    }
    public async Task<RoadCoverageResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await Authority(actor, role, project, false, token);
                var view = await ReadCore(project, token); await tx.CommitAsync(token); return new RoadCoverageResult(200, Value: view);
            });
        }
        catch (Rejected rejected) { return rejected.Result; }
    }
    private async Task<RoadCoverageReadFact> ReadCore(Guid project, CancellationToken token)
    {
        var obligations = await db.RepairObligations.AsNoTracking().Include(row => row.Scope).Where(row =>
            (db.Set<ObligationResponsibility>().Where(owner => owner.ObligationId == row.Id).Select(owner => (Guid?)owner.CurrentProjectId).SingleOrDefault() ?? row.ProjectId) == project)
            .OrderBy(row => row.Id).ToArrayAsync(token);
        var scopes = obligations.Select(row => new RoadCoverageScopeFact(row.Id, row.Scope.PhysicalRoadId, row.Scope.LocationVersion,
            row.Scope.From, row.Scope.To, row.Scope.OffsetFrom, row.Scope.OffsetTo, LD06LifecycleAction.HashScope(row))).ToArray();
        var sources = new List<RoadCoverageSourceFact>();
        var handovers = await db.HandoverDocuments.AsNoTracking().Where(row => row.ProjectId == project).Select(row => row.Id).OrderBy(id => id).ToArrayAsync(token);
        var warranties = await db.Warranties.AsNoTracking().Where(row => row.ProjectId == project).Select(row => row.Id).OrderBy(id => id).ToArrayAsync(token);
        var files = await db.FileScopes.AsNoTracking().Where(row => row.ProjectId == project).Select(row => row.FileId).Distinct().OrderBy(id => id).ToArrayAsync(token);
        foreach (var (kind, ids) in new[] { ("HANDOVER", handovers), ("WARRANTY", warranties), ("MAINTENANCE_BASIS", files) })
            foreach (var id in ids)
                if (await RoadCoverageResolver.Source(db, project, kind, id, token) is { } source) sources.Add(source.View);
        var history = await db.Set<RoadCoverageMapping>().AsNoTracking().Where(row => row.ProjectId == project).OrderBy(row => row.Id).ToArrayAsync(token);
        var facts = new RoadCoverageReadFact(project, scopes, sources.ToArray(), history, "");
        return facts with { Version = RoadCoverageResolver.Hash(facts) };
    }
    public async Task<RoadCoverageResult> ConfirmAsync(RoadCoverageCommand command, CancellationToken token)
    {
        var input = command.Input;
        if (string.IsNullOrWhiteSpace(command.Key) || command.Key.Length > 150 || command.Key.Any(c => c < 33 || c > 126))
            return new(400, "validation_error");
        if (input is null || command.ActorId == Guid.Empty || command.ProjectId == Guid.Empty || input.ScopeObligationId == Guid.Empty ||
            input.HandoverDocumentId == Guid.Empty || input.SourceId == Guid.Empty || input.SupersedesId == Guid.Empty ||
            input.SourceKind is not ("WARRANTY" or "MAINTENANCE_BASIS") || input.Provenance is not ("REAL_SOURCE" or "TEST_ONLY") ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000 || input.ApplicableFromUtc == default ||
            input.ApplicableFromUtc >= input.ApplicableToUtc || input.ExpectedScopeHash?.Length != 64 ||
            input.HandoverVersion?.Length != 64 || input.SourceVersion?.Length != 64 || command.ExpectedVersion?.Length != 64)
            return new(400, "validation_error");
        async Task Guard(CancellationToken ct)
        {
            await Authority(command.ActorId, command.Role, command.ProjectId, true, ct);
            var scope = await db.RepairObligations.AsNoTracking().Include(row => row.Scope).SingleOrDefaultAsync(row => row.Id == input.ScopeObligationId, ct);
            if (scope is null || await ObligationResponsibilityScope.ResolveAsync(db, scope.Id, scope.ProjectId, ct) != command.ProjectId)
                throw new Rejected(403, "scope_forbidden");
            if (LD06LifecycleAction.HashScope(scope) != input.ExpectedScopeHash) throw new Rejected(409, "scope_stale");
            var handover = await RoadCoverageResolver.Source(db, command.ProjectId, "HANDOVER", input.HandoverDocumentId, ct);
            var source = await RoadCoverageResolver.Source(db, command.ProjectId, input.SourceKind, input.SourceId, ct);
            if (handover?.View.Version != input.HandoverVersion || source?.View.Version != input.SourceVersion)
                throw new Rejected(409, "coverage_source_stale");
            if (input.Provenance == "REAL_SOURCE" && (handover.Synthetic || source.Synthetic))
                throw new Rejected(409, "synthetic_source_cannot_activate");
        }
        try
        {
            var outcome = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId, command.ProjectId,
                Operation, command.Key, RoadCoverageResolver.Hash(command), async ct =>
                {
                    await Guard(ct);
                    if (await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId &&
                        row.Operation == Operation && row.IdempotencyKey == command.Key, ct)) throw new ReceiptRace();
                    var view = await ReadCore(command.ProjectId, ct);
                    if (view.Version != command.ExpectedVersion) throw new Rejected(409, "concurrency_conflict");
                    var obligation = await db.RepairObligations.AsNoTracking().Include(row => row.Scope).SingleAsync(row => row.Id == input.ScopeObligationId, ct);
                    var scope = obligation.Scope;
                    var handover = (await RoadCoverageResolver.Source(db, command.ProjectId, "HANDOVER", input.HandoverDocumentId, ct))!;
                    var source = (await RoadCoverageResolver.Source(db, command.ProjectId, input.SourceKind, input.SourceId, ct))!;
                    var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                    var from = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(input.ApplicableFromUtc, zone).DateTime);
                    var last = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(input.ApplicableToUtc.AddTicks(-1), zone).DateTime);
                    if (from < handover.View.From) throw new Rejected(409, "handover_time_mismatch");
                    if (input.SourceKind == "WARRANTY")
                    {
                        var warranty = await db.Warranties.AsNoTracking().SingleAsync(row => row.Id == input.SourceId, ct);
                        if (warranty.RoadSectionId is Guid road && road != scope.PhysicalRoadId ||
                            warranty.HandoverDocumentId is Guid document && document != input.HandoverDocumentId ||
                            from < warranty.WarrantyStartDate || last > warranty.WarrantyEndDate)
                            throw new Rejected(409, "warranty_scope_time_mismatch");
                    }
                    RoadCoverageMapping? previous = null;
                    if (input.SupersedesId is Guid prior)
                    {
                        previous = view.History.SingleOrDefault(row => row.Id == prior);
                        if (previous is null || previous.ScopeObligationId != obligation.Id || previous.Provenance != input.Provenance ||
                            view.History.Any(row => row.SupersedesId == prior)) throw new Rejected(409, "coverage_supersession_conflict");
                    }
                    if (input.Provenance == "REAL_SOURCE" && view.History.Any(row => row.Provenance == "TEST_ONLY" &&
                        (row.SourceKind == input.SourceKind && row.SourceId == input.SourceId || row.HandoverDocumentId == input.HandoverDocumentId)))
                        throw new Rejected(409, "synthetic_source_cannot_activate");
                    var now = clock.GetUtcNow(); var id = Guid.NewGuid();
                    var facts = JsonSerializer.Serialize(new
                    {
                        handover = handover.Facts,
                        coverage = source.Facts,
                        scope = view.Scopes.Single(row => row.ObligationId == obligation.Id),
                        input.Provenance,
                        input.ApplicableFromUtc,
                        input.ApplicableToUtc
                    }, RoadCoverageResolver.Json);
                    var mapping = new RoadCoverageMapping(id, command.ProjectId, obligation.Id, scope.PhysicalRoadId, scope.LocationVersion,
                        scope.From, scope.To, scope.OffsetFrom, scope.OffsetTo, input.ApplicableFromUtc.ToUniversalTime(), input.ApplicableToUtc.ToUniversalTime(),
                        input.HandoverDocumentId, input.HandoverVersion, input.SourceKind, input.SourceId, input.SourceVersion,
                        handover.View.FileId, source.View.FileId, facts, input.Provenance, command.ActorId, now, input.Reason, previous?.Id);
                    db.Add(mapping);
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "road_coverage_confirmed", "RoadCoverageMapping", id, null,
                        JsonSerializer.Serialize(new { mappingId = id, sourceVersion = input.SourceVersion, provenance = input.Provenance }, RoadCoverageResolver.Json),
                        input.Reason, Operation, id, ["mappingId", "sourceVersion", "provenance"]));
                    db.OutboxMessages.Add(OutboxMessage.Create(id, "road.coverage.confirmed.v1", now, id,
                        JsonSerializer.Serialize(new { mappingId = id, projectId = command.ProjectId, sourceVersion = input.SourceVersion }, RoadCoverageResolver.Json)));
                    await db.SaveChangesAsync(ct); await Guard(ct);
                    return (id, JsonSerializer.Serialize(await ReadCore(command.ProjectId, ct), RoadCoverageResolver.Json));
                }, token, receiptAccessGuard: Guard);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value:
                JsonSerializer.Deserialize<RoadCoverageReadFact>(outcome.OutcomeJson, RoadCoverageResolver.Json));
        }
        catch (Rejected rejected) { db.ChangeTracker.Clear(); return rejected.Result; }
        catch (ReceiptRace) { db.ChangeTracker.Clear(); return await ConfirmAsync(command, token); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
}
