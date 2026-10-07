using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Projects;

public sealed partial class ProjectLifecycleRepository
{
    public async Task<ProjectLifecycleWriteResult> ExecuteAsync(LD06LifecycleCommand command, CancellationToken cancellationToken)
    {
        var token = cancellationToken;
        if (command.Input is null || command.Input.EvidenceFileIds is null || command.ActorId == Guid.Empty || command.ProjectId == Guid.Empty ||
            !Enum.IsDefined(command.Kind) || string.IsNullOrWhiteSpace(command.Input.Reason) || command.Input.Reason.Length > 2000 ||
            command.Input.EvidenceFileIds.Length > 100 || command.Input.EvidenceFileIds.Any(x => x == Guid.Empty) ||
            command.Input.EvidenceFileIds.Distinct().Count() != command.Input.EvidenceFileIds.Length || string.IsNullOrWhiteSpace(command.Key) ||
            command.ExpectedVersion?.Length != 64) return new(400, "validation_error");
        var input = command.Input;
        var operation = $"ld06.lifecycle.{command.Kind}.v1";
        async Task Guard(CancellationToken ct)
        {
            var required = command.Kind is LD06ActionKind.DeclareConstruction or LD06ActionKind.LinkRecurrence
                ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor;
            if (command.Role != required) throw new Rejected(403, "access_forbidden");
            Guid? otherProject = command.Kind == LD06ActionKind.IssueTransfer ? input.ReceivingProjectId : null;
            if (command.Kind == LD06ActionKind.AcceptTransfer && input.SourceActionId is Guid issueId)
                otherProject = await db.Set<LD06LifecycleAction>().AsNoTracking().Where(x => x.Id == issueId &&
                    x.Kind == LD06ActionKind.IssueTransfer && x.ReceivingProjectId == command.ProjectId).Select(x => (Guid?)x.ProjectId).SingleOrDefaultAsync(ct);
            foreach (var project in new[] { command.ProjectId, otherProject ?? command.ProjectId }.Distinct().Order())
                await Anh02ReceiptAuthority.LockAsync(db, command.ActorId, project, ct);
            if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(command.ActorId, required, ct))
                throw new Rejected(403, "access_forbidden");
            _ = await ReadCore(command.ActorId, command.ProjectId, ct) ?? throw new Rejected(404, "not_found");
            if (command.Kind == LD06ActionKind.DeclareConstruction)
                _ = await Evidence(command.ProjectId, input.EvidenceFileIds, clock.GetUtcNow(), ct);
            if (command.Kind == LD06ActionKind.ConfirmConstruction && input.SourceActionId is Guid declarationId)
            {
                if (!await db.Set<LD06LifecycleAction>().AnyAsync(x => x.Id == declarationId && x.ProjectId == command.ProjectId && x.Kind == LD06ActionKind.DeclareConstruction, ct))
                    throw new Rejected(409, "construction_declaration_required");
                var pins = await db.Set<LD06ActionEvidence>().Where(x => x.ActionId == declarationId).ToArrayAsync(ct);
                var currentFiles = await Evidence(command.ProjectId, pins.Select(x => x.FileId).ToArray(), clock.GetUtcNow(), ct);
                if (pins.Length == 0 || pins.Any(x => !currentFiles.Any(f => f.Id == x.FileId && f.Checksum == x.Checksum)))
                    throw new Rejected(409, "construction_evidence_stale");
            }
            if (input.ObligationId is Guid obligationId)
            {
                var origin = await db.Set<RepairObligation>().AsNoTracking().Where(x => x.Id == obligationId).Select(x => (Guid?)x.ProjectId).SingleOrDefaultAsync(ct);
                var effective = await db.Set<ObligationResponsibility>().AsNoTracking().Where(x => x.ObligationId == obligationId)
                    .Select(x => (Guid?)x.CurrentProjectId).SingleOrDefaultAsync(ct) ?? origin;
                if (command.Kind == LD06ActionKind.IssueTransfer && effective != command.ProjectId)
                    throw new Rejected(403, "obligation_responsibility_forbidden");
                if (command.Kind == LD06ActionKind.AcceptTransfer && effective != command.ProjectId && effective != otherProject)
                    throw new Rejected(403, "obligation_responsibility_forbidden");
            }
        }
        try
        {
            var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(command, Json))).ToLowerInvariant();
            var outcome = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId, command.ProjectId,
                operation, command.Key, hash, async ct =>
                {
                    await Guard(ct);
                    if (await db.IdempotencyRecords.AnyAsync(x => x.ActorUserId == command.ActorId && x.ProjectId == command.ProjectId &&
                        x.Operation == operation && x.IdempotencyKey == command.Key, ct)) throw new ReceiptRace();
                    // Project anchors serialize inventory-changing commands with lifecycle decisions.
                    await db.Projects.FromSqlInterpolated($"SELECT * FROM Projects WITH (UPDLOCK,HOLDLOCK) WHERE Id={command.ProjectId}").LoadAsync(ct);
                    var view = await ReadCore(command.ActorId, command.ProjectId, ct) ?? throw new Rejected(404, "not_found");
                    if (view.Version != command.ExpectedVersion) throw new Rejected(409, "concurrency_conflict");
                    var now = clock.GetUtcNow();
                    LD06LifecycleAction? source = null;
                    if (input.SourceActionId is Guid sourceId)
                        source = await db.Set<LD06LifecycleAction>().SingleOrDefaultAsync(x => x.Id == sourceId, ct)
                            ?? throw new Rejected(409, "source_unavailable");
                    var evidence = await Evidence(command.ProjectId, input.EvidenceFileIds, now, ct);
                    var obligations = await db.Set<RepairObligation>().Include(x => x.Scope).Where(x => x.ProjectId == command.ProjectId).ToArrayAsync(ct);
                    var defects = await db.Defects.Where(x => x.ProjectId == command.ProjectId).ToArrayAsync(ct);
                    var incoming = await db.Set<ObligationResponsibility>().Where(x => x.CurrentProjectId == command.ProjectId).ToArrayAsync(ct);
                    var incomingIds = incoming.Select(x => x.ObligationId).ToArray();
                    var received = await db.Set<RepairObligation>().Include(x => x.Scope).Where(x => incomingIds.Contains(x.Id)).ToArrayAsync(ct);
                    var all = obligations.Concat(received).DistinctBy(x => x.Id).ToArray();
                    var responsibility = await db.Set<ObligationResponsibility>().Where(x => obligations.Select(o => o.Id).Contains(x.ObligationId)).ToArrayAsync(ct);
                    bool Transferred(RepairObligation o) => responsibility.Any(x => x.ObligationId == o.Id && x.CurrentProjectId != command.ProjectId);
                    bool CompleteInventory() => defects.All(d => obligations.Any(o => o.DefectId == d.Id));
                    string scopeHash = "";
                    RepairObligation? transferObligation = null;
                    switch (command.Kind)
                    {
                        case LD06ActionKind.DeclareConstruction:
                            if (evidence.Length == 0) throw new Rejected(409, "construction_evidence_required");
                            break;
                        case LD06ActionKind.ConfirmConstruction:
                            if (source is null || source.Kind != LD06ActionKind.DeclareConstruction || source.ProjectId != command.ProjectId || source.At > now ||
                                !await db.Set<LD06ActionEvidence>().AnyAsync(x => x.ActionId == source.Id, ct))
                                throw new Rejected(409, "construction_declaration_required");
                            var sourceFiles = await db.Set<LD06ActionEvidence>().Where(x => x.ActionId == source.Id).Select(x => x.FileId).ToArrayAsync(ct);
                            var confirmedFiles = await Evidence(command.ProjectId, sourceFiles, now, ct);
                            var pinned = await db.Set<LD06ActionEvidence>().Where(x => x.ActionId == source.Id).ToArrayAsync(ct);
                            if (pinned.Any(x => !confirmedFiles.Any(f => f.Id == x.FileId && f.Checksum == x.Checksum)))
                                throw new Rejected(409, "construction_evidence_stale");
                            break;
                        case LD06ActionKind.CloseDefect:
                            var defect = defects.SingleOrDefault(x => x.Id == input.DefectId) ?? throw new Rejected(404, "not_found");
                            var owned = obligations.Where(x => x.DefectId == defect.Id).ToArray();
                            if (owned.Length == 0 || owned.Any(x => x.Mandatory && !x.IsResolved)) throw new Rejected(409, "mandatory_obligations_unresolved");
                            if (defect.Status == DefectStatus.Closed) throw new Rejected(409, "defect_already_closed");
                            db.Entry(defect).Property(x => x.Status).CurrentValue = DefectStatus.Closed;
                            break;
                        case LD06ActionKind.OperationalClose:
                            if (!CompleteInventory()) throw new Rejected(409, "obligation_inventory_incomplete");
                            if (all.Any(x => x.Mandatory && !x.IsResolved && !Transferred(x))) throw new Rejected(409, "mandatory_obligations_unresolved");
                            break;
                        case LD06ActionKind.LinkRecurrence:
                            throw new Rejected(409, "atomic_confirmed_new_defect_workflow_required");
                        case LD06ActionKind.IssueTransfer:
                            transferObligation = all.SingleOrDefault(x => x.Id == input.ObligationId) ?? throw new Rejected(404, "not_found");
                            await LockObligation(transferObligation.Id, ct);
                            var current = await db.Set<ObligationResponsibility>().SingleOrDefaultAsync(x => x.ObligationId == transferObligation.Id, ct);
                            if ((current?.CurrentProjectId ?? transferObligation.ProjectId) != command.ProjectId ||
                                input.ReceivingProjectId is null || input.ReceivingProjectId == command.ProjectId ||
                                !await db.Projects.AnyAsync(x => x.Id == input.ReceivingProjectId, ct)) throw new Rejected(409, "transfer_scope_conflict");
                            scopeHash = LD06LifecycleAction.HashScope(transferObligation);
                            if (input.ScopeHash != scopeHash) throw new Rejected(409, "transfer_scope_conflict");
                            break;
                        case LD06ActionKind.AcceptTransfer:
                            {
                                if (source is null || source.Kind != LD06ActionKind.IssueTransfer || source.ReceivingProjectId != command.ProjectId ||
                                    source.ObligationId != input.ObligationId || source.ScopeHash != input.ScopeHash || source.At > now)
                                    throw new Rejected(409, "transfer_scope_conflict");
                                await LockObligation(source.ObligationId!.Value, ct);
                                transferObligation = await db.Set<RepairObligation>().Include(x => x.Scope).SingleAsync(x => x.Id == source.ObligationId, ct);
                                await db.Entry(transferObligation).ReloadAsync(ct);
                                var owner = await db.Set<ObligationResponsibility>().SingleOrDefaultAsync(x => x.ObligationId == transferObligation.Id, ct);
                                using var offerFacts = JsonDocument.Parse(source.FactsJson);
                                var offeredVersion = offerFacts.RootElement.GetProperty("obligationVersion").GetString();
                                var offeredOwner = offerFacts.RootElement.GetProperty("responsibilityHead").ValueKind == JsonValueKind.Null ? (Guid?)null :
                                    offerFacts.RootElement.GetProperty("responsibilityHead").GetGuid();
                                if ((owner?.CurrentProjectId ?? transferObligation.ProjectId) != source.ProjectId ||
                                    LD06LifecycleAction.HashScope(transferObligation) != source.ScopeHash ||
                                    offeredVersion != Convert.ToBase64String(db.Entry(transferObligation).Property<byte[]>("RowVersion").CurrentValue!) ||
                                    offeredOwner != owner?.AcceptanceActionId ||
                                    await db.Set<LD06LifecycleAction>().AnyAsync(x => x.SourceActionId == source.Id && x.Kind == LD06ActionKind.AcceptTransfer, ct))
                                    throw new Rejected(409, "transfer_source_stale");
                                scopeHash = source.ScopeHash;
                                break;
                            }
                        default: throw new Rejected(400, "validation_error");
                    }
                    var id = Guid.NewGuid();
                    var responsibilityHead = transferObligation is null ? null : await db.Set<ObligationResponsibility>().Where(x => x.ObligationId == transferObligation.Id)
                        .Select(x => (Guid?)x.AcceptanceActionId).SingleOrDefaultAsync(ct);
                    var facts = transferObligation is not null ? JsonSerializer.Serialize(new
                    {
                        obligationId = transferObligation.Id,
                        obligationKind = transferObligation.Kind.ToString(),
                        mandatory = transferObligation.Mandatory,
                        sourceProjectId = command.Kind == LD06ActionKind.AcceptTransfer ? source!.ProjectId : command.ProjectId,
                        receivingProjectId = command.Kind == LD06ActionKind.AcceptTransfer ? command.ProjectId : input.ReceivingProjectId,
                        scopeHash,
                        obligationVersion = Convert.ToBase64String(db.Entry(transferObligation).Property<byte[]>("RowVersion").CurrentValue!),
                        responsibilityHead,
                        sourceActionId = source?.Id,
                        scope = new
                        {
                            scopeId = transferObligation.Scope.Id,
                            transferObligation.Scope.PhysicalRoadId,
                            transferObligation.Scope.LocationVersion,
                            transferObligation.Scope.RouteLabel,
                            transferObligation.Scope.From,
                            transferObligation.Scope.To,
                            transferObligation.Scope.OffsetFrom,
                            transferObligation.Scope.OffsetTo
                        }
                    }, Json) : JsonSerializer.Serialize(new
                    {
                        completeInventory = CompleteInventory(),
                        defectIds = defects.Select(x => x.Id).Order(),
                        obligationIds = all.Select(x => x.Id).Order(),
                        evidenceFileIds = input.EvidenceFileIds,
                        scopeHash,
                        obligationVersion = transferObligation is null ? null : Convert.ToBase64String(db.Entry(transferObligation).Property<byte[]>("RowVersion").CurrentValue!),
                        responsibilityHead,
                        priorRepairDecisionId = input.PriorRepairDecisionId,
                        sourceActionId = source?.Id
                    }, Json);
                    var action = LD06LifecycleAction.Create(id, command.ProjectId, command.ActorId, command.Kind, now, input.Reason, facts,
                        source?.Id, input.DefectId, input.LinkedDefectId, input.ObligationId,
                        command.Kind == LD06ActionKind.AcceptTransfer ? command.ProjectId : input.ReceivingProjectId, input.PriorRepairDecisionId, scopeHash);
                    db.Add(action); foreach (var file in evidence) db.Add(LD06ActionEvidence.Pin(id, file.Id, file.Checksum));
                    await db.SaveChangesAsync(ct);
                    if (command.Kind == LD06ActionKind.AcceptTransfer)
                    {
                        var owner = await db.Set<ObligationResponsibility>().SingleOrDefaultAsync(x => x.ObligationId == transferObligation!.Id, ct);
                        if (owner is null) db.Add(ObligationResponsibility.Accept(transferObligation!.Id, transferObligation.ProjectId, command.ProjectId, id));
                        else owner.Transfer(command.ProjectId, id);
                    }
                    if (command.Kind == LD06ActionKind.OperationalClose)
                        db.Add(ProjectLifecycleHistoryRecord.RecordProductionClosure(action));
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, $"ld06_{command.Kind}", "Project", command.ProjectId,
                        null, facts, input.Reason, "ld06.lifecycle", id,
                        ["completeInventory", "defectIds", "obligationIds", "evidenceFileIds", "scopeHash", "obligationVersion", "responsibilityHead", "priorRepairDecisionId", "sourceActionId",
                            "obligationId", "obligationKind", "mandatory", "sourceProjectId", "receivingProjectId", "scope", "scopeId", "physicalRoadId", "locationVersion", "routeLabel", "from", "to", "offsetFrom", "offsetTo"]));
                    db.OutboxMessages.Add(OutboxMessage.Create(id, "project.lifecycle.changed.v1", now, null,
                        JsonSerializer.Serialize(new { schemaVersion = 1, actionId = id, projectId = command.ProjectId, kind = command.Kind.ToString(), at = now }, Json)));
                    await db.SaveChangesAsync(ct); await Guard(ct);
                    return (id, JsonSerializer.Serialize(await ReadCore(command.ActorId, command.ProjectId, ct), Json));
                }, token, receiptAccessGuard: Guard);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201,
                Value: JsonSerializer.Deserialize<ProjectLifecycleFacts>(outcome.OutcomeJson, Json));
        }
        catch (ReceiptRace) { db.ChangeTracker.Clear(); return await ExecuteAsync(command, token); }
        catch (Rejected e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (UnauthorizedAccessException) { db.ChangeTracker.Clear(); return new(403, "access_forbidden"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }

    private async Task<RoadGuardSystem.BusinessObjects.Files.StoredFile[]> Evidence(Guid project, Guid[] ids, DateTimeOffset at, CancellationToken ct)
    {
        var files = await db.Files.Where(f => ids.Contains(f.Id) && f.UploadedAt <= at &&
            db.UploadSessions.Any(u => u.FileId == f.Id && u.Status == UploadSessionStatus.Verified) &&
            db.FileScopes.Any(s => s.FileId == f.Id && s.ProjectId == project)).ToArrayAsync(ct);
        if (files.Length != ids.Length) throw new Rejected(409, "verified_project_evidence_required");
        return files;
    }

    private async Task LockObligation(Guid obligation, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM RepairObligations WITH (UPDLOCK,HOLDLOCK) WHERE Id={obligation}; SELECT ObligationId FROM ObligationResponsibilities WITH (UPDLOCK,HOLDLOCK) WHERE ObligationId={obligation}", ct);
    }
}
