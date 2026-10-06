using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;

namespace RoadGuardSystem.Repositories.Implementations.Offline;

public sealed partial class OfflineWorkflowRepository
{
    private sealed record StagedBatch(OfflineSyncBatch Batch, Dictionary<Guid, OfflineOperationAdmission> Admissions);

    private async Task<OfflineWorkflowFact> SynchronizeAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, Func<CancellationToken, Task> currentGuard, CancellationToken cancellationToken)
    {
        if (command.Input is not OfflineSignedBatchData input)
            throw new ArgumentException("A signed batch is required.");
        return await ProcessBatchAsync(command, algorithms, currentGuard, input, null, cancellationToken);
    }

    private async Task<OfflineWorkflowFact> ProcessBatchAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, Func<CancellationToken, Task> currentGuard,
        OfflineSignedBatchData input, OfflinePackageImportData? imported, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Offline batch processing owns per-item transactions.");
        if (input.Operations is null || input.Operations.Length is < 1 or > 1000)
            throw new ArgumentException("A bounded signed batch is required.");
        var staged = await OwnTransactionAsync(async token =>
        {
            await currentGuard(token);
            var source = imported is null
                ? await GuardSyncSourceAsync(command, input.SourceDeviceRegistrationId, token)
                : await GuardImportTransportAsync(command, algorithms, input, imported, token);
            var descriptors = input.Operations.Select(algorithms.Describe).ToArray();
            var manifest = algorithms.CanonicalManifest(command.ProjectId, input.BatchId, source.Id, descriptors);
            OfflinePackageAuthentication.VerifyClaim(manifest, input.Signature, source.SigningPublicKey);
            var attached = Encoding.UTF8.GetString(algorithms.CanonicalAttachedPayload(input.BatchId, source.Id, input.Operations, input.Signature));
            var manifestJson = Encoding.UTF8.GetString(manifest); var hash = Digest(manifestJson);
            var batches = await db.Set<OfflineSyncBatch>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineSyncBatches] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={command.ProjectId} AND [SourceDeviceRegistrationId]={source.Id} AND [SourceBatchId]={input.BatchId} AND [CurrentImporterId]={command.ActorId}")
                .AsNoTracking().ToArrayAsync(token);
            if (batches.Length > 1) throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
            var batch = batches.SingleOrDefault();
            if (batch is not null && (batch.ContentHash != hash || batch.AttachedPayloadJson != attached ||
                batch.GrantId != imported?.GrantId || batch.PackageId != imported?.PackageId ||
                batch.RecipientDeviceRegistrationId != imported?.RecipientDeviceRegistrationId ||
                batch.RecipientSignature != imported?.RecipientSignature))
                throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
            if (batch is null)
            {
                batch = OfflineSyncBatch.ReceiveAuthenticated(imported?.BatchId ?? Guid.NewGuid(), command.ProjectId, input.BatchId, source.Id,
                    command.ActorId, imported?.PackageId, imported?.GrantId, manifestJson, input.Signature,
                    attached, clock.GetUtcNow(), imported?.RecipientDeviceRegistrationId,
                    imported?.RecipientSignature); db.Add(batch);
                await db.SaveChangesAsync(token);
            }
            var admissions = new Dictionary<Guid, OfflineOperationAdmission>();
            foreach (var operation in input.Operations)
            {
                await GuardSyncSnapshotAsync(command, source, operation, token, imported is not null);
                if (operation.Repair is not null && (repairCore is null || repairCore.ComputeCoreHash(operation) != operation.CorePayloadHash))
                    throw new OfflineAdmissionRejectedException(409, "repair_adapter_not_ready");
                var validator = fieldAdmission ?? new OfflineAdmissionValidator(db, clock);
                await validator.GuardOriginBindingAsync(command.ProjectId, operation.OriginId, operation.Kind,
                    operation.CorePayloadHash, operation.OriginalActorId, operation.TaskId, token);
                var binding = await db.Set<OfflineOperationBinding>().FromSqlInterpolated(
                    $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={command.ProjectId} AND [OriginId]={operation.OriginId}")
                    .AsNoTracking().SingleOrDefaultAsync(token);
                var envelope = JsonSerializer.Serialize(operation, Json);
                if (binding is not null && (binding.EnvelopeHash != algorithms.EnvelopeHash(operation) ||
                    binding.SourceDeviceRegistrationId != source.Id || binding.SnapshotId != operation.SnapshotId))
                    throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
                if (binding is null)
                {
                    if (await db.Set<OfflineOperationBinding>().FromSqlInterpolated(
                        $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [EffectId]={operation.EffectId}")
                        .AsNoTracking().AnyAsync(token)) throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
                    binding = OfflineOperationBinding.Bind(Guid.NewGuid(), command.ProjectId, operation.OriginId, operation.EffectId,
                        operation.Kind, operation.CorePayloadHash, algorithms.EnvelopeHash(operation), operation.OriginalActorId,
                        source.Id, operation.TaskId, operation.AssignmentId, operation.SnapshotId, envelope, clock.GetUtcNow(), operation.Repair?.ResourceId);
                    db.Add(binding); await db.SaveChangesAsync(token);
                }
                var admission = await db.Set<OfflineOperationAdmission>().FromSqlInterpolated(
                    $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [BatchId]={batch.Id} AND [BindingId]={binding.Id}")
                    .AsNoTracking().SingleOrDefaultAsync(token);
                if (admission is null)
                {
                    admission = OfflineOperationAdmission.Record(Guid.NewGuid(), command.ProjectId, batch.Id, binding.Id,
                        command.ActorId, command.Role, imported?.GrantId, JsonSerializer.Serialize(new { operation.TaskId, operation.AssignmentId,
                            operation.SnapshotId, sourceRegistrationId = source.Id, claimStatus = "SIGNED_ORIGIN_UNVERIFIED_TIME" }, Json), clock.GetUtcNow());
                    db.Add(admission); await db.SaveChangesAsync(token);
                }
                admissions.Add(operation.OriginId, admission);
            }
            return new StagedBatch(batch, admissions);
        }, cancellationToken);

        var outcomes = new Dictionary<Guid, OfflineOperationResult>(); var remaining = input.Operations.ToList();
        while (remaining.Count > 0)
        {
            var ready = remaining.Where(operation => operation.Dependencies.All(id => outcomes.TryGetValue(id, out var result) && result.DurableAck)).ToArray();
            if (ready.Length == 0)
            {
                foreach (var operation in remaining)
                    outcomes.Add(operation.OriginId, await RetainFailureAsync(command, input.SourceDeviceRegistrationId,
                        staged.Batch, staged.Admissions[operation.OriginId], operation, "PENDING_DEPENDENCY", "dependency_not_committed", currentGuard, imported, algorithms, input, cancellationToken));
                break;
            }
            foreach (var operation in ready)
            {
                var admission = staged.Admissions[operation.OriginId];
                try
                {
                    outcomes.Add(operation.OriginId, await ApplyItemAsync(command, input.SourceDeviceRegistrationId, staged.Batch,
                        admission, operation, outcomes, currentGuard, imported, algorithms, input, cancellationToken));
                }
                catch (FieldCoreRejectedException rejected)
                {
                    outcomes.Add(operation.OriginId, await RetainFailureAsync(command, input.SourceDeviceRegistrationId,
                        staged.Batch, admission, operation, rejected.Result.Code == "stale_snapshot" ? "STALE_SNAPSHOT" : "REJECTED",
                        rejected.Result.Code ?? "field_command_rejected", currentGuard, imported, algorithms, input, cancellationToken));
                }
                catch (OfflineAdmissionRejectedException rejected) when (rejected.Status == 409)
                {
                    outcomes.Add(operation.OriginId, await RetainFailureAsync(command, input.SourceDeviceRegistrationId,
                        staged.Batch, admission, operation, rejected.Code == "stale_snapshot" ? "STALE_SNAPSHOT" : "CONFLICT",
                        rejected.Code, currentGuard, imported, algorithms, input, cancellationToken));
                }
                remaining.Remove(operation);
            }
        }
        var canonicalIds = await OwnTransactionAsync(async token =>
        {
            await currentGuard(token);
            if (imported is null) await GuardSyncSourceAsync(command, input.SourceDeviceRegistrationId, token);
            else await GuardImportTransportAsync(command, algorithms, input, imported, token);
            foreach (var operation in input.Operations.Where(operation => outcomes[operation.OriginId].DurableAck &&
                         operation.Repair is not null))
            {
                var caller = new FieldAdmissionContext(command.ActorId, command.Role,
                    operation.OriginalActorId, imported is null ? "SYNC" : "HANDOVER", false,
                    imported?.GrantId, staged.Admissions[operation.OriginId].Id);
                await ((fieldAdmission as IOfflineRepairAdmissionValidator) ??
                    new OfflineAdmissionValidator(db, clock)).ValidateRepairAsync(operation, command.ProjectId,
                    caller, token);
            }
            foreach (var operation in input.Operations.Where(operation => outcomes[operation.OriginId].DurableAck && operation.Repair is null))
            {
                var body = FieldBody(operation);
                await (fieldAdmission ?? new OfflineAdmissionValidator(db, clock)).ValidateAsync(
                    new(command.ProjectId, operation.TaskId, FieldAction(operation) ?? throw new ArgumentException("A typed FIELD action is required."), body, null, operation.TaskVersion,
                        new(command.ActorId, command.Role, operation.OriginalActorId,
                            imported is null ? "SYNC" : "HANDOVER", false, imported?.GrantId,
                            staged.Admissions[operation.OriginId].Id)), token);
            }
            var ids = input.Operations.Select(operation => operation.OriginId).ToArray();
            return await db.Set<FieldInspectionOperationOrigin>().AsNoTracking().Where(row => row.ProjectId == command.ProjectId && ids.Contains(row.OriginId))
                .ToDictionaryAsync(row => row.OriginId, row => row.Id, token);
        }, cancellationToken);
        var items = input.Operations.Select(operation => ItemView(outcomes[operation.OriginId], operation.Kind,
            canonicalIds.GetValueOrDefault(operation.OriginId))).ToArray();
        return new(200, Value: new OfflineBatchFact(staged.Batch.Id, items));
    }

    private async Task<OfflineOperationResult> ApplyItemAsync(OfflineWorkflowCommand command, Guid sourceRegistration,
        OfflineSyncBatch batch, OfflineOperationAdmission admission, OfflineOperationData operation,
        Dictionary<Guid, OfflineOperationResult> predecessors, Func<CancellationToken, Task> currentGuard,
        OfflinePackageImportData? imported, OfflineWorkflowAlgorithms algorithms, OfflineSignedBatchData input,
        CancellationToken cancellationToken)
        => await OwnTransactionAsync(async token =>
        {
            await currentGuard(token);
            if (imported is null) await GuardSyncSourceAsync(command, sourceRegistration, token);
            else await GuardImportTransportAsync(command, algorithms, input, imported, token);
            if (operation.Repair is not null)
            {
                var caller = new FieldAdmissionContext(command.ActorId, command.Role,
                    operation.OriginalActorId, imported is null ? "SYNC" : "HANDOVER", false,
                    imported?.GrantId, admission.Id);
                var repairProof = await ((fieldAdmission as IOfflineRepairAdmissionValidator) ??
                    new OfflineAdmissionValidator(db, clock)).ValidateRepairAsync(operation, command.ProjectId,
                    caller, token) ?? throw new OfflineAdmissionRejectedException(403,
                    "offline_admission_proof_unavailable");
                var existingRepair = await db.Set<OfflineOperationResult>().FromSqlInterpolated(
                    $"SELECT * FROM [OfflineOperationResults] WITH (UPDLOCK,HOLDLOCK) WHERE [AdmissionId]={admission.Id} AND [DurableAck]=1")
                    .AsNoTracking().SingleOrDefaultAsync(token);
                if (existingRepair is not null) return existingRepair;
                if (repairCore is null) throw new OfflineAdmissionRejectedException(409, "repair_adapter_not_ready");
                var repairExpected = PredecessorVersion(operation, input.Operations, predecessors);
                if (repairExpected is null)
                {
                    var snapshotJson = await db.Set<OfflineTaskSnapshot>().AsNoTracking()
                        .Where(row => row.Id == operation.SnapshotId && row.ProjectId == command.ProjectId)
                        .Select(row => row.SnapshotJson).SingleAsync(token);
                    using var snapshot = JsonDocument.Parse(snapshotJson);
                    if (!snapshot.RootElement.TryGetProperty("repair", out var repair) ||
                        repair.ValueKind != JsonValueKind.Object ||
                        !repair.TryGetProperty("itemVersion", out var itemVersion))
                        throw new OfflineAdmissionRejectedException(409, "repair_snapshot_source_unavailable");
                    repairExpected = itemVersion.GetString();
                }
                if (string.IsNullOrWhiteSpace(repairExpected))
                    throw new OfflineAdmissionRejectedException(409, "repair_snapshot_source_unavailable");
                var repairEffect = await repairCore.ApplyInTransactionAsync(new(operation, command.ProjectId,
                    command.ActorId, command.Role, repairProof, repairExpected), token);
                if (repairEffect.Code is not null || repairEffect.Status is not (200 or 201) ||
                    repairEffect.EffectId != repairProof.EffectId || repairEffect.CanonicalOriginId is null ||
                    repairEffect.SafeOutcome is null || string.IsNullOrWhiteSpace(repairEffect.ResourceVersion))
                    throw new OfflineAdmissionRejectedException(repairEffect.Code is null ? 409 : repairEffect.Status,
                        repairEffect.Code ?? "offline_effect_identity_mismatch");
                var received = clock.GetUtcNow();
                var verifiedFinish = operation.Kind == "REPAIR_EXECUTION_FINISH"
                    ? repairEffect.IndependentlyVerifiedFinishedAt : null;
                var repairResult = OfflineOperationResult.Record(Guid.NewGuid(), command.ProjectId, batch.Id,
                    admission.Id, operation.OriginId, repairEffect.EffectId, "COMMITTED", null,
                    verifiedFinish.HasValue ? "VERIFIED_ORIGINAL" : "UNCERTAIN",
                    verifiedFinish.HasValue ? algorithms.SyncLateness(verifiedFinish, received) : "UNKNOWN",
                    operation.ClaimedFinishedAt, verifiedFinish,
                    JsonSerializer.Serialize(repairEffect.SafeOutcome, Json), repairEffect.ResourceVersion, received);
                db.Add(repairResult); await db.SaveChangesAsync(token); await currentGuard(token); return repairResult;
            }
            var action = FieldAction(operation);
            if (action is null)
            {
                if (repairCore is null) throw new OfflineAdmissionRejectedException(409, "repair_adapter_not_ready");
                throw new OfflineAdmissionRejectedException(409, "repair_admission_not_ready");
            }
            var body = FieldBody(operation);
            var expected = PredecessorVersion(operation, input.Operations, predecessors) ?? operation.TaskVersion;
            var fieldCommand = new FieldWorkflowCommand(command.ProjectId, operation.TaskId, action, body, null, expected,
                new(command.ActorId, command.Role, operation.OriginalActorId,
                    imported is null ? "SYNC" : "HANDOVER", false, imported?.GrantId, admission.Id));
            var validator = fieldAdmission ?? new OfflineAdmissionValidator(db, clock);
            var proof = await validator.ValidateAsync(fieldCommand, token)
                ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
            var existing = await db.Set<OfflineOperationResult>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineOperationResults] WITH (UPDLOCK,HOLDLOCK) WHERE [AdmissionId]={admission.Id} AND [DurableAck]=1")
                .AsNoTracking().SingleOrDefaultAsync(token);
            if (existing is not null) return existing;
            if (fieldCore is null) throw new OfflineAdmissionRejectedException(409, "field_adapter_not_ready");
            var effect = await fieldCore.ApplyInternalInTransactionAsync(fieldCommand,
                async inner => { await currentGuard(inner); return true; }, token);
            if (effect.Code is not null) throw new FieldCoreRejectedException(effect);
            var canonical = await db.Set<FieldInspectionOperationOrigin>().FromSqlInterpolated(
                $"SELECT * FROM [FieldInspectionOperationOrigins] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={command.ProjectId} AND [OriginId]={operation.OriginId}")
                .AsNoTracking().SingleOrDefaultAsync(token);
            if (canonical is null || canonical.Kind != operation.Kind || canonical.ContentHash != operation.CorePayloadHash ||
                canonical.EffectId != proof.EffectId || canonical.TaskId != operation.TaskId || canonical.OriginalActorId != operation.OriginalActorId)
                throw new OfflineAdmissionRejectedException(409, "offline_effect_identity_mismatch");
            var version = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking().Where(row => row.Id == operation.TaskId)
                .Select(row => row.RowVersion).SingleAsync(token));
            var result = OfflineOperationResult.Record(Guid.NewGuid(), command.ProjectId, batch.Id, admission.Id, operation.OriginId,
                proof.EffectId, "COMMITTED", null, proof.VerifiedOriginalAt.HasValue ? "VERIFIED_ORIGINAL" : "UNCERTAIN", "UNKNOWN",
                operation.ClaimedFinishedAt, null, JsonSerializer.Serialize(effect.Value, Json), version, clock.GetUtcNow());
            db.Add(result); await db.SaveChangesAsync(token); await currentGuard(token); return result;
        }, cancellationToken);

    private static string? PredecessorVersion(OfflineOperationData operation,
        OfflineOperationData[] signedOperations, Dictionary<Guid, OfflineOperationResult> predecessors)
    {
        var byOrigin = signedOperations.ToDictionary(row => row.OriginId);
        var visited = new HashSet<Guid>();
        var matching = new HashSet<Guid>();
        void Visit(Guid originId)
        {
            if (!visited.Add(originId)) return;
            var ancestor = byOrigin[originId];
            if (operation.Repair is not null
                ? ancestor.Repair?.ResourceId == operation.Repair.ResourceId
                : ancestor.Repair is null && ancestor.TaskId == operation.TaskId)
            {
                matching.Add(originId);
            }
            foreach (var dependency in ancestor.Dependencies) Visit(dependency);
        }
        foreach (var dependency in operation.Dependencies) Visit(dependency);
        bool AncestorOf(Guid possibleAncestor, Guid descendant)
        {
            var seen = new HashSet<Guid>();
            var pending = new Stack<Guid>(byOrigin[descendant].Dependencies);
            while (pending.Count > 0)
            {
                var candidate = pending.Pop();
                if (candidate == possibleAncestor) return true;
                if (seen.Add(candidate))
                    foreach (var parent in byOrigin[candidate].Dependencies) pending.Push(parent);
            }
            return false;
        }
        var nearest = matching.Where(candidate => !matching.Any(other =>
            other != candidate && AncestorOf(candidate, other))).ToArray();
        if (nearest.Length > 1)
            throw new OfflineAdmissionRejectedException(409, "dependency_version_ambiguous");
        return nearest.Length == 0 ? null : predecessors[nearest[0]].ResourceVersion;
    }

    private async Task<OfflineOperationResult> RetainFailureAsync(OfflineWorkflowCommand command, Guid sourceRegistration,
        OfflineSyncBatch batch, OfflineOperationAdmission admission, OfflineOperationData operation, string state, string code,
        Func<CancellationToken, Task> currentGuard, OfflinePackageImportData? imported,
        OfflineWorkflowAlgorithms algorithms, OfflineSignedBatchData input, CancellationToken cancellationToken)
        => await OwnTransactionAsync(async token =>
        {
            await currentGuard(token);
            if (imported is null) await GuardSyncSourceAsync(command, sourceRegistration, token);
            else await GuardImportTransportAsync(command, algorithms, input, imported, token);
            var result = OfflineOperationResult.Record(Guid.NewGuid(), command.ProjectId, batch.Id, admission.Id, operation.OriginId,
                null, state, code, "UNCERTAIN", "UNKNOWN", operation.ClaimedFinishedAt, null, "{}", null, clock.GetUtcNow());
            db.Add(result); await db.SaveChangesAsync(token); return result;
        }, cancellationToken);

    private async Task<OfflineDeviceRegistration> GuardSyncSourceAsync(OfflineWorkflowCommand command, Guid sourceRegistration,
        CancellationToken cancellationToken)
    {
        var source = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={sourceRegistration} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (source is null || source.ActorId != command.ActorId || source.RoleSnapshot != command.Role ||
            await db.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={sourceRegistration}")
                .AsNoTracking().AnyAsync(cancellationToken)) throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        return source;
    }

    private async Task GuardSyncSnapshotAsync(OfflineWorkflowCommand command, OfflineDeviceRegistration source,
        OfflineOperationData operation, CancellationToken cancellationToken, bool handover = false)
    {
        var snapshot = await db.Set<OfflineTaskSnapshot>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineTaskSnapshots] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={operation.SnapshotId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (snapshot is null || snapshot.DeviceRegistrationId != source.Id || snapshot.OriginalActorId != source.ActorId ||
            operation.OriginalActorId != source.ActorId || !handover && source.ActorId != command.ActorId ||
            snapshot.TaskId != operation.TaskId || snapshot.AssignmentId != operation.AssignmentId ||
            snapshot.TaskVersion != operation.TaskVersion || operation.SourceDeviceId != source.DeviceId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var assignment = await db.FieldInspectionAssignments.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={operation.AssignmentId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (assignment is null || assignment.FieldInspectionTaskId != operation.TaskId || assignment.AssignedToUserId != source.ActorId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
    }

    private async Task<T> OwnTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
        => await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try { var value = await work(cancellationToken); await transaction.CommitAsync(cancellationToken); return value; }
            catch { db.ChangeTracker.Clear(); throw; }
        });

    private static string? FieldAction(OfflineOperationData operation)
        => operation.Kind switch { "FIELD_ACCEPT" => "accept", "FIELD_START" => "start", "FIELD_SUBMISSION" => "submit", _ => null };

    private static object? FieldBody(OfflineOperationData operation)
        => operation.Kind switch { "FIELD_ACCEPT" => operation.FieldAction, "FIELD_START" => operation.FieldStart, _ => operation.FieldSubmission };

    private static OfflineItemFact ItemView(OfflineOperationResult result, string kind, Guid canonicalId)
        => new(result.OriginId, kind, result.State, result.Code, result.EffectId, canonicalId == Guid.Empty ? null : canonicalId, result.DurableAck, result.RecordedAt,
            result.TimeProvenance, result.SyncLateness, result.ClaimedFinishedAt, result.VerifiedFinishedAt,
            result.VerifiedFinishedAt?.AddHours(24), JsonSerializer.Deserialize<JsonElement>(result.OutcomeJson, Json));
}
