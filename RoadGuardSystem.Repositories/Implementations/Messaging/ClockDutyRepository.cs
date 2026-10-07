using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class ClockDutyRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider time) : IClockDutyRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class ExistingReceipt : Exception { }
    private sealed class Denied(int status, string code) : Exception { public BusinessDutyResult Result { get; } = new(status, code); }
    private static void Deny(int status, string code) => throw new Denied(status, code);
    private async Task<DeadlineClock> Load(ClockDutyCommand c, CancellationToken token)
    {
        var row = await db.Set<DeadlineClock>().FromSqlInterpolated($"SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={c.ClockId}")
            .Include(x => x.Extensions).Include(x => x.Breaches).Include(x => x.Appointments).AsSplitQuery().SingleOrDefaultAsync(token);
        if (row is null || await new H6DeadlineNotificationSourceAdapter(db, time).ResponsibilityProjectAsync(row, token) != c.ProjectId) throw new Denied(404, "not_found");
        return row;
    }
    private async Task Guard(ClockDutyCommand c, CancellationToken token)
    {
        if (!await BusinessDutyRepository.CurrentAuthority(db, time, c.ActorId, c.Role, c.ProjectId, token)) Deny(403, "access_forbidden");
        var row = await Load(c, token);
        var required = row.Kind switch
        {
            DeadlineClockKind.ProjectManagerReview or DeadlineClockKind.SupervisorInitialApproval or
                DeadlineClockKind.SupervisorFinalConfirmation or DeadlineClockKind.SupervisorEscalation => UserRoleCode.Supervisor,
            DeadlineClockKind.CrewSupplement or DeadlineClockKind.FinishedDataSync => UserRoleCode.ProjectManager,
            _ => UserRoleCode.Unknown
        };
        if (required == UserRoleCode.Unknown) Deny(409, "clock_extension_prohibited");
        if (c.Role != required) Deny(403, "clock_decision_authority_required");
        if (row.Kind == DeadlineClockKind.ProjectManagerReview && await db.FieldInspectionTasks.AnyAsync(t => t.Id == row.TargetId &&
            (t.Status == FieldInspectionTaskStatus.Cancelled || t.Status == FieldInspectionTaskStatus.Completed), token))
            Deny(409, "clock_source_stale");
        if (row.CompletedAt is not null || (await new H6DeadlineNotificationSourceAdapter(db, time).ResolveDutyAsync(row, token)).Status != "VERIFIED")
            Deny(409, "clock_source_stale");
        if (c.Action == "appoint")
        {
            if (row.Kind is not (DeadlineClockKind.ProjectManagerReview or DeadlineClockKind.SupervisorInitialApproval or DeadlineClockKind.SupervisorFinalConfirmation))
                Deny(409, "use_receiving_request_or_task_assignment");
            var dutyRole = row.Kind == DeadlineClockKind.ProjectManagerReview ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor;
            if (c.AssigneeId is not Guid next || !await BusinessDutyRepository.CurrentAuthority(db, time, next, dutyRole, c.ProjectId, token))
                Deny(403, "assignee_not_authorized");
            var receipt = await db.IdempotencyRecords.AsNoTracking().AnyAsync(x => x.ActorUserId == c.ActorId && x.ProjectId == c.ProjectId &&
                x.Operation == "activation.clock.appoint.v1" && x.IdempotencyKey == c.Key, token);
            if (receipt && row.AppointedActorId != c.AssigneeId) Deny(409, "duty_assignment_superseded");
        }
    }
    public async Task<BusinessDutyResult> ExecuteAsync(ClockDutyCommand command, CancellationToken token)
    {
        var c = command;
        try
        {
            if (string.IsNullOrWhiteSpace(c.Key) || c.Key.Length > 200 || string.IsNullOrWhiteSpace(c.Reason) || c.Reason.Length > 2000 ||
                c.Action is not ("extend" or "appoint") || string.IsNullOrWhiteSpace(c.ExpectedVersion) ||
                c.Action == "extend" && c.NewDueAt is null) return new(400, "validation_error");
            var operation = "activation.clock." + c.Action + ".v1";
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(c, Json))).ToLowerInvariant();
            var result = await receipts.ExecuteSerializableAsync(c.ActorId, c.ProjectId, operation, c.Key, fingerprint, async ct =>
            {
                await Guard(c, ct); var row = await Load(c, ct);
                if (await db.IdempotencyRecords.AnyAsync(x => x.ActorUserId == c.ActorId && x.ProjectId == c.ProjectId &&
                    x.Operation == operation && x.IdempotencyKey == c.Key, ct)) throw new ExistingReceipt();
                if (Convert.ToBase64String(row.RowVersion) != c.ExpectedVersion) Deny(409, "concurrency_conflict");
                var now = time.GetUtcNow(); var eventId = Guid.NewGuid();
                if (c.Action == "extend") row.Extend(eventId, c.ActorId, c.NewDueAt!.Value, c.Reason, now);
                else
                {
                    var previous = await new H6DeadlineNotificationSourceAdapter(db, time).ResolveDutyAsync(row, ct);
                    Guid? previousActor = previous.ResponsibleUserId;
                    if (previous.ResponsibleIsSupervisor)
                    {
                        var day = DateOnly.FromDateTime(now.UtcDateTime);
                        var candidates = await db.ProjectMembers.Where(m => m.ProjectId == c.ProjectId && m.RoleCode == UserRoleCode.Supervisor &&
                            m.Status == ProjectMemberStatus.Active && m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day) &&
                            db.Users.Any(u => u.Id == m.UserId && u.RoleCode == UserRoleCode.Supervisor && u.Status == UserStatus.Active && !u.MustChangePassword))
                            .Select(m => m.UserId).Distinct().Take(2).ToArrayAsync(ct);
                        previousActor = candidates.Length == 1 ? candidates[0] : null;
                    }
                    row.Appoint(eventId, c.AssigneeId!.Value, row.Kind == DeadlineClockKind.ProjectManagerReview ?
                        UserRoleCode.ProjectManager : UserRoleCode.Supervisor, c.ActorId, c.Reason, now, previousActor);
                }
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.ActorId, now, "clock_" + c.Action, "DeadlineClock", row.Id,
                    null, JsonSerializer.Serialize(new { row.OriginalDueAt, row.CurrentDueAt, row.AppointedActorId }, Json), c.Reason,
                    "activation.clock", eventId, ["originalDueAt", "currentDueAt", "appointedActorId"]));
                db.OutboxMessages.Add(OutboxMessage.Create(eventId, "clock." + c.Action + ".v1", now, row.Id,
                    JsonSerializer.Serialize(new
                    {
                        schemaVersion = 1,
                        eventId,
                        clockId = row.Id,
                        row.ProjectId,
                        row.OriginEventId,
                        row.OriginalDueAt,
                        row.CurrentDueAt,
                        row.AppointedActorId
                    }, Json)));
                await db.SaveChangesAsync(ct);
                return (eventId, JsonSerializer.Serialize(new
                {
                    row.Id,
                    row.ProjectId,
                    row.Kind,
                    row.OriginEventId,
                    row.OriginAt,
                    row.OriginalDueAt,
                    row.CurrentDueAt,
                    row.AppointedActorId,
                    row.AppointedRole,
                    row.Extensions,
                    row.Breaches,
                    row.Appointments,
                    numericalLimitPolicy = "PENDING_OWNER_DECISION",
                    additionalOfflineExecution = "NOT_AUTHORIZED",
                    version = Convert.ToBase64String(row.RowVersion)
                }, Json));
            }, token, receiptAccessGuard: ct => Guard(c, ct));
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var value = JsonSerializer.Deserialize<JsonElement>(result.OutcomeJson, Json);
            return new(result.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: value,
                Version: value.GetProperty("version").GetString(), Replayed: result.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await ExecuteAsync(command, token); }
        catch (ArgumentException) { db.ChangeTracker.Clear(); return new(400, "validation_error"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
}
