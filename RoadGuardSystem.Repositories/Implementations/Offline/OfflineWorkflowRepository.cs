using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;

namespace RoadGuardSystem.Repositories.Implementations.Offline;

public sealed partial class OfflineWorkflowRepository(RoadGuardDbContext db, TimeProvider clock,
    IFieldInspectionWorkflowRepository? fieldCore = null,
    IOfflineFieldAdmissionValidator? fieldAdmission = null,
    IOfflineRepairCommandAdapter? repairCore = null) : IOfflineWorkflowRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class CommittedReceipt : Exception { }
    public async Task<OfflineWorkflowFact> ExecuteAsync(OfflineWorkflowCommand command, OfflineWorkflowAlgorithms algorithms,
        Func<CancellationToken, Task<bool>> currentProjectGuard, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(algorithms);
        ArgumentNullException.ThrowIfNull(currentProjectGuard);
        async Task Guard(CancellationToken token)
        {
            await Anh02ReceiptAuthority.LockAsync(db, command.ActorId, command.ProjectId, token);
            if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(command.ActorId, command.Role, token) ||
                !await currentProjectGuard(token))
                throw new OfflineAdmissionRejectedException(403, "access_forbidden");
            if (command.Action == "device-register" && command.Role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew))
                throw new OfflineAdmissionRejectedException(403, "access_forbidden");
            if (command.Action == "device-revoke")
                await GuardDeviceResourceAsync(command, token);
            if (command.Action == "snapshot-create")
                await GuardSnapshotScopeAsync(command, token);
        }
        try
        {
            if (command.Action == "sync")
                return await SynchronizeAsync(command, algorithms, Guard, cancellationToken);
            if (command.Action is "package-export" or "package-prepare" or "grant-issue" or "grant-revoke" or
                "artifact-register" or "import" or "start-reconcile" or "package-get" or "grant-get" or
                "artifact-get" or "batch-get" or "origin-get")
                return await ExecuteTransferAsync(command, algorithms, Guard, cancellationToken);
            if (command.Action is "device-register" or "device-revoke" or "snapshot-create")
            {
                if (db.Database.CurrentTransaction is not null)
                    throw new InvalidOperationException("Offline command receipts own their transaction.");
                var operation = "h5.offline." + command.Action + ".v1";
                var fingerprint = command.Action == "device-revoke"
                    ? Digest(JsonSerializer.Serialize(new { command.ProjectId, command.Action, command.ResourceId, command.Input }, Json))
                    : Digest(JsonSerializer.Serialize(new { command.ProjectId, command.Action, command.Input }, Json));
                var receipt = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId, command.ProjectId,
                    operation, command.Key!, fingerprint, async token =>
                    {
                        await Guard(token);
                        if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == command.ActorId &&
                            row.ProjectId == command.ProjectId && row.Operation == operation && row.IdempotencyKey == command.Key, token))
                            throw new CommittedReceipt();
                        if (!await db.Projects.AsNoTracking().AnyAsync(row => row.Id == command.ProjectId && row.Status == ProjectStatus.Active, token))
                            throw new OfflineAdmissionRejectedException(409, "project_not_active");
                        var value = command.Action switch
                        {
                            "device-register" => await RegisterDeviceResultAsync(command, token),
                            "device-revoke" => await RevokeDeviceResultAsync(command, token),
                            _ => await CaptureSnapshotAsync(command, token)
                        };
                        await db.SaveChangesAsync(token);
                        return (value.Id, JsonSerializer.Serialize(value.Value, Json));
                    }, cancellationToken, receiptAccessGuard: Guard);
                if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
                return new(receipt.Status == IdempotencyOperationStatus.Replayed ? 200 : 201,
                    Value: JsonSerializer.Deserialize<JsonElement>(receipt.OutcomeJson, Json), Replayed: receipt.Status == IdempotencyOperationStatus.Replayed);
            }
            async Task<OfflineWorkflowFact> Read(CancellationToken token)
            {
                await Guard(token);
                if (command.Action == "device-get")
                {
                    var device = await db.Set<OfflineDeviceRegistration>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.Id == command.ResourceId && row.ProjectId == command.ProjectId, token);
                    if (device is null) return new(404, "not_found");
                    if (command.Role == UserRoleCode.RepairCrew && device.ActorId != command.ActorId) return new(403, "access_forbidden");
                    var revoked = await db.Set<OfflineDeviceRevocation>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.DeviceRegistrationId == device.Id && row.ProjectId == command.ProjectId, token);
                    return new(200, Value: DeviceView(device, revoked));
                }
                if (command.Action == "snapshot-get")
                {
                    var snapshot = await db.Set<OfflineTaskSnapshot>().FromSqlInterpolated(
                        $"SELECT * FROM [OfflineTaskSnapshots] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={command.ResourceId} AND [ProjectId]={command.ProjectId}")
                        .AsNoTracking().SingleOrDefaultAsync(token);
                    if (snapshot is null) return new(404, "not_found");
                    if (command.Role != UserRoleCode.RepairCrew || snapshot.OriginalActorId != command.ActorId ||
                        !await db.Set<OfflineDeviceRegistration>().AsNoTracking().AnyAsync(device =>
                            device.Id == snapshot.DeviceRegistrationId && device.ProjectId == command.ProjectId &&
                            device.ActorId == command.ActorId && device.RoleSnapshot == command.Role, token) ||
                        await db.Set<OfflineDeviceRevocation>().AsNoTracking().AnyAsync(revocation =>
                            revocation.DeviceRegistrationId == snapshot.DeviceRegistrationId, token) ||
                        !await db.FieldInspectionTasks.AsNoTracking().AnyAsync(task =>
                            task.Id == snapshot.TaskId && task.ProjectId == command.ProjectId, token) ||
                        !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(assignment =>
                            assignment.Id == snapshot.AssignmentId && assignment.FieldInspectionTaskId == snapshot.TaskId &&
                            assignment.AssignedToUserId == command.ActorId &&
                            assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null, token))
                        return new(403, "access_forbidden");
                    return new(200, Value: SnapshotView(snapshot));
                }
                throw new OfflineAdmissionRejectedException(400, "validation_error");
            }
            if (db.Database.CurrentTransaction is not null) return await Read(cancellationToken);
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var result = await Read(cancellationToken);
                await transaction.CommitAsync(cancellationToken); return result;
            });
        }
        catch (CommittedReceipt)
        {
            db.ChangeTracker.Clear(); return await ExecuteAsync(command, algorithms, currentProjectGuard, cancellationToken);
        }
        catch (OfflineAdmissionRejectedException rejected) { return new(rejected.Status, rejected.Code); }
        catch (ArgumentException) { return new(400, "validation_error"); }
        catch (JsonException) { return new(400, "validation_error"); }
        catch (CryptographicException) { return new(400, "offline_signature_invalid"); }
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private async Task<(Guid Id, object Value)> RegisterDeviceResultAsync(OfflineWorkflowCommand command, CancellationToken cancellationToken)
    {
        var row = await RegisterDeviceAsync(command, cancellationToken); return (row.Id, DeviceView(row, null));
    }

    private async Task<(Guid Id, object Value)> RevokeDeviceResultAsync(OfflineWorkflowCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Input is not OfflineDeviceRevokeData input || command.ResourceId is not Guid id ||
            string.IsNullOrWhiteSpace(input.Reason)) throw new ArgumentException("A bounded device and reason are required.");
        var device = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (device is null) throw new OfflineAdmissionRejectedException(404, "not_found");
        if (device.ActorId != command.ActorId || device.RoleSnapshot != command.Role)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (await db.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={id}")
            .AsNoTracking().AnyAsync(cancellationToken))
            throw new OfflineAdmissionRejectedException(409, "offline_key_revoked");
        var row = OfflineDeviceRevocation.Record(Guid.NewGuid(), command.ProjectId, id, command.ActorId,
            clock.GetUtcNow(), input.Reason);
        db.Add(row);
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, row.RevokedAt, "offline_device_revoked",
            "OfflineDeviceRegistration", id, null, JsonSerializer.Serialize(new { row.Id, row.DeviceRegistrationId }, Json),
            row.Reason, "h5.offline.v1", row.Id, ["deviceRegistrationId"]));
        db.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), "offline.device.revoked.v1", row.RevokedAt,
            id, JsonSerializer.Serialize(new
            {
                projectId = command.ProjectId,
                sourceKind = "OfflineDeviceRegistration",
                sourceId = id,
                originEventId = row.Id,
                occurredAtUtc = row.RevokedAt
            }, Json)));
        return (row.Id, DeviceView(device, row));
    }

    private async Task GuardDeviceResourceAsync(OfflineWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (command.ResourceId is not Guid id || id == Guid.Empty)
            throw new ArgumentException("A device registration is required.");
        var device = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (device is null) throw new OfflineAdmissionRejectedException(404, "not_found");
        if (device.ActorId != command.ActorId || device.RoleSnapshot != command.Role)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
    }

    private async Task<(Guid Id, object Value)> CaptureSnapshotAsync(OfflineWorkflowCommand command, CancellationToken cancellationToken)
    {
        var (device, task, assignment) = await GuardSnapshotScopeAsync(command, cancellationToken);
        if (task.LifecycleVersion != 2 || task.Status is FieldInspectionTaskStatus.Completed or FieldInspectionTaskStatus.Cancelled or FieldInspectionTaskStatus.Rejected)
            throw new OfflineAdmissionRejectedException(409, "source_not_ready");
        if (task.SegmentSetId is null || fieldCore is null)
            throw new OfflineAdmissionRejectedException(409, "offline_location_source_unavailable");
        object location;
        try
        {
            var geometry = await fieldCore.ApplyInTransactionAsync(new(command.ProjectId, task.Id, "geometry",
                null, null, null, new(command.ActorId, command.Role, command.ActorId, "DIRECT", false)),
                _ => Task.FromResult(true), cancellationToken);
            if (geometry.Status != 200 || geometry.Value is null)
                throw new OfflineAdmissionRejectedException(geometry.Status,
                    geometry.Code ?? "offline_location_source_unavailable");
            location = geometry.Value;
        }
        catch (FieldCoreRejectedException rejected)
        {
            throw new OfflineAdmissionRejectedException(rejected.Result.Status,
                rejected.Result.Code ?? "offline_location_source_unavailable");
        }
        object? repairSource = null;
        if (task.RepairItemId is Guid repairItemId)
        {
            if (repairCore is null)
                throw new OfflineAdmissionRejectedException(409, "repair_adapter_not_ready");
            var facts = await repairCore.ReadSnapshotInTransactionAsync(new(command.ProjectId, task.Id,
                command.ActorId, command.Role), cancellationToken);
            if (facts is null || facts.ItemId != repairItemId || facts.TaskId != task.Id ||
                facts.AssignmentId != assignment.Id || facts.OriginalActorId != command.ActorId)
                throw new OfflineAdmissionRejectedException(409, "repair_snapshot_source_unavailable");
            repairSource = facts.SafePayload;
        }
        var assignmentJson = JsonSerializer.Serialize(new
        {
            assignment.Id,
            assignment.FieldInspectionTaskId,
            assignment.AssignedToUserId,
            assignment.AssignedByUserId,
            assignment.AssignedAt
        }, Json);
        var payload = JsonSerializer.Serialize(new
        {
            task.Id,
            task.ProjectId,
            task.DefectId,
            task.SurveyId,
            task.SourceKind,
            task.RoadSectionVersionId,
            task.SegmentSetId,
            task.LayoutRevisionId,
            task.SlabId,
            task.MapPublicationId,
            task.CrsProfileRevisionId,
            task.RequiredMeasurementType,
            task.MeasurementScope,
            task.Instructions,
            task.TaskMode,
            task.RepairItemId,
            purpose = task.Purpose.ToString(),
            status = task.Status.ToString(),
            task.DueAt,
            assignmentId = assignment.Id,
            originalActorId = assignment.AssignedToUserId,
            location,
            repair = repairSource
        }, Json);
        var row = OfflineTaskSnapshot.Capture(Guid.NewGuid(), command.ProjectId, task.Id, assignment.Id,
            command.ActorId, device.Id, Convert.ToBase64String(task.RowVersion), Digest(assignmentJson), payload, clock.GetUtcNow());
        db.Add(row);
        return (row.Id, SnapshotView(row));
    }

    private static object SnapshotView(OfflineTaskSnapshot row) => new
    {
        row.Id,
        row.TaskId,
        row.AssignmentId,
        row.OriginalActorId,
        row.DeviceRegistrationId,
        row.TaskVersion,
        row.AssignmentHash,
        row.ContentHash,
        row.DownloadedAt,
        snapshot = JsonSerializer.Deserialize<JsonElement>(row.SnapshotJson, Json)
    };

    private async Task<(OfflineDeviceRegistration Device, FieldInspectionTask Task, FieldInspectionAssignment Assignment)>
        GuardSnapshotScopeAsync(OfflineWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (command.Input is not OfflineSnapshotData input) throw new ArgumentException("Typed snapshot input is required.");
        var device = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.DeviceRegistrationId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (device is null || device.ActorId != command.ActorId || device.RoleSnapshot != command.Role ||
            await db.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={input.DeviceRegistrationId}")
                .AsNoTracking().AnyAsync(cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var task = await db.FieldInspectionTasks.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.TaskId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (task is null) throw new OfflineAdmissionRejectedException(404, "not_found");
        var assignment = await db.FieldInspectionAssignments.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [FieldInspectionTaskId]={task.Id}")
            .AsNoTracking().SingleOrDefaultAsync(row => row.AssignedToUserId == command.ActorId &&
                row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, cancellationToken);
        if (assignment is null) throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        return (device, task, assignment);
    }

    private async Task<OfflineDeviceRegistration> RegisterDeviceAsync(OfflineWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (command.Input is not OfflineDeviceRegisterData input) throw new ArgumentException("Typed device registration is required.");
        var registrations = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={command.ProjectId} AND [ActorId]={command.ActorId} AND [DeviceId]={input.DeviceId}")
            .AsNoTracking().ToArrayAsync(cancellationToken);
        var proposed = OfflineDeviceRegistration.Register(Guid.NewGuid(), command.ProjectId, command.ActorId, input.DeviceId, 1,
            command.Role, input.EncryptionPublicKey, input.SigningPublicKey, clock.GetUtcNow());
        foreach (var existing in registrations.Where(row => row.KeyFingerprint == proposed.KeyFingerprint))
        {
            if (await db.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={existing.Id}")
                .AsNoTracking().AnyAsync(cancellationToken))
                throw new OfflineAdmissionRejectedException(409, "offline_key_revoked");
            if (existing.RoleSnapshot == command.Role) return existing;
        }
        var lastRevision = registrations.Length == 0 ? 0 : registrations.Max(row => row.Revision);
        if (lastRevision == int.MaxValue) throw new OfflineAdmissionRejectedException(409, "device_revision_limit");
        var row = OfflineDeviceRegistration.Register(proposed.Id, command.ProjectId, command.ActorId, input.DeviceId,
            lastRevision + 1, command.Role, input.EncryptionPublicKey, input.SigningPublicKey, proposed.RegisteredAt);
        db.Add(row);
        var audit = JsonSerializer.Serialize(new { projectId = row.ProjectId, deviceId = row.DeviceId, registrationId = row.Id, revision = row.Revision }, Json);
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, row.RegisteredAt, "offline_device_registered",
            "OfflineDeviceRegistration", row.Id, null, audit, null, "h5.offline.v1", row.Id,
            ["projectId", "deviceId", "registrationId", "revision"]));
        db.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), "offline.device.registered.v1", row.RegisteredAt, row.Id,
            JsonSerializer.Serialize(new
            {
                projectId = row.ProjectId,
                sourceKind = "OfflineDeviceRegistration",
                sourceId = row.Id,
                originEventId = row.Id,
                occurredAtUtc = row.RegisteredAt
            }, Json)));
        return row;
    }

    private static object DeviceView(OfflineDeviceRegistration row, OfflineDeviceRevocation? revocation) => new
    {
        row.Id,
        row.ProjectId,
        row.ActorId,
        row.DeviceId,
        row.Revision,
        role = row.RoleSnapshot.ToString(),
        row.EncryptionPublicKey,
        row.SigningPublicKey,
        row.KeyFingerprint,
        row.RegisteredAt,
        revokedAt = revocation?.RevokedAt
    };
}
