using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Offline;

namespace RoadGuardSystem.Repositories.Implementations.Offline;

public sealed partial class OfflineWorkflowRepository
{
    private async Task<OfflineWorkflowFact> ExecuteTransferAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, Func<CancellationToken, Task> guard, CancellationToken cancellationToken)
    {
        if (command.Action == "import")
            return await ImportAsync(command, algorithms, guard, cancellationToken);
        if (command.Action is "package-get" or "grant-get" or "artifact-get" or "batch-get" or "origin-get")
            return await OwnTransactionAsync(async token =>
            {
                await guard(token);
                return await ReadTransferAsync(command, algorithms, token);
            }, cancellationToken);
        if (command.Action == "start-reconcile")
            return await ReconcileStartAsync(command, algorithms, guard, cancellationToken);
        if (db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Offline transfer receipts own their transaction.");
        var operation = "h5.offline." + command.Action + ".v1";
        var fingerprint = Digest(JsonSerializer.Serialize(new
        {
            command.ProjectId,
            command.Action,
            command.ResourceId,
            command.Input
        }, Json));
        var receipt = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId,
            command.ProjectId, operation, command.Key!, fingerprint, async token =>
            {
                await guard(token);
                if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == command.ActorId &&
                    row.ProjectId == command.ProjectId && row.Operation == operation && row.IdempotencyKey == command.Key, token))
                    throw new CommittedReceipt();
                if (!await db.Projects.AsNoTracking().AnyAsync(row => row.Id == command.ProjectId &&
                    row.Status == ProjectStatus.Active, token))
                    throw new OfflineAdmissionRejectedException(409, "project_not_active");
                var result = command.Action switch
                {
                    "package-export" or "package-prepare" => await RegisterPackageAsync(command, algorithms, token),
                    "grant-issue" => await IssueGrantAsync(command, token),
                    "grant-revoke" => await RevokeGrantAsync(command, token),
                    "artifact-register" => await RegisterArtifactAsync(command, token),
                    _ => throw new OfflineAdmissionRejectedException(400, "validation_error")
                };
                await db.SaveChangesAsync(token);
                return (result.Id, JsonSerializer.Serialize(result.Value, Json));
            }, cancellationToken, receiptAccessGuard: async token =>
            {
                await guard(token);
                await GuardTransferReceiptAsync(command, token);
            });
        if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
        return new(receipt.Status == IdempotencyOperationStatus.Replayed ? 200 : 201,
            Value: JsonSerializer.Deserialize<JsonElement>(receipt.OutcomeJson, Json),
            Replayed: receipt.Status == IdempotencyOperationStatus.Replayed);
    }

    private async Task<OfflineWorkflowFact> ImportAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, Func<CancellationToken, Task> guard, CancellationToken cancellationToken)
    {
        if (command.Input is not OfflinePackageImportData imported || imported.Operations is null ||
            imported.BatchId == Guid.Empty || imported.PackageId == Guid.Empty || imported.GrantId == Guid.Empty ||
            imported.RecipientDeviceRegistrationId == Guid.Empty)
            throw new ArgumentException("A bounded recipient import is required.");
        var sourceBatch = await OwnTransactionAsync(async token =>
        {
            await guard(token);
            var package = await db.Set<OfflineEncryptedPackageRecord>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == imported.PackageId && row.ProjectId == command.ProjectId, token)
                ?? throw new OfflineAdmissionRejectedException(404, "not_found");
            return new OfflineSignedBatchData(package.SourceBatchId, package.SourceDeviceRegistrationId,
                imported.Operations, imported.SourceSignature);
        }, cancellationToken);
        return await ProcessBatchAsync(command, algorithms, guard, sourceBatch, imported, cancellationToken);
    }

    private async Task<OfflineDeviceRegistration> GuardImportTransportAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, OfflineSignedBatchData input, OfflinePackageImportData imported,
        CancellationToken token)
    {
        if (command.Role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var package = await db.Set<OfflineEncryptedPackageRecord>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={imported.PackageId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token)
            ?? throw new OfflineAdmissionRejectedException(404, "not_found");
        var grant = await db.Set<OfflineHandoverGrant>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrants] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={imported.GrantId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (grant.PackageId != package.Id || grant.SourceActorId != package.OriginalActorId ||
            grant.SourceDeviceRegistrationId != package.SourceDeviceRegistrationId ||
            grant.ManifestHash != package.ManifestHash || grant.RecipientActorId != command.ActorId ||
            grant.RecipientRole != command.Role || grant.RecipientDeviceRegistrationId != imported.RecipientDeviceRegistrationId ||
            input.BatchId != package.SourceBatchId || input.SourceDeviceRegistrationId != package.SourceDeviceRegistrationId ||
            input.Signature != package.SourceSignature || imported.SourceSignature != package.SourceSignature)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (await db.Set<OfflineHandoverGrantRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrantRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [GrantId]={grant.Id}")
            .AsNoTracking().AnyAsync(token)) throw new OfflineAdmissionRejectedException(403, "handover_grant_revoked");
        if (clock.GetUtcNow() >= grant.ExpiresAt && !await db.Set<OfflineSyncBatch>().AsNoTracking().AnyAsync(row =>
            row.Id == imported.BatchId && row.GrantId == grant.Id && row.ProjectId == command.ProjectId, token))
            throw new OfflineAdmissionRejectedException(403, "handover_grant_expired");
        var recipient = await DeviceForTransferAsync(command.ProjectId, imported.RecipientDeviceRegistrationId, token);
        if (recipient.ActorId != command.ActorId || recipient.RoleSnapshot != command.Role)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var source = await DeviceForTransferAsync(command.ProjectId, package.SourceDeviceRegistrationId, token,
            allowRevokedSource: true);
        if (source.ActorId != package.OriginalActorId ||
            await db.Set<OfflineDeviceRevocation>().AsNoTracking().AnyAsync(row =>
                row.DeviceRegistrationId == source.Id && row.RevokedAt <= package.RegisteredAt, token))
            throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        var descriptors = input.Operations.Select(algorithms.Describe).ToArray();
        var signedManifest = Encoding.UTF8.GetString(algorithms.CanonicalManifest(command.ProjectId,
            input.BatchId, source.Id, descriptors));
        if (signedManifest != package.SignedManifestJson || Digest(signedManifest) != package.ManifestHash)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        OfflinePackageAuthentication.VerifyClaim(Encoding.UTF8.GetBytes(signedManifest),
            package.SourceSignature, source.SigningPublicKey);
        var attached = algorithms.CanonicalAttachedPayload(input.BatchId, source.Id, input.Operations,
            input.Signature);
        var cipher = JsonSerializer.Deserialize<OfflineEncryptedPackage>(package.CipherPackageJson, Json)
            ?? throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
        OfflinePackageAuthentication.VerifyAttachedPayload(cipher, attached, source.SigningPublicKey);
        if (cipher.Header.PackageId != package.Id || cipher.Header.ProjectId != command.ProjectId ||
            cipher.Header.OriginalActorId != source.ActorId || cipher.Header.SourceDeviceId != source.DeviceId.ToString("D") ||
            OfflineCryptoFormat.PackageContentFingerprint(cipher) != package.PackageFingerprint ||
            Digest(Encoding.UTF8.GetString(attached)) != package.PayloadHash)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var recipientKeyHash = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(recipient.EncryptionPublicKey))).ToLowerInvariant();
        if (!cipher.Recipients.Any(wrap => wrap.ActorId == recipient.ActorId &&
            wrap.DeviceId == recipient.DeviceId.ToString("D") && wrap.RecipientKeyFingerprint == recipientKeyHash))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        OfflinePackageAuthentication.VerifyClaim(OfflineRecipientEndorsement.CanonicalClaim(new(
            command.ProjectId, package.Id, grant.Id, imported.BatchId, recipient.Id,
            package.PayloadHash)), imported.RecipientSignature, recipient.SigningPublicKey);
        using var scopeDocument = JsonDocument.Parse(grant.ScopeJson);
        var scoped = scopeDocument.RootElement.GetProperty("items").EnumerateArray().ToArray();
        if (descriptors.Length != scoped.Length || descriptors.Any(descriptor => scoped.Count(item =>
            item.GetProperty("originId").GetGuid() == descriptor.OriginId &&
            item.GetProperty("kind").GetString() == descriptor.Kind &&
            item.GetProperty("corePayloadHash").GetString() == descriptor.CorePayloadHash &&
            item.GetProperty("envelopeHash").GetString() == descriptor.EnvelopeHash &&
            item.GetProperty("taskId").GetGuid() == descriptor.TaskId &&
            item.GetProperty("assignmentId").GetGuid() == descriptor.AssignmentId &&
            item.GetProperty("snapshotId").GetGuid() == descriptor.SnapshotId &&
            (item.GetProperty("repairResourceId").ValueKind == JsonValueKind.Null
                ? (Guid?)null : item.GetProperty("repairResourceId").GetGuid()) == descriptor.RepairResourceId) != 1))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        return source;
    }

    private async Task<(Guid Id, object Value)> RegisterPackageAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, CancellationToken token)
    {
        if (command.Input is not OfflinePackageExportData input || input.Manifest is null ||
            input.Package is null || input.Manifest.Items is null ||
            input.SourceFileId.HasValue || command.Action == "package-export" && command.Role != UserRoleCode.RepairCrew ||
            command.Action == "package-prepare" && command.Role != UserRoleCode.Supervisor)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var source = await DeviceForTransferAsync(command.ProjectId, input.Manifest.SourceDeviceRegistrationId, token);
        if (command.Action == "package-export" && (source.ActorId != command.ActorId || source.RoleSnapshot != command.Role))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var cipher = input.Package;
        var canonical = algorithms.CanonicalManifest(command.ProjectId, input.Manifest.SourceBatchId,
            source.Id, input.Manifest.Items);
        if (input.Manifest.Items.Any(item => item is null || item.EffectId != item.OriginId) ||
            cipher.Header.PackageId == Guid.Empty || cipher.Header.ProjectId != command.ProjectId ||
            cipher.Header.OriginalActorId != source.ActorId || cipher.Header.SourceDeviceId != source.DeviceId.ToString("D") ||
            !cipher.Header.OriginIds.Order().SequenceEqual(input.Manifest.Items.Select(item => item.OriginId).Order()))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        foreach (var item in input.Manifest.Items)
        {
            var snapshot = await db.Set<OfflineTaskSnapshot>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineTaskSnapshots] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item.SnapshotId} AND [ProjectId]={command.ProjectId}")
                .AsNoTracking().SingleOrDefaultAsync(token);
            if (snapshot is null || snapshot.DeviceRegistrationId != source.Id ||
                snapshot.OriginalActorId != source.ActorId || snapshot.TaskId != item.TaskId ||
                snapshot.AssignmentId != item.AssignmentId)
                throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        }
        OfflinePackageAuthentication.VerifyClaim(canonical, input.Manifest.Signature, source.SigningPublicKey);
        OfflinePackageAuthentication.VerifyEncryptedSignature(cipher, source.SigningPublicKey);
        var existing = await db.Set<OfflineEncryptedPackageRecord>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={cipher.Header.PackageId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (existing is not null) throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var packageJson = input.ExactCipherPackageJson;
        var manifestJson = Encoding.UTF8.GetString(canonical);
        var fingerprint = OfflineCryptoFormat.PackageContentFingerprint(cipher);
        var row = command.Action == "package-prepare"
            ? OfflineEncryptedPackageRecord.RegisterPrepared(cipher.Header.PackageId, command.ProjectId,
                source.ActorId, source.Id, input.Manifest.SourceBatchId, command.ActorId, command.Role,
                packageJson, manifestJson, input.Manifest.Signature, cipher.Header.PayloadSha256,
                fingerprint, clock.GetUtcNow())
            : OfflineEncryptedPackageRecord.Capture(cipher.Header.PackageId, command.ProjectId,
                source.ActorId, source.Id, input.Manifest.SourceBatchId, packageJson, manifestJson,
                input.Manifest.Signature, cipher.Header.PayloadSha256, fingerprint, clock.GetUtcNow());
        db.Add(row);
        TransferAudit(command, row.Id, "offline_package_registered", "OfflineEncryptedPackage", row.RegisteredAt);
        return (row.Id, new
        {
            row.Id,
            row.ProjectId,
            row.SourceBatchId,
            row.SourceDeviceRegistrationId,
            row.ManifestHash,
            row.PayloadHash,
            row.PackageFingerprint,
            row.RegistrationMode
        });
    }

    private async Task<(Guid Id, object Value)> IssueGrantAsync(OfflineWorkflowCommand command, CancellationToken token)
    {
        if (command.Role != UserRoleCode.Supervisor || command.Input is not OfflineHandoverGrantData input ||
            input.Items is null || input.Items.Length is < 1 or > 1000 || input.Items.Any(item => item is null) ||
            input.Items.Select(item => item.OriginId).Distinct().Count() != input.Items.Length)
            throw new OfflineAdmissionRejectedException(400, "validation_error");
        var package = await db.Set<OfflineEncryptedPackageRecord>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.PackageId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token)
            ?? throw new OfflineAdmissionRejectedException(404, "not_found");
        var source = await DeviceForTransferAsync(command.ProjectId, package.SourceDeviceRegistrationId, token,
            allowRevokedSource: true);
        if (source.ActorId != package.OriginalActorId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var recipient = await DeviceForTransferAsync(command.ProjectId, input.RecipientDeviceRegistrationId, token);
        var cipher = JsonSerializer.Deserialize<OfflineEncryptedPackage>(package.CipherPackageJson, Json)
            ?? throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
        OfflinePackageAuthentication.VerifyEncryptedSignature(cipher, source.SigningPublicKey);
        var keyFingerprint = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(recipient.EncryptionPublicKey))).ToLowerInvariant();
        if (!cipher.Recipients.Any(wrap => wrap.ActorId == recipient.ActorId &&
            wrap.DeviceId == recipient.DeviceId.ToString("D") && wrap.RecipientKeyFingerprint == keyFingerprint))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        using var document = JsonDocument.Parse(package.SignedManifestJson);
        var signed = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        if (input.Items.Length != signed.Length || input.Items.Any(scope => signed.Count(item =>
            item.GetProperty("originId").GetGuid() == scope.OriginId &&
            item.GetProperty("kind").GetString() == scope.Kind &&
            item.GetProperty("corePayloadHash").GetString() == scope.CorePayloadHash &&
            item.GetProperty("envelopeHash").GetString() == scope.EnvelopeHash &&
            item.GetProperty("taskId").GetGuid() == scope.TaskId &&
            item.GetProperty("assignmentId").GetGuid() == scope.AssignmentId &&
            item.GetProperty("snapshotId").GetGuid() == scope.SnapshotId &&
            (item.GetProperty("repairResourceId").ValueKind == JsonValueKind.Null
                ? (Guid?)null : item.GetProperty("repairResourceId").GetGuid()) == scope.RepairResourceId) != 1))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var scopeJson = JsonSerializer.Serialize(new { items = input.Items }, Json);
        var grant = OfflineHandoverGrant.Issue(Guid.NewGuid(), command.ProjectId, package.Id,
            package.OriginalActorId, source.Id, recipient.ActorId, recipient.Id, recipient.RoleSnapshot,
            command.ActorId, command.Role, clock.GetUtcNow(), package.ManifestHash, scopeJson, input.Reason);
        db.AddRange(grant, DeadlineClock.Create(Guid.NewGuid(), command.ProjectId,
            DeadlineClockKind.DeviceHandover, grant.Id, grant.Id, grant.IssuedAt));
        TransferAudit(command, grant.Id, "offline_handover_grant_issued", "OfflineHandoverGrant", grant.IssuedAt);
        return (grant.Id, GrantView(grant, null));
    }

    private async Task<(Guid Id, object Value)> RevokeGrantAsync(OfflineWorkflowCommand command, CancellationToken token)
    {
        if (command.Role != UserRoleCode.Supervisor || command.ResourceId is not Guid grantId ||
            command.Input is not OfflineGrantRevokeData input)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var grant = await db.Set<OfflineHandoverGrant>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrants] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={grantId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token) ?? throw new OfflineAdmissionRejectedException(404, "not_found");
        if (await db.Set<OfflineHandoverGrantRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrantRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [GrantId]={grantId}")
            .AsNoTracking().AnyAsync(token)) throw new OfflineAdmissionRejectedException(409, "handover_grant_revoked");
        var revocation = OfflineHandoverGrantRevocation.Record(Guid.NewGuid(), command.ProjectId, grant.Id,
            command.ActorId, input.Reason, clock.GetUtcNow());
        db.Add(revocation);
        TransferAudit(command, revocation.Id, "offline_handover_grant_revoked", "OfflineHandoverGrant", revocation.RevokedAt);
        return (revocation.Id, GrantView(grant, revocation));
    }

    private async Task<(Guid Id, object Value)> RegisterArtifactAsync(OfflineWorkflowCommand command,
        CancellationToken token)
    {
        if (command.Input is not OfflineCaptureArtifactData input || input.Manifest is null ||
            input.Envelope is null || input.Manifest.Chunks is null)
            throw new OfflineAdmissionRejectedException(400, "validation_error");
        var package = await db.Set<OfflineEncryptedPackageRecord>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.Manifest.ParentPackageId} AND [ProjectId]={command.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token)
            ?? throw new OfflineAdmissionRejectedException(404, "not_found");
        var source = await DeviceForTransferAsync(command.ProjectId, package.SourceDeviceRegistrationId, token,
            allowRevokedSource: true);
        if (source.ActorId != package.OriginalActorId ||
            await db.Set<OfflineDeviceRevocation>().AsNoTracking().AnyAsync(row =>
                row.DeviceRegistrationId == source.Id && row.RevokedAt <= package.RegisteredAt, token))
            throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        if (command.Role != UserRoleCode.Supervisor &&
            (source.ActorId != command.ActorId || source.RoleSnapshot != command.Role ||
            await db.Set<OfflineDeviceRevocation>().AsNoTracking().AnyAsync(row =>
                row.DeviceRegistrationId == source.Id, token)))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var artifact = OfflineEncryptedCaptureArtifact.Register(package, source, input.Manifest,
            input.ManifestSignature, input.ChunkIndex, input.Envelope, command.ActorId,
            clock.GetUtcNow());
        if (await db.Set<OfflineEncryptedCaptureArtifact>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedCaptureArtifacts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={artifact.Id}")
            .AsNoTracking().AnyAsync(token)) throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        db.Add(artifact);
        TransferAudit(command, artifact.Id, "offline_capture_artifact_registered",
            "OfflineEncryptedCaptureArtifact", artifact.RegisteredAt);
        return (artifact.Id, new
        {
            artifact.Id,
            artifact.ParentPackageId,
            artifact.CaptureOriginId,
            artifact.TaskId,
            artifact.ChunkIndex,
            artifact.EnvelopeFingerprint,
            artifact.ManifestHash
        });
    }

    private async Task<OfflineWorkflowFact> ReadTransferAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, CancellationToken token)
    {
        if (command.ResourceId is not Guid id || id == Guid.Empty)
            throw new OfflineAdmissionRejectedException(400, "validation_error");
        if (command.Action == "grant-get")
        {
            var grant = await db.Set<OfflineHandoverGrant>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == id && row.ProjectId == command.ProjectId, token);
            if (grant is null) return new(404, "not_found");
            if (command.Role != UserRoleCode.Supervisor &&
                (grant.RecipientActorId != command.ActorId || grant.RecipientRole != command.Role ||
                 await db.Set<OfflineDeviceRevocation>().AsNoTracking().AnyAsync(row =>
                     row.DeviceRegistrationId == grant.RecipientDeviceRegistrationId, token)))
                return new(403, "access_forbidden");
            var revoked = await db.Set<OfflineHandoverGrantRevocation>().AsNoTracking()
                .SingleOrDefaultAsync(row => row.GrantId == id, token);
            return new(200, Value: GrantView(grant, revoked));
        }
        if (command.Action == "package-get")
        {
            var package = await db.Set<OfflineEncryptedPackageRecord>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == id && row.ProjectId == command.ProjectId, token);
            if (package is null) return new(404, "not_found");
            if (package.OriginalActorId != command.ActorId && command.Role != UserRoleCode.Supervisor &&
                !await HasCurrentRecipientGrantAsync(command, id, token))
                return new(403, "access_forbidden");
            return new(200, Value: new
            {
                package.Id,
                package.ProjectId,
                package.OriginalActorId,
                package.SourceDeviceRegistrationId,
                package.SourceBatchId,
                package.ManifestHash,
                package.PayloadHash,
                package.CipherPackageJson,
                package.SignedManifestJson,
                package.SourceSignature
            });
        }
        if (command.Action == "artifact-get")
        {
            var artifact = await db.Set<OfflineEncryptedCaptureArtifact>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == id && row.ProjectId == command.ProjectId, token);
            if (artifact is null) return new(404, "not_found");
            if (command.Role != UserRoleCode.Supervisor && artifact.OriginalActorId != command.ActorId)
            {
                if (!await HasCurrentRecipientGrantAsync(command, artifact.ParentPackageId, token))
                    return new(403, "access_forbidden");
            }
            return new(200, Value: new
            {
                artifact.Id,
                artifact.ProjectId,
                artifact.ParentPackageId,
                artifact.CaptureOriginId,
                artifact.TaskId,
                artifact.OriginalActorId,
                artifact.SourceDeviceRegistrationId,
                artifact.ChunkIndex,
                artifact.ChunkOffset,
                artifact.ChunkLength,
                artifact.PlaintextChecksum,
                artifact.EnvelopeFingerprint,
                artifact.ManifestHash,
                artifact.SignedManifestJson,
                artifact.ManifestSignature,
                artifact.CipherEnvelopeJson
            });
        }
        if (command.Action == "batch-get")
        {
            var batch = await db.Set<OfflineSyncBatch>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineSyncBatches] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [ProjectId]={command.ProjectId}")
                .AsNoTracking().SingleOrDefaultAsync(token);
            if (batch is null) return new(404, "not_found");
            var signed = await GuardBatchStatusAsync(command, algorithms, batch, token);
            var admissions = await db.Set<OfflineOperationAdmission>().AsNoTracking()
                .Where(row => row.BatchId == batch.Id).ToArrayAsync(token);
            var results = await db.Set<OfflineOperationResult>().AsNoTracking()
                .Where(row => row.BatchId == batch.Id).OrderByDescending(row => row.RecordedAt)
                .ThenByDescending(row => row.Id).ToArrayAsync(token);
            var current = results.GroupBy(row => row.OriginId).ToDictionary(group => group.Key,
                group => group.OrderByDescending(row => row.DurableAck).ThenByDescending(row => row.RecordedAt)
                    .ThenByDescending(row => row.Id).First());
            var originIds = signed.Operations.Select(row => row.OriginId).ToArray();
            var canonical = await db.Set<RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin>()
                .AsNoTracking().Where(row => row.ProjectId == command.ProjectId && originIds.Contains(row.OriginId))
                .ToDictionaryAsync(row => row.OriginId, row => row.Id, token);
            var items = signed.Operations.Select(operation => current.TryGetValue(operation.OriginId, out var result)
                ? ItemView(result, operation.Kind, result.DurableAck
                    ? canonical.GetValueOrDefault(operation.OriginId) : Guid.Empty)
                : new OfflineItemFact(operation.OriginId, operation.Kind, "PENDING_DEPENDENCY", "not_processed",
                    null, null, false, batch.ReceivedAt, "UNCERTAIN", "UNKNOWN",
                    operation.ClaimedFinishedAt, null, null, null)).ToArray();
            if (admissions.Length != signed.Operations.Length)
                throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
            return new(200, Value: new OfflineBatchFact(batch.Id, items));
        }
        if (command.Action == "origin-get")
        {
            var binding = await db.Set<OfflineOperationBinding>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.ProjectId == command.ProjectId && row.OriginId == id, token);
            if (binding is null) return new(404, "not_found");
            var admissions = await db.Set<OfflineOperationAdmission>().AsNoTracking()
                .Where(row => row.BindingId == binding.Id && row.CurrentImporterId == command.ActorId &&
                    row.ImporterRole == command.Role).OrderByDescending(row => row.AdmittedAt).ToArrayAsync(token);
            if (admissions.Length == 0) return new(403, "access_forbidden");
            var admission = admissions[0];
            var batch = await db.Set<OfflineSyncBatch>().AsNoTracking().SingleAsync(row =>
                row.Id == admission.BatchId && row.ProjectId == command.ProjectId, token);
            await GuardBatchStatusAsync(command, algorithms, batch, token);
            var result = await db.Set<OfflineOperationResult>().AsNoTracking()
                .Where(row => row.AdmissionId == admission.Id).OrderByDescending(row => row.DurableAck)
                .ThenByDescending(row => row.RecordedAt)
                .ThenByDescending(row => row.Id).FirstOrDefaultAsync(token);
            var canonical = result?.DurableAck == true
                ? await db.Set<RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin>()
                    .AsNoTracking().Where(row => row.ProjectId == command.ProjectId && row.OriginId == id)
                    .Select(row => row.Id).SingleOrDefaultAsync(token)
                : Guid.Empty;
            return new(200, Value: result is null
                ? new OfflineItemFact(id, binding.Kind, "PENDING_DEPENDENCY", "not_processed", null,
                    null, false, batch.ReceivedAt, "UNCERTAIN", "UNKNOWN", null, null, null, null)
                : ItemView(result, binding.Kind, canonical));
        }
        return new(409, "offline_admission_not_ready");
    }

    private Task<bool> HasCurrentRecipientGrantAsync(OfflineWorkflowCommand command, Guid packageId,
        CancellationToken token)
    {
        var now = clock.GetUtcNow();
        return db.Set<OfflineHandoverGrant>().AsNoTracking().AnyAsync(grant =>
            grant.ProjectId == command.ProjectId && grant.PackageId == packageId &&
            grant.RecipientActorId == command.ActorId && grant.RecipientRole == command.Role &&
            grant.ExpiresAt > now &&
            !db.Set<OfflineHandoverGrantRevocation>().Any(row => row.GrantId == grant.Id) &&
            !db.Set<OfflineDeviceRevocation>().Any(row =>
                row.DeviceRegistrationId == grant.RecipientDeviceRegistrationId), token);
    }

    private async Task<OfflineSignedBatchData> GuardBatchStatusAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, OfflineSyncBatch batch, CancellationToken token)
    {
        if (batch.CurrentImporterId != command.ActorId || batch.AttachedPayloadJson is null ||
            batch.AttachedPayloadHash != Digest(batch.AttachedPayloadJson))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var signed = JsonSerializer.Deserialize<OfflineSignedBatchData>(batch.AttachedPayloadJson, Json)
            ?? throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
        if (signed.BatchId != batch.SourceBatchId || signed.SourceDeviceRegistrationId != batch.SourceDeviceRegistrationId ||
            signed.Signature != batch.SourceSignature || signed.Operations is null ||
            signed.Operations.Length is < 1 or > 1000)
            throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
        if (batch.GrantId is null)
            await GuardSyncSourceAsync(command, batch.SourceDeviceRegistrationId, token);
        else
        {
            if (batch.PackageId is null || batch.RecipientDeviceRegistrationId is null ||
                batch.RecipientSignature is null)
                throw new OfflineAdmissionRejectedException(409, "offline_admission_proof_unavailable");
            await GuardImportTransportAsync(command, algorithms, signed,
                new(batch.Id, batch.PackageId.Value, batch.GrantId.Value,
                    batch.RecipientDeviceRegistrationId.Value, signed.Operations, batch.SourceSignature,
                    batch.RecipientSignature), token);
        }
        return signed;
    }

    private async Task<OfflineWorkflowFact> ReconcileStartAsync(OfflineWorkflowCommand command,
        OfflineWorkflowAlgorithms algorithms, Func<CancellationToken, Task> guard, CancellationToken cancellationToken)
    {
        if (command.Input is not OfflineStartReconcileData input || input.BatchId == Guid.Empty ||
            input.OriginId == Guid.Empty)
            throw new OfflineAdmissionRejectedException(400, "validation_error");
        var signed = await OwnTransactionAsync(async token =>
        {
            await guard(token);
            var batch = await db.Set<OfflineSyncBatch>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineSyncBatches] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.BatchId} AND [ProjectId]={command.ProjectId}")
                .AsNoTracking().SingleOrDefaultAsync(token)
                ?? throw new OfflineAdmissionRejectedException(404, "not_found");
            var source = await GuardBatchStatusAsync(command, algorithms, batch, token);
            if (source.Operations.Length != 1 || source.Operations[0].Kind != "FIELD_START" ||
                source.Operations[0].OriginId != input.OriginId)
                throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
            return (batch, source);
        }, cancellationToken);
        if (signed.batch.GrantId is null)
            return await ProcessBatchAsync(command, algorithms, guard, signed.source, null, cancellationToken);
        return await ProcessBatchAsync(command, algorithms, guard, signed.source,
            new(signed.batch.Id, signed.batch.PackageId!.Value, signed.batch.GrantId.Value,
                signed.batch.RecipientDeviceRegistrationId!.Value, signed.source.Operations,
                signed.batch.SourceSignature, signed.batch.RecipientSignature!), cancellationToken);
    }

    private async Task GuardTransferReceiptAsync(OfflineWorkflowCommand command, CancellationToken token)
    {
        if (command.Action is "package-export" or "package-prepare")
        {
            if (command.Input is not OfflinePackageExportData input) throw new OfflineAdmissionRejectedException(400, "validation_error");
            var source = await DeviceForTransferAsync(command.ProjectId, input.Manifest.SourceDeviceRegistrationId, token);
            if (command.Action == "package-export" && source.ActorId != command.ActorId)
                throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        }
        if ((command.Action is "grant-issue" or "grant-revoke") && command.Role != UserRoleCode.Supervisor)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
    }

    private async Task<OfflineDeviceRegistration> DeviceForTransferAsync(Guid projectId, Guid registrationId,
        CancellationToken token, bool allowRevokedSource = false)
    {
        var device = await db.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={registrationId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(token) ?? throw new OfflineAdmissionRejectedException(404, "not_found");
        if (!allowRevokedSource && await db.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={device.Id}")
            .AsNoTracking().AnyAsync(token)) throw new OfflineAdmissionRejectedException(403, "offline_key_revoked");
        return device;
    }

    private void TransferAudit(OfflineWorkflowCommand command, Guid eventId, string action,
        string sourceKind, DateTimeOffset at)
    {
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, at, action,
            sourceKind, eventId, null, JsonSerializer.Serialize(new { command.ProjectId, eventId }, Json),
            null, "h5.offline.v1", eventId, ["projectId", "eventId"]));
        db.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), action.Replace('_', '.') + ".v1", at,
            eventId, JsonSerializer.Serialize(new
            {
                projectId = command.ProjectId,
                sourceKind,
                sourceId = eventId,
                originEventId = eventId,
                occurredAtUtc = at
            }, Json)));
    }

    private static object GrantView(OfflineHandoverGrant row, OfflineHandoverGrantRevocation? revoked)
        => new
        {
            row.Id,
            row.ProjectId,
            row.PackageId,
            row.SourceActorId,
            row.SourceDeviceRegistrationId,
            row.RecipientActorId,
            row.RecipientDeviceRegistrationId,
            role = row.RecipientRole.ToString(),
            row.IssuedBy,
            row.IssuedAt,
            row.ExpiresAt,
            row.ManifestHash,
            row.ScopeJson,
            revokedAt = revoked?.RevokedAt
        };
}
