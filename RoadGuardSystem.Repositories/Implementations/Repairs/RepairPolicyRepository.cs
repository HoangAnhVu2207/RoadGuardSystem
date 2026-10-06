using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed class RepairPolicyRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : IRepairPolicyRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class Denied(int status, string code) : Exception { public RepairPolicyResult Result { get; } = new(status, code); }
    private sealed class AlreadyCommitted : Exception { }
    public async Task<RepairPolicyResult> ExecuteAsync(RepairPolicyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.Role != UserRoleCode.ProjectManager) throw new Denied(403, "access_forbidden");
            async Task Guard(CancellationToken token)
            {
                await Anh02ReceiptAuthority.LockAsync(db, command.ActorId, command.ProjectId, token);
                var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(command.ActorId, command.Role, token) ||
                    !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == command.ProjectId &&
                        row.UserId == command.ActorId && row.RoleCode == command.Role && row.Status == ProjectMemberStatus.Active &&
                        row.ValidFrom <= day && (row.ValidTo == null || row.ValidTo >= day), token))
                    throw new Denied(403, "access_forbidden");
                if (command.ResourceId is Guid id)
                {
                    var revision = command.Action is "revoke" or "revision-get";
                    var exists = revision
                        ? await db.Set<RepairPolicyRevision>().AsNoTracking().AnyAsync(row => row.Id == id && row.ProjectId == command.ProjectId, token)
                        : await db.Set<RepairPolicyDraft>().AsNoTracking().AnyAsync(row => row.Id == id && row.ProjectId == command.ProjectId, token);
                    if (!exists) throw new Denied(404, "not_found");
                }
            }
            if (command.Action.EndsWith("-get", StringComparison.Ordinal))
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                    await Guard(cancellationToken);
                    var view = command.Action == "draft-get" ? DraftView(await Draft(command.ResourceId!.Value, cancellationToken))
                        : RevisionView(await Revision(command.ResourceId!.Value, cancellationToken));
                    await tx.CommitAsync(cancellationToken); return new RepairPolicyResult(200, Value: view);
                });
            var operation = "h4.repair.policy." + command.Action + ".v1";
            var fingerprint = Hash(JsonSerializer.Serialize(command, Json));
            async Task ReceiptGuard(CancellationToken token)
            {
                await Guard(token);
                var stored = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row =>
                    row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId && row.Operation == operation &&
                    row.IdempotencyKey == command.Key, token);
                if (stored is not null && !await db.Set<RepairPolicyDraft>().AnyAsync(row => row.Id == stored.OperationId && row.ProjectId == command.ProjectId, token) &&
                    !await db.Set<RepairPolicyRevision>().AnyAsync(row => row.Id == stored.OperationId && row.ProjectId == command.ProjectId, token))
                    throw new Denied(403, "stored_receipt_access_forbidden");
            }
            var receipt = await receipts.ExecuteSerializableAsync(command.ActorId, command.ProjectId, operation,
                command.Key!, fingerprint, async token =>
                {
                    await ReceiptGuard(token);
                    if (await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId &&
                        row.Operation == operation && row.IdempotencyKey == command.Key, token)) throw new AlreadyCommitted();
                    if (!await db.Projects.AnyAsync(row => row.Id == command.ProjectId && row.Status == ProjectStatus.Active, token))
                        throw new Denied(409, "project_not_active");
                    var now = clock.GetUtcNow(); RepairPolicyRecord view;
                    if (command.Action == "create")
                    {
                        var input = command.Definition ?? throw new ArgumentException("Definition required.");
                        if (!await db.DefectTypes.AnyAsync(row => row.Code == input.DefectTypeCode, token)) throw new Denied(409, "defect_type_not_found");
                        var draft = RepairPolicyDraft.Create(Guid.NewGuid(), command.ProjectId, Guid.NewGuid(), command.ActorId,
                            command.Role, now, input.Reason, input.DefectTypeCode, input.ChecklistVersion, input.Measurements, input.StopConditions);
                        var head = draft.CurrentChangeId;
                        db.Add(draft); db.Entry(draft).Property(row => row.CurrentChangeId).CurrentValue = null;
                        await db.SaveChangesAsync(token);
                        db.Entry(draft).Property(row => row.CurrentChangeId).CurrentValue = head;
                        await db.SaveChangesAsync(token); view = DraftView(draft);
                    }
                    else if (command.Action is "update" or "publish")
                    {
                        var draft = await Draft(command.ResourceId!.Value, token);
                        if (Version(draft) != command.ExpectedVersion) throw new Denied(409, "concurrency_conflict");
                        if (command.Action == "update")
                        {
                            var input = command.Definition ?? throw new ArgumentException("Definition required.");
                            if (!await db.DefectTypes.AnyAsync(row => row.Code == input.DefectTypeCode, token)) throw new Denied(409, "defect_type_not_found");
                            Transition(() => draft.Update(Guid.NewGuid(), draft.CurrentChangeId!.Value, command.ActorId, command.Role, now,
                                input.Reason, input.DefectTypeCode, input.ChecklistVersion, input.Measurements, input.StopConditions));
                            await AppendBeforeHeads(() =>
                            {
                                var entry = db.Add(draft.Changes[^1]);
                                entry.Property<Guid?>("DraftId").CurrentValue = draft.Id;
                            }, token);
                            await db.SaveChangesAsync(token); view = DraftView(draft);
                        }
                        else
                        {
                            // Project authority lock serializes revision allocation across this project's PMs.
                            var next = (await db.Set<RepairPolicyRevision>().Where(row => row.ProjectId == command.ProjectId)
                                .Select(row => (int?)row.Revision).MaxAsync(token) ?? 0) + 1;
                            RepairPolicyRevision? revision = null;
                            Transition(() => revision = draft.Publish(Guid.NewGuid(), next, command.ActorId, command.Role, now));
                            await AppendBeforeHeads(() => db.Add(revision!), token);
                            await db.SaveChangesAsync(token); view = RevisionView(revision!);
                        }
                    }
                    else if (command.Action == "revoke")
                    {
                        var revision = await Revision(command.ResourceId!.Value, token);
                        if (RevisionView(revision).Version != command.ExpectedVersion) throw new Denied(409, "concurrency_conflict");
                        Transition(() => revision.Revoke(Guid.NewGuid(), command.ActorId, command.Role, command.Reason!, now));
                        await db.SaveChangesAsync(token); view = RevisionView(revision);
                    }
                    else throw new ArgumentException("Unknown policy command.");
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "repair_policy_" + command.Action,
                        "RepairPolicy", view.Id, null, JsonSerializer.Serialize(view, Json), command.Reason ?? command.Definition!.Reason,
                        "h4.policy.v1", view.Id, ["policy"]));
                    await db.SaveChangesAsync(token); await ReceiptGuard(token);
                    return (view.Id, JsonSerializer.Serialize(view, Json));
                }, cancellationToken, receiptAccessGuard: ReceiptGuard);
            if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(receipt.Status == IdempotencyOperationStatus.Replayed ? 200 : 201,
                Value: JsonSerializer.Deserialize<RepairPolicyRecord>(receipt.OutcomeJson, Json),
                Replayed: receipt.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (AlreadyCommitted) { db.ChangeTracker.Clear(); return await ExecuteAsync(command, cancellationToken); }
        catch (Denied denied) { db.ChangeTracker.Clear(); return denied.Result; }
        catch (ArgumentException) { db.ChangeTracker.Clear(); return new(400, "validation_error"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
    private static void Transition(Action action)
    {
        try { action(); } catch (InvalidOperationException) { throw new Denied(409, "invalid_policy_transition"); }
    }
    private Task<RepairPolicyDraft> Draft(Guid id, CancellationToken token) => db.Set<RepairPolicyDraft>()
        .FromSqlInterpolated($"SELECT * FROM [RepairPolicyDrafts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
        .Include(row => row.Changes).Include(row => row.PublishedRevision).SingleAsync(token);
    private Task<RepairPolicyRevision> Revision(Guid id, CancellationToken token) => db.Set<RepairPolicyRevision>()
        .FromSqlInterpolated($"SELECT * FROM [RepairPolicyRevisions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
        .Include(row => row.Measurements).Include(row => row.Revocations).SingleAsync(token);
    private async Task AppendBeforeHeads(Action append, CancellationToken token)
    {
        var detect = db.ChangeTracker.AutoDetectChangesEnabled;
        try { db.ChangeTracker.AutoDetectChangesEnabled = false; append(); await db.SaveChangesAsync(token); }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = detect; }
        db.ChangeTracker.DetectChanges();
    }
    private string Version(RepairPolicyDraft draft) => Convert.ToBase64String(db.Entry(draft).Property<byte[]>("RowVersion").CurrentValue!);
    private RepairPolicyRecord DraftView(RepairPolicyDraft draft)
    {
        var head = draft.Changes.Single(row => row.Id == draft.CurrentChangeId);
        return new(draft.Id, draft.ProjectId, head.Id, draft.PublishedRevisionId, null, head.DefectTypeCode,
            head.ChecklistVersion, head.Measurements.ToArray(), head.StopConditions.ToArray(),
            draft.PublishedRevisionId is null ? "DRAFT" : "PUBLISHED", head.ActorId, head.At, head.Reason, Version(draft));
    }
    private static RepairPolicyRecord RevisionView(RepairPolicyRevision revision) => new(revision.Id, revision.ProjectId,
        null, revision.Id, revision.Revision, revision.DefectTypeCode, revision.ChecklistVersion,
        revision.Measurements.ToArray(), revision.StopConditions.ToArray(), revision.IsRevoked ? "REVOKED" : "PUBLISHED",
        revision.PublishedBy, revision.PublishedAt, revision.Revocations.Count == 0 ? "" : revision.Revocations[0].Reason,
        "h4-policy-v1-" + Hash(JsonSerializer.Serialize(new { revision.Id, revocations = revision.Revocations.OrderBy(row => row.Id).ToArray() }, Json)));
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
