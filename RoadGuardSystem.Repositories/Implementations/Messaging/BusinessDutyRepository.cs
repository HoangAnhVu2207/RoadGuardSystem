using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class BusinessDutyRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider time) : IBusinessDutyRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class ExistingReceipt : Exception { }
    private sealed class Denied(int status, string code) : Exception { public BusinessDutyResult Result { get; } = new(status, code); }
    private static void Deny(int status, string code) => throw new Denied(status, code);

    internal static async Task<bool> CurrentAuthority(RoadGuardDbContext db, TimeProvider time,
        Guid actor, UserRoleCode role, Guid project, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        var day = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token) &&
            await db.ProjectMembers.AnyAsync(x => x.ProjectId == project && x.UserId == actor && x.RoleCode == role &&
                x.Status == ProjectMemberStatus.Active && x.ValidFrom <= day && (x.ValidTo == null || x.ValidTo >= day), token);
    }

    private async Task<BusinessReceivingRequest> Load(Guid project, Guid request, CancellationToken token)
    {
        var row = await db.Set<BusinessReceivingRequest>().FromSqlInterpolated(
            $"SELECT * FROM [BusinessReceivingRequests] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={request}")
            .Include(x => x.Clock).ThenInclude(x => x!.Breaches).Include(x => x.Appointments).AsSplitQuery().SingleOrDefaultAsync(token);
        if (row is null || row.ProjectId != project) throw new Denied(404, "not_found");
        return row;
    }

    internal static async Task<bool> SourceCurrent(RoadGuardDbContext db, BusinessReceivingRequest request, CancellationToken token)
    {
        if (request.SourceVersion != request.SourceId.ToString("N")) return false;
        if (request.SourceKind == "FieldReview")
        {
            var review = await db.Set<FieldInspectionReview>().AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == request.SourceId && x.ProjectId == request.ProjectId && x.TaskId == request.ScopeId &&
                x.Decision == "SUPPLEMENT" && x.OccurredAt == request.RequestedAt, token);
            if (review is null) return false;
            var submission = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == review.SubmissionId, token);
            return !await db.Set<FieldInspectionSubmission>().AnyAsync(x => x.TaskId == review.TaskId && x.Revision > submission.Revision, token) &&
                await db.FieldInspectionTasks.AnyAsync(x => x.Id == review.TaskId &&
                    x.Status != FieldInspectionTaskStatus.Completed && x.Status != FieldInspectionTaskStatus.Cancelled &&
                    x.Status != FieldInspectionTaskStatus.Submitted, token) &&
                await db.FieldInspectionAssignments.AnyAsync(x => x.FieldInspectionTaskId == review.TaskId &&
                    x.AssignedToUserId == request.ResponsibleActorId && x.Status == FieldInspectionAssignmentStatus.Active && x.EndedAt == null, token);
        }
        if (request.SourceKind == "RepairReview")
            return await db.Set<RepairAttemptReview>().AnyAsync(x => x.Id == request.SourceId && x.ProjectId == request.ProjectId &&
                x.Decision == "SUPPLEMENT" && x.At == request.RequestedAt && db.Set<RepairItem>().Any(i =>
                    i.Id == x.ItemId && i.CurrentReviewId == x.Id && i.SupersededByItemId == null &&
                    db.Set<RepairFieldTaskBinding>().Any(b => b.Id == i.CurrentBindingId && b.TaskId == request.ScopeId &&
                        b.CrewId == request.ResponsibleActorId && db.FieldInspectionAssignments.Any(a =>
                            a.Id == b.AssignmentId && a.FieldInspectionTaskId == b.TaskId && a.AssignedToUserId == b.CrewId &&
                            a.Status == FieldInspectionAssignmentStatus.Active && a.EndedAt == null))), token);
        if (request.SourceKind == "ReviewBreach")
            return await db.Set<DeadlineClock>().AnyAsync(x => x.Id == request.ScopeId && x.ProjectId == request.ProjectId &&
                x.Kind == DeadlineClockKind.ProjectManagerReview && x.CompletedAt == null &&
                x.Breaches.Any(b => b.Id == request.SourceId && b.ObservedAt == request.RequestedAt) &&
                db.FieldInspectionSubmissions.Any(s => s.Id == x.OriginEventId && s.RootId == s.Id && s.TaskId == x.TargetId &&
                    s.ProjectId == x.ProjectId && s.ServerReceivedAt == x.OriginAt), token);
        return false;
    }

    private async Task Guard(BusinessDutyCommand c, CancellationToken token)
    {
        if (!await CurrentAuthority(db, time, c.ActorId, c.Role, c.ProjectId, token)) Deny(403, "access_forbidden");
        var row = await Load(c.ProjectId, c.RequestId, token);
        if (c.Action == "ack")
        {
            if (row.ResponsibleActorId != c.ActorId || row.ResponsibleRole != c.Role) Deny(403, "current_assignee_required");
        }
        else if (c.Action == "appoint")
        {
            var required = row.Kind == DeadlineClockKind.CrewSupplement ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor;
            if (c.Role != required || c.AssigneeId is not Guid next ||
                !await CurrentAuthority(db, time, next, row.ResponsibleRole, c.ProjectId, token)) Deny(403, "assignee_not_authorized");
            // Crew replacements use the actual task assignment workflow and update this request there.
            if (row.Kind == DeadlineClockKind.CrewSupplement) Deny(409, "use_task_reassignment");
            var prior = await db.IdempotencyRecords.AnyAsync(x => x.ActorUserId == c.ActorId && x.ProjectId == c.ProjectId &&
                x.Operation == "activation.duty.appoint.v1" && x.IdempotencyKey == c.Key, token);
            if (prior && row.ResponsibleActorId != c.AssigneeId) Deny(409, "duty_assignment_superseded");
        }
        else Deny(400, "validation_error");
        if (row.CompletedAt is not null || !await SourceCurrent(db, row, token)) Deny(409, "request_source_stale");
    }

    public async Task<BusinessDutyResult> ExecuteAsync(BusinessDutyCommand c, CancellationToken token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(c.Key) || c.Key.Length > 200 || string.IsNullOrWhiteSpace(c.ExpectedVersion) ||
                c.Action is not ("ack" or "appoint") || c.Action == "appoint" &&
                (string.IsNullOrWhiteSpace(c.Reason) || c.Reason.Length > 2000)) return new(400, "validation_error");
            var operation = "activation.duty." + c.Action + ".v1";
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(c, Json))).ToLowerInvariant();
            var result = await receipts.ExecuteSerializableAsync(c.ActorId, c.ProjectId, operation, c.Key, fingerprint, async ct =>
            {
                await Guard(c, ct); var row = await Load(c.ProjectId, c.RequestId, ct);
                if (await db.IdempotencyRecords.AnyAsync(x => x.ActorUserId == c.ActorId && x.ProjectId == c.ProjectId &&
                    x.Operation == operation && x.IdempotencyKey == c.Key, ct)) throw new ExistingReceipt();
                // A reclick with a new key keeps the immutable first ACK. A stale pre-appointment version never does.
                if (Convert.ToBase64String(row.RowVersion) != c.ExpectedVersion) Deny(409, "concurrency_conflict");
                if (c.Action == "ack" && row.AcknowledgmentId is Guid firstAck)
                    return (firstAck, JsonSerializer.Serialize(View(row), Json));
                var now = time.GetUtcNow(); var eventId = Guid.NewGuid();
                if (c.Action == "ack") row.Acknowledge(c.ActorId, eventId, now, c.ClaimedDeviceAt);
                else row.Appoint(c.AssigneeId!.Value, c.ActorId, c.Reason!, now);
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.ActorId, now,
                    c.Action == "ack" ? "business_request_received" : "business_duty_appointed", "BusinessReceivingRequest",
                    row.Id, null, JsonSerializer.Serialize(new { row.AcknowledgmentId, row.ClockId, row.ResponsibleActorId }, Json),
                    c.Reason ?? "Đã nhận yêu cầu", "activation.duty", eventId,
                    ["acknowledgmentId", "clockId", "responsibleActorId"]));
                db.OutboxMessages.Add(OutboxMessage.Create(eventId, "business.duty." + c.Action + ".v1", now, row.Id,
                    JsonSerializer.Serialize(new
                    {
                        schemaVersion = 1,
                        eventId,
                        row.ProjectId,
                        requestId = row.Id,
                        row.SourceKind,
                        row.SourceId,
                        row.SourceVersion,
                        row.AcknowledgmentId,
                        row.ClockId,
                        row.ResponsibleActorId
                    }, Json)));
                await db.SaveChangesAsync(ct);
                return (eventId, JsonSerializer.Serialize(View(row), Json));
            }, token, receiptAccessGuard: ct => Guard(c, ct));
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var view = JsonSerializer.Deserialize<BusinessDutyView>(result.OutcomeJson, Json)!;
            return new(result.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: view, Version: view.Version,
                Replayed: result.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await ExecuteAsync(c, token); }
        catch (ArgumentException) { db.ChangeTracker.Clear(); return new(400, "validation_error"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }

    public async Task<BusinessDutyResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid request, CancellationToken token)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                if (!await CurrentAuthority(db, time, actor, role, project, token)) Deny(403, "access_forbidden");
                var row = await Load(project, request, token);
                if (actor != row.ResponsibleActorId && role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) Deny(403, "access_forbidden");
                var value = View(row); await tx.CommitAsync(token); return new BusinessDutyResult(200, Value: value, Version: value.Version);
            });
        }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
    }
    public async Task<BusinessDutyResult> ListAsync(Guid actor, UserRoleCode role, Guid project, Guid? scope, Guid? after, int limit, CancellationToken token)
    {
        if (limit is < 1 or > 100 || scope == Guid.Empty || after == Guid.Empty) return new(400, "validation_error");
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                if (!await CurrentAuthority(db, time, actor, role, project, token)) Deny(403, "access_forbidden");
                var query = db.Set<BusinessReceivingRequest>().AsNoTracking().Where(x => x.ProjectId == project);
                if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) query = query.Where(x => x.ResponsibleActorId == actor);
                if (scope is Guid scopeId) query = query.Where(x => x.ScopeId == scopeId);
                if (after is Guid afterId) query = query.Where(x => x.Id.CompareTo(afterId) > 0);
                var rows = await query.OrderBy(x => x.Id).Take(limit + 1).Include(x => x.Clock).Include(x => x.Appointments).AsSplitQuery().ToArrayAsync(token);
                var selected = rows.Take(limit).ToArray();
                var value = new { items = selected.Select(View).ToArray(), continuation = rows.Length > limit ? selected[^1].Id : (Guid?)null };
                await tx.CommitAsync(token); return new BusinessDutyResult(200, Value: value);
            });
        }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
    }
    private BusinessDutyView View(BusinessReceivingRequest row) => new(row.Id, row.ProjectId, row.Kind.ToString(), row.SourceKind,
        row.SourceId, row.SourceVersion, row.ScopeId, row.ResponsibleActorId, row.ResponsibleRole.ToString(), row.RequestedAt,
        row.AcknowledgedAt is null ? "Chưa xác nhận nhận" : "Đã nhận yêu cầu",
        Math.Max(0, ((row.AcknowledgedAt ?? time.GetUtcNow()) - row.RequestedAt).TotalSeconds), row.AcknowledgmentId,
        row.AcknowledgedBy, row.AcknowledgedAt, row.ClaimedDeviceAt, row.ClockId, row.Clock?.OriginalDueAt, row.Clock?.CurrentDueAt,
        row.CompletedAt, Convert.ToBase64String(row.RowVersion), row.Appointments.OrderBy(x => x.EffectiveAt).Select(x => (object)new
        { x.Id, x.PreviousActorId, x.CurrentActorId, x.DecisionActorId, x.Reason, x.EffectiveAt }).ToArray());
}
