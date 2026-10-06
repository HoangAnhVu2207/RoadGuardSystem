using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Offline;

// Separate from OfflineWorkflowRepository to avoid a FIELD -> offline workflow -> FIELD DI cycle.
public sealed class OfflineAdmissionValidator : IOfflineFieldAdmissionValidator,
    IOfflineEvidenceAdmissionValidator, IOfflineUploadAdmissionValidator, IOfflineRepairAdmissionValidator
{
    private readonly RoadGuardDbContext _context;
    private readonly TimeProvider _clock;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public OfflineAdmissionValidator(RoadGuardDbContext context, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(clock);
        _context = context;
        _clock = clock;
    }
    public Task<OfflineFieldAdmissionFacts?> ValidateAsync(FieldWorkflowCommand command, CancellationToken cancellationToken)
        => ValidateFieldCoreAsync(command, false, cancellationToken);

    private Task<OfflineFieldAdmissionFacts?> ValidateFieldCoreAsync(FieldWorkflowCommand command,
        bool committedCaptureContinuation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var kind = command.Action switch
        {
            "accept" => "FIELD_ACCEPT", "start" => "FIELD_START", "submit" => "FIELD_SUBMISSION", _ => null
        };
        var hash = Digest(JsonSerializer.Serialize(new
        {
            id = command.TaskId, input = command.Input, command.Admission.OriginalActorId
        }, Json));
        return ValidateCoreAsync(command.ProjectId, command.TaskId, command.Admission, kind, hash,
            null, null, committedCaptureContinuation, cancellationToken);
    }

    public Task<OfflineFieldAdmissionFacts?> ValidateRepairAsync(OfflineOperationData operation, Guid projectId,
        FieldAdmissionContext admission, CancellationToken cancellationToken)
        => ValidateRepairCoreAsync(operation, projectId, admission, false, cancellationToken);

    private Task<OfflineFieldAdmissionFacts?> ValidateRepairCoreAsync(OfflineOperationData operation, Guid projectId,
        FieldAdmissionContext admission, bool committedCaptureContinuation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(admission);
        var repair = operation.Repair;
        if (repair is null || repair.ResourceId == Guid.Empty)
            throw new OfflineAdmissionRejectedException(400, "validation_error");
        var hash = operation.Kind switch
        {
            "REPAIR_ASSESSMENT" when repair.Action == "assessment" && repair.Assessment is not null &&
                repair.Start is null && repair.Finish is null => RepairCommandCoreHash.Assessment(operation.TaskId,
                    repair.ResourceId, operation.OriginalActorId, repair.Assessment),
            "REPAIR_EXECUTION_START" when repair.Action == "execution-start" && repair.Start is not null &&
                repair.Assessment is null && repair.Finish is null => RepairCommandCoreHash.ExecutionStart(operation.TaskId,
                    repair.ResourceId, operation.OriginalActorId, repair.Start),
            "REPAIR_EXECUTION_FINISH" when repair.Action == "execution-finish" && repair.Finish is not null &&
                repair.Assessment is null && repair.Start is null => RepairCommandCoreHash.ExecutionFinish(operation.TaskId,
                    repair.ResourceId, operation.OriginalActorId, repair.Finish),
            _ => throw new OfflineAdmissionRejectedException(400, "validation_error")
        };
        return ValidateCoreAsync(projectId, operation.TaskId, admission, operation.Kind, hash,
            repair.ResourceId, operation, committedCaptureContinuation, cancellationToken);
    }

    private async Task<OfflineFieldAdmissionFacts?> ValidateCoreAsync(Guid projectId, Guid? taskId,
        FieldAdmissionContext caller, string? expectedKind, string coreHash, Guid? repairResourceId,
        OfflineOperationData? assertedOperation, bool committedCaptureContinuation,
        CancellationToken cancellationToken)
    {
        RequireTransaction();
        if (caller.CallerId == Guid.Empty || projectId == Guid.Empty ||
            caller.CallerRole is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        await Anh02ReceiptAuthority.LockAsync(_context, caller.CallerId, projectId, cancellationToken);
        if (!await new AnhHuyFactsRepository(_context).IsCurrentActorAsync(caller.CallerId, caller.CallerRole, cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        if (!await _context.ProjectMembers.AsNoTracking().AnyAsync(member => member.ProjectId == projectId &&
            member.UserId == caller.CallerId && member.RoleCode == caller.CallerRole &&
            member.Status == ProjectMemberStatus.Active && member.ValidFrom <= today &&
            (member.ValidTo == null || member.ValidTo >= today), cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (caller.Mode is not ("SYNC" or "HANDOVER") || caller.OfflineAdmissionId is null ||
            caller.OfflineAdmissionId == Guid.Empty || taskId is null || taskId == Guid.Empty)
            throw new OfflineAdmissionRejectedException(403, "offline_admission_required");
        var admission = await _context.Set<OfflineOperationAdmission>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={caller.OfflineAdmissionId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (admission is null || admission.CurrentImporterId != caller.CallerId || admission.ImporterRole != caller.CallerRole ||
            admission.GrantId != caller.HandoverGrantId || (caller.Mode == "HANDOVER") != admission.GrantId.HasValue)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var binding = await _context.Set<OfflineOperationBinding>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={admission.BindingId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var batch = await _context.Set<OfflineSyncBatch>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineSyncBatches] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={admission.BatchId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (binding is null || batch is null || binding.TaskId != taskId || binding.OriginalActorId != caller.OriginalActorId ||
            batch.CurrentImporterId != caller.CallerId || batch.SourceDeviceRegistrationId != binding.SourceDeviceRegistrationId ||
            batch.GrantId != admission.GrantId || batch.AttachedPayloadJson is null || batch.AttachedPayloadHash is null)
            throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        var source = await _context.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.SourceDeviceRegistrationId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var snapshot = await _context.Set<OfflineTaskSnapshot>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineTaskSnapshots] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.SnapshotId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (source is null || snapshot is null || source.ActorId != binding.OriginalActorId || snapshot.OriginalActorId != binding.OriginalActorId ||
            snapshot.TaskId != binding.TaskId || snapshot.AssignmentId != binding.AssignmentId || snapshot.DeviceRegistrationId != source.Id)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var sourceRevocation = await _context.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={source.Id} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var operation = JsonSerializer.Deserialize<OfflineOperationData>(binding.EnvelopeJson, Json)
            ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        if (expectedKind is null || expectedKind != binding.Kind || operation.Kind != binding.Kind || operation.SchemaVersion != 1 ||
            operation.OriginId != binding.OriginId || operation.EffectId != binding.EffectId || operation.TaskId != binding.TaskId ||
            operation.AssignmentId != binding.AssignmentId || operation.OriginalActorId != binding.OriginalActorId ||
            operation.SourceDeviceId != source.DeviceId || operation.SnapshotId != snapshot.Id || operation.TaskVersion != snapshot.TaskVersion ||
            operation.CorePayloadHash != binding.CorePayloadHash || coreHash != binding.CorePayloadHash ||
            binding.RepairResourceId != repairResourceId || operation.Repair?.ResourceId != repairResourceId ||
            assertedOperation is not null && Digest(JsonSerializer.Serialize(assertedOperation, Json)) != binding.EnvelopeHash ||
            Digest(binding.EnvelopeJson) != binding.EnvelopeHash)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        OfflinePackageAuthentication.VerifyClaim(Encoding.UTF8.GetBytes(batch.SignedDescriptorJson), batch.SourceSignature, source.SigningPublicKey);
        if (Digest(batch.AttachedPayloadJson) != batch.AttachedPayloadHash || Digest(batch.SignedDescriptorJson) != batch.ContentHash)
            throw new CryptographicException("Retained offline payload integrity does not match.");
        var attached = JsonSerializer.Deserialize<OfflineSignedBatchData>(batch.AttachedPayloadJson, Json)
            ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        if (attached.BatchId != batch.SourceBatchId || attached.SourceDeviceRegistrationId != source.Id || attached.Signature != batch.SourceSignature ||
            attached.Operations is null || attached.Operations.Length is < 1 or > 1000 ||
            attached.Operations.Count(item => item is not null && item.OriginId == binding.OriginId &&
                Digest(JsonSerializer.Serialize(item, Json)) == binding.EnvelopeHash) != 1)
            throw new CryptographicException("Attached operation differs from its retained signed binding.");
        using var manifestDocument = JsonDocument.Parse(batch.SignedDescriptorJson);
        var manifest = manifestDocument.RootElement;
        if (manifest.GetProperty("projectId").GetGuid() != projectId || manifest.GetProperty("sourceBatchId").GetGuid() != batch.SourceBatchId ||
            manifest.GetProperty("sourceDeviceRegistrationId").GetGuid() != source.Id ||
            manifest.GetProperty("items").EnumerateArray().Count(item => item.GetProperty("originId").GetGuid() == binding.OriginId &&
                item.GetProperty("effectId").GetGuid() == binding.EffectId && item.GetProperty("kind").GetString() == binding.Kind &&
                item.GetProperty("corePayloadHash").GetString() == binding.CorePayloadHash && item.GetProperty("envelopeHash").GetString() == binding.EnvelopeHash &&
                item.GetProperty("taskId").GetGuid() == binding.TaskId && item.GetProperty("assignmentId").GetGuid() == binding.AssignmentId &&
                item.GetProperty("snapshotId").GetGuid() == binding.SnapshotId &&
                (item.GetProperty("repairResourceId").ValueKind == JsonValueKind.Null
                    ? (Guid?)null : item.GetProperty("repairResourceId").GetGuid()) == operation.Repair?.ResourceId) != 1)
            throw new CryptographicException("Signed manifest differs from its admitted operation.");
        var assignment = await _context.FieldInspectionAssignments.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.AssignmentId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var task = await _context.FieldInspectionTasks.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.TaskId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (assignment is null || task is null || assignment.FieldInspectionTaskId != task.Id || assignment.AssignedToUserId != binding.OriginalActorId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (assignment.Status != FieldInspectionAssignmentStatus.Active || assignment.EndedAt is not null)
            throw new OfflineAdmissionRejectedException(409, "stale_snapshot");
        if (admission.GrantId is null)
        {
            if (caller.OriginalActorId != caller.CallerId || source.RoleSnapshot != caller.CallerRole || sourceRevocation is not null)
                throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        }
        else
            await GuardHandoverAsync(admission, binding, batch, source, sourceRevocation,
                committedCaptureContinuation, cancellationToken);
        var originalStart = await _context.Set<FieldTaskStartOrigin>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.ProjectId == projectId && row.OriginId == binding.OriginId && row.Id == binding.EffectId &&
            row.ContentHash == binding.CorePayloadHash && row.OriginalActorId == binding.OriginalActorId, cancellationToken);
        var verifiedAt = originalStart?.VerifiedOriginalAt;
        return new(admission.Id, binding.EffectId, projectId, binding.TaskId, binding.AssignmentId,
            binding.OriginalActorId, source.DeviceId, caller.CallerId, caller.CallerRole, admission.GrantId,
            binding.Kind, binding.CorePayloadHash, snapshot.Id, verifiedAt, verifiedAt.HasValue ? "VERIFIED_ORIGINAL" : "UNCERTAIN");
    }
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private async Task GuardHandoverAsync(OfflineOperationAdmission admission, OfflineOperationBinding binding,
        OfflineSyncBatch batch, OfflineDeviceRegistration source, OfflineDeviceRevocation? sourceRevocation,
        bool committedCaptureContinuation, CancellationToken cancellationToken)
    {
        var grant = await _context.Set<OfflineHandoverGrant>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrants] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={admission.GrantId} AND [ProjectId]={admission.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (grant is null || grant.RecipientActorId != admission.CurrentImporterId || grant.RecipientRole != admission.ImporterRole ||
            grant.PackageId != batch.PackageId || grant.SourceActorId != binding.OriginalActorId || grant.SourceDeviceRegistrationId != source.Id ||
            grant.RecipientDeviceRegistrationId != batch.RecipientDeviceRegistrationId || batch.RecipientSignature is null)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (await _context.Set<OfflineHandoverGrantRevocation>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrantRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [GrantId]={grant.Id} AND [ProjectId]={grant.ProjectId}")
            .AsNoTracking().AnyAsync(cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "handover_grant_revoked");
        var recipient = await _context.Set<OfflineDeviceRegistration>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineDeviceRegistrations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={grant.RecipientDeviceRegistrationId} AND [ProjectId]={grant.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (recipient is null || recipient.ActorId != admission.CurrentImporterId || recipient.RoleSnapshot != admission.ImporterRole ||
            await _context.Set<OfflineDeviceRevocation>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineDeviceRevocations] WITH (UPDLOCK,HOLDLOCK) WHERE [DeviceRegistrationId]={recipient.Id} AND [ProjectId]={grant.ProjectId}")
                .AsNoTracking().AnyAsync(cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var package = await _context.Set<OfflineEncryptedPackageRecord>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEncryptedPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={grant.PackageId} AND [ProjectId]={grant.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (package is null || package.SourceDeviceRegistrationId != source.Id || package.OriginalActorId != binding.OriginalActorId ||
            package.SourceBatchId != batch.SourceBatchId || package.ManifestHash != grant.ManifestHash ||
            package.SourceSignature != batch.SourceSignature || package.PayloadHash != batch.AttachedPayloadHash ||
            sourceRevocation is not null && package.RegisteredAt >= sourceRevocation.RevokedAt)
            throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        var cipher = JsonSerializer.Deserialize<OfflineEncryptedPackage>(package.CipherPackageJson, Json)
            ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        var encrypted = cipher;
        _ = OfflinePackageAuthentication.VerifyAttachedPayload(encrypted, Encoding.UTF8.GetBytes(batch.AttachedPayloadJson!), source.SigningPublicKey);
        var recipientKeyHash = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(recipient.EncryptionPublicKey))).ToLowerInvariant();
        if (!encrypted.Recipients.Any(wrap => wrap.ActorId == recipient.ActorId && wrap.DeviceId == recipient.DeviceId.ToString("D") &&
            wrap.RecipientKeyFingerprint == recipientKeyHash) || encrypted.Header.ProjectId != grant.ProjectId ||
            encrypted.Header.OriginalActorId != binding.OriginalActorId || encrypted.Header.SourceDeviceId != source.DeviceId.ToString("D") ||
            encrypted.Header.PackageId != package.Id || OfflineCryptoFormat.PackageContentFingerprint(encrypted) != package.PackageFingerprint)
            throw new CryptographicException("Encrypted recipient scope differs from the retained registration.");
        OfflinePackageAuthentication.VerifyClaim(OfflineRecipientEndorsement.CanonicalClaim(new(grant.ProjectId,
            package.Id, grant.Id, batch.Id, recipient.Id, batch.AttachedPayloadHash!)), batch.RecipientSignature, recipient.SigningPublicKey);
        using var scopeDocument = JsonDocument.Parse(grant.ScopeJson);
        if (scopeDocument.RootElement.GetProperty("items").EnumerateArray().Count(item =>
            item.GetProperty("originId").GetGuid() == binding.OriginId && item.GetProperty("kind").GetString() == binding.Kind &&
            item.GetProperty("corePayloadHash").GetString() == binding.CorePayloadHash && item.GetProperty("envelopeHash").GetString() == binding.EnvelopeHash &&
            item.GetProperty("taskId").GetGuid() == binding.TaskId && item.GetProperty("assignmentId").GetGuid() == binding.AssignmentId &&
            item.GetProperty("snapshotId").GetGuid() == binding.SnapshotId &&
            (item.GetProperty("repairResourceId").ValueKind == JsonValueKind.Null
                ? (Guid?)null : item.GetProperty("repairResourceId").GetGuid()) == binding.RepairResourceId) != 1)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        if (_clock.GetUtcNow() >= grant.ExpiresAt && !committedCaptureContinuation &&
            !await HasDurableEffectAsync(binding, cancellationToken))
            throw new OfflineAdmissionRejectedException(403, "handover_grant_expired");
    }

    private async Task<bool> HasDurableEffectAsync(OfflineOperationBinding binding, CancellationToken cancellationToken)
    {
        if (!await _context.Set<FieldInspectionOperationOrigin>().AsNoTracking().AnyAsync(row => row.ProjectId == binding.ProjectId &&
            row.OriginId == binding.OriginId && row.Kind == binding.Kind && row.EffectId == binding.EffectId &&
            row.ContentHash == binding.CorePayloadHash && row.TaskId == binding.TaskId && row.OriginalActorId == binding.OriginalActorId, cancellationToken))
            return false;
        return binding.Kind switch
        {
            "FIELD_START" => await _context.Set<FieldTaskStartOrigin>().AsNoTracking().AnyAsync(row => row.Id == binding.EffectId &&
                row.ProjectId == binding.ProjectId && row.OriginId == binding.OriginId && row.TaskId == binding.TaskId &&
                row.AssignmentId == binding.AssignmentId && row.ContentHash == binding.CorePayloadHash && row.OriginalActorId == binding.OriginalActorId, cancellationToken),
            "FIELD_SUBMISSION" => await _context.Set<FieldInspectionSubmission>().AsNoTracking().AnyAsync(row => row.Id == binding.EffectId &&
                row.ProjectId == binding.ProjectId && row.OriginId == binding.OriginId && row.TaskId == binding.TaskId &&
                row.AssignmentId == binding.AssignmentId && row.ContentHash == binding.CorePayloadHash && row.OriginalActorId == binding.OriginalActorId, cancellationToken),
            "FIELD_ACCEPT" => await _context.Set<FieldInspectionTaskEvent>().AsNoTracking().AnyAsync(row => row.Id == binding.EffectId &&
                row.ProjectId == binding.ProjectId && row.TaskId == binding.TaskId && row.AssignmentId == binding.AssignmentId &&
                row.Kind == "ACCEPTED" && row.ActorId == binding.OriginalActorId, cancellationToken),
            "REPAIR_ASSESSMENT" => await _context.Set<RepairMeasurementAssessment>().AsNoTracking().AnyAsync(row =>
                row.Id == binding.EffectId && row.ProjectId == binding.ProjectId && row.ItemId == binding.RepairResourceId &&
                row.TaskId == binding.TaskId && row.OriginId == binding.OriginId &&
                row.ContentHash == binding.CorePayloadHash && row.OriginalActorId == binding.OriginalActorId,
                cancellationToken),
            "REPAIR_EXECUTION_START" => await _context.Set<RepairExecutionStart>().AsNoTracking().AnyAsync(row =>
                row.Id == binding.EffectId && row.ProjectId == binding.ProjectId && row.ItemId == binding.RepairResourceId &&
                row.OriginId == binding.OriginId && row.ContentHash == binding.CorePayloadHash &&
                row.OriginalActorId == binding.OriginalActorId, cancellationToken),
            "REPAIR_EXECUTION_FINISH" => await _context.Set<RepairExecutionFinish>().AsNoTracking().AnyAsync(row =>
                row.Id == binding.EffectId && row.ProjectId == binding.ProjectId && row.ItemId == binding.RepairResourceId &&
                row.OriginId == binding.OriginId && row.ContentHash == binding.CorePayloadHash &&
                row.OriginalActorId == binding.OriginalActorId, cancellationToken),
            _ => false
        };
    }
    public async Task GuardOriginBindingAsync(Guid projectId, Guid originId, string kind, string corePayloadHash,
        Guid originalActorId, Guid taskId, CancellationToken cancellationToken)
    {
        RequireTransaction();
        if (new[] { projectId, originId, originalActorId, taskId }.Any(id => id == Guid.Empty) ||
            corePayloadHash is not { Length: 64 } || corePayloadHash.Any(value => !char.IsAsciiHexDigit(value)))
            throw new ArgumentException("An exact bounded canonical identity is required.");
        var hash = corePayloadHash.ToLowerInvariant();
        var canonical = await _context.Set<FieldInspectionOperationOrigin>().FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionOperationOrigins] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={projectId} AND [OriginId]={originId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (canonical is not null && (canonical.Kind != kind || canonical.ContentHash != hash ||
            canonical.OriginalActorId != originalActorId || canonical.TaskId != taskId))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var bound = await _context.Set<OfflineOperationBinding>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={projectId} AND [OriginId]={originId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (bound is not null && (bound.Kind != kind || bound.CorePayloadHash != hash ||
            bound.OriginalActorId != originalActorId || bound.TaskId != taskId))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
    }
    public async Task<OfflineEvidenceAdmissionFacts?> ResolveDeclaredEvidenceAsync(Guid projectId, Guid taskId,
        Guid currentOfflineAdmissionId, Guid originalActorId, RepairFieldEvidenceData declaration,
        CancellationToken cancellationToken)
    {
        RequireTransaction(); ArgumentNullException.ThrowIfNull(declaration);
        var admission = await _context.Set<OfflineOperationAdmission>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={currentOfflineAdmissionId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var binding = await _context.Set<OfflineOperationBinding>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={admission.BindingId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var operation = JsonSerializer.Deserialize<OfflineOperationData>(binding.EnvelopeJson, Json)
            ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        if (binding.TaskId != taskId || binding.OriginalActorId != originalActorId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var caller = new FieldAdmissionContext(admission.CurrentImporterId, admission.ImporterRole,
            originalActorId, admission.GrantId.HasValue ? "HANDOVER" : "SYNC", false,
            admission.GrantId, admission.Id);
        var facts = binding.Kind switch
        {
            "FIELD_SUBMISSION" when operation.FieldSubmission is not null =>
                await ValidateAsync(new(projectId, taskId, "submit", operation.FieldSubmission, null,
                    operation.TaskVersion, caller), cancellationToken),
            "REPAIR_ASSESSMENT" when operation.Repair?.Assessment is not null =>
                await ValidateRepairAsync(operation, projectId, caller, cancellationToken),
            _ => throw new OfflineAdmissionRejectedException(403, "access_forbidden")
        };
        if (facts is null) throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        var declared = binding.Kind == "FIELD_SUBMISSION"
            ? operation.FieldSubmission!.Evidence : operation.Repair!.Assessment!.Evidence;
        var matched = (declared ?? []).Where(item => item is not null && item.CaptureOriginId == declaration.CaptureOriginId).ToArray();
        if (matched.Length != 1 || JsonSerializer.Serialize(matched[0], Json) != JsonSerializer.Serialize(declaration, Json))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var uploadCapture = await _context.Set<OfflineEvidenceCaptureReference>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEvidenceCaptureReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={projectId} AND [TaskId]={taskId} AND [CaptureOriginId]={declaration.CaptureOriginId}")
            .AsNoTracking().ToArrayAsync(cancellationToken);
        var capture = uploadCapture.SingleOrDefault(row => row.OriginalActorId == originalActorId &&
            row.Checksum == declaration.ChecksumSha256 && row.Purpose == declaration.Purpose && row.MediaType == declaration.MediaType);
        var fileId = declaration.FileId ?? capture?.FileId;
        if (!fileId.HasValue) return null;
        var file = await _context.Files.FromSqlInterpolated(
            $"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId.Value}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var scope = await _context.FileScopes.FromSqlInterpolated(
            $"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId.Value}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var upload = await _context.UploadSessions.FromSqlInterpolated(
            $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId.Value}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (file is null || scope is null || upload is null) throw new OfflineAdmissionRejectedException(404, "not_found");
        var approvedBeforeReuse = false;
        if (declaration.Purpose == "BEFORE")
        {
            var decision = await _context.Set<FieldInspectionEvidenceReuseDecision>().AsNoTracking()
                .Where(row => row.ProjectId == projectId && row.TaskId == taskId && row.FileId == file.Id &&
                    row.FileChecksum == file.Checksum && row.SourceKind == "REPORTER")
                .OrderByDescending(row => row.OccurredAt).FirstOrDefaultAsync(cancellationToken);
            if (decision is not null && scope.ProjectId is null && scope.Purpose == "REPORT_PHOTO")
            {
                var reportIds = await _context.Set<HuyDefectSourceLink>().AsNoTracking()
                    .Where(row => row.ProjectId == projectId && row.EndedAt == null &&
                        _context.FieldInspectionTasks.Any(task => task.Id == taskId && task.DefectId == row.DefectId))
                    .Select(row => row.ReportSourceId).ToArrayAsync(cancellationToken);
                var original = await _context.Reports.AsNoTracking().Where(row => reportIds.Contains(row.Id))
                    .SelectMany(row => row.OriginalEvidence)
                    .SingleOrDefaultAsync(row => row.Id == decision.SourceEvidenceId && row.FileId == file.Id, cancellationToken);
                var supplement = original is null
                    ? await _context.ReportSupplements.AsNoTracking().Where(row => reportIds.Contains(row.ReportId))
                        .SelectMany(row => row.Evidence)
                        .SingleOrDefaultAsync(row => row.Id == decision.SourceEvidenceId && row.FileId == file.Id, cancellationToken)
                    : null;
                var evidence = original ?? supplement;
                var currentFile = await new AnhHuyFactsRepository(_context).GetFileAsync(file.Id, cancellationToken);
                using var provenance = JsonDocument.Parse(decision.ProvenanceJson);
                approvedBeforeReuse = evidence is not null && currentFile is not null &&
                    evidence.OwnerUserId == scope.OwnerUserId && evidence.FileVersion == currentFile.FileVersion &&
                    currentFile.State == "VERIFIED" && currentFile.Checksum == decision.FileChecksum &&
                    provenance.RootElement.TryGetProperty("fileVersion", out var pinnedVersion) &&
                    pinnedVersion.GetString() == currentFile.FileVersion;
            }
        }
        if (!approvedBeforeReuse && (scope.ProjectId != projectId || scope.TargetId != taskId || scope.Purpose != declaration.Purpose ||
            capture is null && (scope.OwnerUserId != originalActorId || file.UploadedByUserId != originalActorId)))
            throw new OfflineAdmissionRejectedException(403, "evidence_access_forbidden");
        if (file.Checksum != declaration.ChecksumSha256 || file.MimeType != declaration.MediaType ||
            upload.ExpectedChecksumSha256 != declaration.ChecksumSha256)
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        if (upload.Status != UploadSessionStatus.Verified) return null;
        if (file.UploadedByUserId is not Guid uploadedBy) throw new OfflineAdmissionRejectedException(409, "source_not_ready");
        var retained = await _context.Set<OfflineAdmittedFileReference>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineAdmittedFileReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [AdmissionId]={admission.Id} AND [CaptureOriginId]={declaration.CaptureOriginId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (retained is not null && (retained.FileId != file.Id || retained.ContentChecksum != file.Checksum ||
            retained.ActualFileOwnerId != scope.OwnerUserId || retained.ActualUploadedById != uploadedBy))
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        if (retained is null)
        {
            var provenance = JsonSerializer.Serialize(new { file.UploadedAt, file.UploadedByUserId, scope.OwnerUserId,
                file.Checksum, file.MimeType, file.SizeBytes, uploadSessionId = upload.Id,
                uploadVersion = Convert.ToBase64String(upload.RowVersion), declaration.CapturedAt,
                declaration.AttemptChecklist, source = "SIGNED_DECLARATION_VERIFIED_FILE_BYTES", timeProvenance = "CLAIMED" }, Json);
            retained = OfflineAdmittedFileReference.Capture(Guid.NewGuid(), projectId, taskId, admission.Id, binding.Id,
                declaration.CaptureOriginId, file.Id, originalActorId, admission.CurrentImporterId, scope.OwnerUserId,
                uploadedBy, declaration.Purpose, declaration.ChecksumSha256, file.Checksum, provenance, _clock.GetUtcNow());
            _context.Add(retained);
        }
        return new(retained.Id, admission.Id, originalActorId, uploadedBy, admission.GrantId, file.Id, upload.Id,
            file.Checksum, declaration.Purpose, file.MimeType, retained.ReferencedAt);
    }
    public async Task<OfflineEvidenceAdmissionFacts?> ValidateFieldEvidenceAsync(Guid projectId, Guid taskId,
        Guid originalActorId, Guid captureOriginId, Guid fileId, string checksum, string purpose,
        string mediaType, CancellationToken cancellationToken)
    {
        RequireTransaction();
        var capture = await _context.Set<OfflineEvidenceCaptureReference>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEvidenceCaptureReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={projectId} AND [TaskId]={taskId} AND [CaptureOriginId]={captureOriginId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (capture is null) return null;
        if (capture.OriginalActorId != originalActorId || capture.FileId != fileId ||
            capture.Checksum != checksum || capture.Purpose != purpose || capture.MediaType != mediaType)
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        return await GuardExistingFileAsync(capture.ActualUploaderId, fileId, false, cancellationToken);
    }
    public async Task<OfflineEvidenceAdmissionFacts?> GuardExistingFileAsync(Guid currentActorId, Guid fileId,
        bool technicalContinuation, CancellationToken cancellationToken)
    {
        RequireTransaction();
        var capture = await _context.Set<OfflineEvidenceCaptureReference>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEvidenceCaptureReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (capture is null) return null;
        if (capture.ActualUploaderId != currentActorId || technicalContinuation)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var admission = await _context.Set<OfflineOperationAdmission>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={capture.AdmissionId} AND [ProjectId]={capture.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (admission is null || admission.CurrentImporterId != currentActorId || admission.GrantId != capture.GrantId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        await GuardCaptureAdmissionAsync(admission, capture.TaskId, capture.CaptureOriginId, currentActorId,
            admission.ImporterRole, capture.Checksum, capture.Purpose, capture.MediaType, cancellationToken);
        var file = await _context.Files.FromSqlInterpolated(
            $"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var scope = await _context.FileScopes.FromSqlInterpolated(
            $"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var session = await _context.UploadSessions.FromSqlInterpolated(
            $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={capture.UploadSessionId} AND [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (file is null || scope is null || session is null || scope.ProjectId != capture.ProjectId ||
            scope.TargetId != capture.TaskId || scope.OwnerUserId != currentActorId ||
            scope.Purpose != capture.Purpose || file.UploadedByUserId != currentActorId ||
            file.Checksum != capture.Checksum || file.MimeType != capture.MediaType ||
            session.ExpectedChecksumSha256 != capture.Checksum)
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        return new(capture.Id, capture.AdmissionId, capture.OriginalActorId, capture.ActualUploaderId,
            capture.GrantId, fileId, session.Id, capture.Checksum, capture.Purpose,
            capture.MediaType, capture.AdmittedAt);
    }
    public async Task<OfflineUploadCaptureFacts> GuardNewUploadAsync(OfflineUploadCaptureRequest request,
        CancellationToken cancellationToken)
    {
        RequireTransaction();
        ArgumentNullException.ThrowIfNull(request);
        var upload = request.Upload;
        if (request.CurrentActorId == Guid.Empty || request.ProjectId == Guid.Empty || request.TaskId == Guid.Empty ||
            request.CaptureOriginId == Guid.Empty || upload.ActorUserId != request.CurrentActorId ||
            upload.ProjectId != request.ProjectId || upload.TargetId != request.TaskId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var admission = await _context.Set<OfflineOperationAdmission>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={request.AdmissionId} AND [ProjectId]={request.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        await GuardNewCaptureGrantAsync(admission, cancellationToken);
        var (binding, declaration) = await GuardCaptureAdmissionAsync(admission, request.TaskId,
            request.CaptureOriginId, request.CurrentActorId, request.CurrentRole, upload.ChecksumSha256,
            upload.Purpose, upload.MediaType, cancellationToken);
        if (declaration.FileId.HasValue ||
            await _context.Set<OfflineEvidenceCaptureReference>().FromSqlInterpolated(
                $"SELECT * FROM [OfflineEvidenceCaptureReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={request.ProjectId} AND [TaskId]={request.TaskId} AND [CaptureOriginId]={request.CaptureOriginId}")
                .AsNoTracking().AnyAsync(cancellationToken))
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        return new(request.ProjectId, request.TaskId, admission.Id, binding.Id, request.CaptureOriginId,
            binding.OriginalActorId, request.CurrentActorId, admission.GrantId, declaration.ChecksumSha256,
            declaration.Purpose, declaration.MediaType, declaration.CapturedAt,
            JsonSerializer.Serialize(new { declaration.AttemptChecklist, declaration.CapturedAt,
                source = "SIGNED_DECLARATION", timeProvenance = "CLAIMED" }, Json));
    }
    public async Task BindCreatedFileAsync(OfflineUploadCaptureFacts facts, Guid fileId, Guid uploadSessionId,
        DateTimeOffset admittedAt, CancellationToken cancellationToken)
    {
        RequireTransaction();
        ArgumentNullException.ThrowIfNull(facts);
        var file = await _context.Files.FromSqlInterpolated(
            $"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var scope = await _context.FileScopes.FromSqlInterpolated(
            $"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var session = await _context.UploadSessions.FromSqlInterpolated(
            $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={uploadSessionId} AND [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (file is null || scope is null || session is null || scope.ProjectId != facts.ProjectId ||
            scope.TargetId != facts.TaskId || scope.OwnerUserId != facts.ActualUploaderId ||
            file.UploadedByUserId != facts.ActualUploaderId || scope.Purpose != facts.Purpose ||
            file.Checksum != facts.Checksum || file.MimeType != facts.MediaType ||
            session.ExpectedChecksumSha256 != facts.Checksum)
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        var admission = await _context.Set<OfflineOperationAdmission>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == facts.AdmissionId && row.ProjectId == facts.ProjectId, cancellationToken)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        await GuardNewCaptureGrantAsync(admission, cancellationToken);
        var (binding, declaration) = await GuardCaptureAdmissionAsync(admission, facts.TaskId,
            facts.CaptureOriginId, facts.ActualUploaderId,
            admission.ImporterRole, facts.Checksum, facts.Purpose, facts.MediaType, cancellationToken);
        if (binding.Id != facts.BindingId || binding.OriginalActorId != facts.OriginalActorId ||
            declaration.CapturedAt != facts.DeclaredCapturedAt || admission.GrantId != facts.GrantId)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        _context.Add(OfflineEvidenceCaptureReference.Bind(Guid.NewGuid(), facts.ProjectId, facts.TaskId,
            facts.AdmissionId, facts.BindingId, facts.CaptureOriginId, facts.OriginalActorId,
            facts.ActualUploaderId, facts.GrantId, fileId, uploadSessionId, facts.Purpose, facts.Checksum,
            facts.MediaType, facts.DeclaredCapturedAt, facts.CaptureFactsJson, admittedAt));
    }

    public async Task GuardCommittedUploadAsync(OfflineUploadCaptureRequest request, Guid fileId,
        Guid uploadSessionId, CancellationToken cancellationToken)
    {
        RequireTransaction();
        ArgumentNullException.ThrowIfNull(request);
        var admission = await _context.Set<OfflineOperationAdmission>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationAdmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={request.AdmissionId} AND [ProjectId]={request.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var capture = await _context.Set<OfflineEvidenceCaptureReference>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineEvidenceCaptureReferences] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={request.ProjectId} AND [TaskId]={request.TaskId} AND [CaptureOriginId]={request.CaptureOriginId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (capture is null || capture.AdmissionId != admission.Id ||
            capture.ActualUploaderId != request.CurrentActorId || capture.GrantId != admission.GrantId ||
            capture.FileId != fileId || capture.UploadSessionId != uploadSessionId)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var file = await _context.Files.FromSqlInterpolated(
            $"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var scope = await _context.FileScopes.FromSqlInterpolated(
            $"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var session = await _context.UploadSessions.FromSqlInterpolated(
            $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={uploadSessionId} AND [FileId]={fileId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (file is null || scope is null || session is null || scope.ProjectId != request.ProjectId ||
            scope.TargetId != request.TaskId || scope.OwnerUserId != request.CurrentActorId ||
            scope.Purpose != request.Upload.Purpose ||
            file.UploadedByUserId != request.CurrentActorId || file.Checksum != request.Upload.ChecksumSha256 ||
            file.MimeType != request.Upload.MediaType || file.SizeBytes != request.Upload.SizeBytes ||
            session.OwnerUserId != request.CurrentActorId || session.Purpose != request.Upload.Purpose ||
            session.MediaType != request.Upload.MediaType ||
            session.ExpectedChecksumSha256 != request.Upload.ChecksumSha256 ||
            session.ExpectedSizeBytes != request.Upload.SizeBytes)
            throw new OfflineAdmissionRejectedException(409, "evidence_version_mismatch");
        await GuardCommittedCaptureGrantAsync(admission, capture, file, session, cancellationToken);
        var (binding, declaration) = await GuardCaptureAdmissionAsync(admission, request.TaskId,
            request.CaptureOriginId, request.CurrentActorId, request.CurrentRole,
            request.Upload.ChecksumSha256, request.Upload.Purpose, request.Upload.MediaType,
            cancellationToken, committedCaptureContinuation: true);
        if (capture.BindingId != binding.Id || capture.OriginalActorId != binding.OriginalActorId ||
            capture.Checksum != declaration.ChecksumSha256 || capture.Purpose != declaration.Purpose ||
            capture.MediaType != declaration.MediaType || capture.DeclaredCapturedAt != declaration.CapturedAt)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
    }

    private async Task GuardNewCaptureGrantAsync(OfflineOperationAdmission admission, CancellationToken token)
    {
        if (admission.GrantId is not Guid grantId) return;
        var grant = await _context.Set<OfflineHandoverGrant>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrants] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={grantId} AND [ProjectId]={admission.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (grant is null || !grant.AllowsNewAdmissionAt(_clock.GetUtcNow()) ||
            await _context.Set<OfflineHandoverGrantRevocation>().AsNoTracking().AnyAsync(row =>
                row.GrantId == grantId, token))
            throw new OfflineAdmissionRejectedException(403, "handover_grant_expired");
    }

    private async Task GuardCommittedCaptureGrantAsync(OfflineOperationAdmission admission,
        OfflineEvidenceCaptureReference capture, RoadGuardSystem.BusinessObjects.Files.StoredFile file,
        RoadGuardSystem.BusinessObjects.Files.UploadSession session, CancellationToken token)
    {
        if (admission.GrantId is not Guid grantId) return;
        var grant = await _context.Set<OfflineHandoverGrant>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineHandoverGrants] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={grantId} AND [ProjectId]={admission.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (grant is null || capture.AdmittedAt >= grant.ExpiresAt || file.UploadedAt >= grant.ExpiresAt ||
            _clock.GetUtcNow() >= session.ExpiresAt ||
            await _context.Set<OfflineHandoverGrantRevocation>().AsNoTracking().AnyAsync(row =>
                row.GrantId == grantId, token))
            throw new OfflineAdmissionRejectedException(403, "handover_grant_expired");
    }

    private async Task<(OfflineOperationBinding Binding, RepairFieldEvidenceData Declaration)> GuardCaptureAdmissionAsync(
        OfflineOperationAdmission admission, Guid taskId, Guid captureOriginId, Guid currentActorId,
        UserRoleCode currentRole, string checksum, string purpose, string mediaType, CancellationToken token,
        bool committedCaptureContinuation = false)
    {
        if (admission.CurrentImporterId != currentActorId || admission.ImporterRole != currentRole ||
            admission.ProjectId == Guid.Empty)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var binding = await _context.Set<OfflineOperationBinding>().FromSqlInterpolated(
            $"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={admission.BindingId} AND [ProjectId]={admission.ProjectId}")
            .AsNoTracking().SingleOrDefaultAsync(token)
            ?? throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var operation = JsonSerializer.Deserialize<OfflineOperationData>(binding.EnvelopeJson, Json)
            ?? throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        if (binding.TaskId != taskId)
            throw new OfflineAdmissionRejectedException(403, "access_forbidden");
        var declarations = (binding.Kind switch
        {
            "FIELD_SUBMISSION" => operation.FieldSubmission?.Evidence,
            "REPAIR_ASSESSMENT" => operation.Repair?.Assessment?.Evidence,
            _ => null
        })?.Where(item => item.CaptureOriginId == captureOriginId).ToArray();
        if (declarations is not { Length: 1 } || declarations[0].ChecksumSha256 != checksum ||
            declarations[0].Purpose != purpose || declarations[0].MediaType != mediaType)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var caller = new FieldAdmissionContext(currentActorId, currentRole, binding.OriginalActorId,
            admission.GrantId.HasValue ? "HANDOVER" : "SYNC", false, admission.GrantId, admission.Id);
        if (binding.Kind == "FIELD_SUBMISSION")
            await ValidateFieldCoreAsync(new(admission.ProjectId, taskId, "submit", operation.FieldSubmission, null,
                operation.TaskVersion, caller), committedCaptureContinuation, token);
        else
            await ValidateRepairCoreAsync(operation, admission.ProjectId, caller,
                committedCaptureContinuation, token);
        return (binding, declarations[0]);
    }
    private void RequireTransaction()
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Caller-owned offline transaction required.");
    }
}
