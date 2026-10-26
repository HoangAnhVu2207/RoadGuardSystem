using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed class NotificationSupervisorRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : INotificationSupervisorRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class Denied(int status, string code) : Exception { public NotificationSupervisorResult Result { get; } = new(status, code); }
    private sealed class ExistingReceipt : Exception { }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Deny(int status, string code) => throw new Denied(status, code);

    public async Task<NotificationSupervisorResult> ExecuteAsync(NotificationSupervisorRequest request, CancellationToken ct)
    {
        try
        {
            // Authorize exact resource before parsing the command envelope, including errors/replays.
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                await Guard(request, ct); await tx.CommitAsync(ct);
            });
            if (request.Method == "GET")
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                    await Guard(request, ct);
                    var head = await Head(request.ItemId, ct);
                    if (head is null) Deny(404, "not_found");
                    var value = View(head); await tx.CommitAsync(ct); return new NotificationSupervisorResult(200, Value: value, Version: value.Version);
                });
            var supervisor = Parse(request);
            var operation = "b2.assignment." + request.ItemId.ToString("N");
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            {
                method = request.Method,
                request.ProjectId,
                request.PackageId,
                request.ItemId,
                supervisorId = supervisor,
                condition = request.IfMatch is null ? "If-None-Match" : "If-Match",
                value = request.IfMatch ?? request.IfNoneMatch
            }, Json))).ToLowerInvariant();
            async Task GuardReceipt(CancellationToken token) => await Guard(request, token);
            var outcome = await receipts.ExecuteSerializableAsync(request.ActorId, request.ProjectId, operation,
                request.Key!, fingerprint, async token =>
                {
                    var item = await Guard(request, token);
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(x => x.ActorUserId == request.ActorId &&
                        x.ProjectId == request.ProjectId && x.Operation == operation && x.IdempotencyKey == request.Key, token)) throw new ExistingReceipt();
                    var head = await Head(item.Id, token);
                    if (request.Method == "DELETE" && head is null) Deny(404, "not_found");
                    if (request.IfNoneMatch is not null ? head is not null : head is null || '"' + Convert.ToBase64String(head.RowVersion) + '"' != request.IfMatch)
                        Deny(412, "precondition_failed");
                    var now = clock.GetUtcNow().ToUniversalTime();
                    Guid? previous = head?.AssignmentId;
                    var create = head is null;
                    if (request.Method == "DELETE")
                    {
                        if (head!.RevokedAt is not null) Deny(409, "notification_assignment_revoked");
                        head.RevokedAt = now;
                    }
                    else
                    {
                        if (!await RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository.CurrentAuthority(db, clock, supervisor!.Value,
                            UserRoleCode.Supervisor, request.ProjectId, token)) Deny(404, "not_found");
                        if (item.Mode != RepairMode.FastTrack || item.SupersededByItemId is not null ||
                            item.State is RepairItemState.Confirmed or RepairItemState.CorrectionRequired or RepairItemState.Cancelled ||
                            !await db.RepairObligations.AnyAsync(x => x.Id == item.ObligationId && x.CurrentRepairItemId == item.Id, token))
                            Deny(409, "invalid_state_transition");
                        if (head is null)
                        {
                            head = new() { ItemId = item.Id, ProjectId = request.ProjectId, PackageId = request.PackageId };
                            db.Add(head);
                        }
                        head.AssignmentId = Guid.NewGuid(); head.SupervisorId = supervisor.Value;
                        head.AssignedAt = now; head.RevokedAt = null;
                    }
                    if (item.Mode != RepairMode.FastTrack) Deny(409, "invalid_state_transition");
                    await db.SaveChangesAsync(token);
                    if (request.Method == "PUT") db.Add(new NotificationSupervisorGeneration
                    {
                        Id = head!.AssignmentId,
                        ItemId = item.Id,
                        ProjectId = request.ProjectId,
                        SupervisorId = head.SupervisorId,
                        ActorId = request.ActorId,
                        AssignedAt = now,
                        Version = Convert.ToBase64String(head.RowVersion)
                    });
                    db.Add(new NotificationSupervisorAudit
                    {
                        Id = Guid.NewGuid(),
                        ItemId = item.Id,
                        AssignmentId = head!.AssignmentId,
                        PreviousAssignmentId = previous,
                        ActorId = request.ActorId,
                        At = now,
                        Action = request.Method == "DELETE" ? "REVOKED" : create ? "CREATED" : "REPLACED"
                    });
                    await db.SaveChangesAsync(token);
                    var result = new NotificationSupervisorResult(request.Method == "DELETE" ? 204 : create ? 201 : 200,
                        Value: request.Method == "DELETE" ? null : View(head), Version: Convert.ToBase64String(head.RowVersion));
                    return (head.AssignmentId, JsonSerializer.Serialize(result, Json));
                }, ct, receiptAccessGuard: GuardReceipt);
            return outcome.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_key_reused") :
                JsonSerializer.Deserialize<NotificationSupervisorResult>(outcome.OutcomeJson, Json)!;
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await ExecuteAsync(request, ct); }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(412, "precondition_failed"); }
    }

    private async Task<RepairItem> Guard(NotificationSupervisorRequest r, CancellationToken ct)
    {
        if (r.Role != UserRoleCode.ProjectManager) Deny(403, "access_forbidden");
        if (!await RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository.CurrentAuthority(db, clock, r.ActorId, r.Role, r.ProjectId, ct)) Deny(403, "access_forbidden");
        var item = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={r.ItemId}").SingleOrDefaultAsync(ct);
        if (item is null || item.ProjectId != r.ProjectId || db.Entry(item).Property<Guid?>("PackageId").CurrentValue != r.PackageId ||
            !await db.RepairPackages.AnyAsync(x => x.Id == r.PackageId && x.ProjectId == r.ProjectId, ct)) Deny(404, "not_found");
        if (await ObligationResponsibilityScope.ResolveAsync(db, item.ObligationId, item.ProjectId, ct) != r.ProjectId)
            Deny(404, "not_found");
        return item;
    }
    private Task<NotificationSupervisorHead?> Head(Guid item, CancellationToken ct) => db.Set<NotificationSupervisorHead>()
        .FromSqlInterpolated($"SELECT * FROM [NotificationSupervisorHeads] WITH (UPDLOCK,HOLDLOCK) WHERE [ItemId]={item}").SingleOrDefaultAsync(ct);
    private static NotificationSupervisorReadRecord View(NotificationSupervisorHead h) =>
        new(h.AssignmentId, h.SupervisorId, h.RevokedAt is null ? "ACTIVE" : "REVOKED", h.AssignedAt, h.RevokedAt, Convert.ToBase64String(h.RowVersion));

    private static Guid? Parse(NotificationSupervisorRequest r)
    {
        if (r.Method is not ("PUT" or "DELETE") || string.IsNullOrWhiteSpace(r.Key) || r.Key.Length > 200 ||
            r.Key != r.Key.Trim() || r.Key.Any(char.IsControl) || r.IfMatch is not null && r.IfNoneMatch is not null) Deny(400, "validation_error");
        if (r.IfMatch is null && r.IfNoneMatch is null) Deny(428, "precondition_required");
        if (r.IfNoneMatch is not null && (r.IfNoneMatch != "*" || r.Method != "PUT") ||
            r.IfMatch is not null && (r.IfMatch.Length < 2 || r.IfMatch[0] != '"' || r.IfMatch[^1] != '"' || !NotificationAssignmentSnapshot.IsVersion(r.IfMatch[1..^1]))) Deny(400, "validation_error");
        if (r.Method == "DELETE") { if (!string.IsNullOrWhiteSpace(r.Body)) Deny(400, "validation_error"); return null; }
        try
        {
            using var doc = JsonDocument.Parse(r.Body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) Deny(400, "validation_error");
            var fields = doc.RootElement.EnumerateObject().ToArray();
            if (fields.Length != 1 || fields[0].Name != "supervisorId" || fields[0].Value.ValueKind != JsonValueKind.String ||
                !fields[0].Value.TryGetGuid(out var id) || id == Guid.Empty) Deny(400, "validation_error");
            return fields[0].Value.GetGuid();
        }
        catch (JsonException) { Deny(400, "validation_error"); throw; }
    }
}
