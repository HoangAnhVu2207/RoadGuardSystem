using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Offline;

public sealed partial class OfflineWorkflowRepository
{
    private static RepairFieldEvidenceData[] CaptureDeclarations(OfflineOperationData operation)
        => operation.Kind == "FIELD_SUBMISSION" ? operation.FieldSubmission?.Evidence ?? []
            : operation.Kind == "REPAIR_ASSESSMENT" ? operation.Repair?.Assessment?.Evidence ?? [] : [];

    private async Task RequireUploadedEvidenceAsync(Guid project, OfflineOperationAdmission admission,
        OfflineOperationData operation, CancellationToken token)
    {
        var declarations = CaptureDeclarations(operation);
        if (operation.Kind == "FIELD_SUBMISSION" && declarations.Length == 0)
            throw new OfflineAdmissionRejectedException(409, "evidence_declaration_required");
        if (declarations.Length > 100 || declarations.Any(d => d is null || d.CaptureOriginId == Guid.Empty ||
                d.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT") || d.MediaType is not ("image/png" or "image/jpeg") ||
                d.ChecksumSha256 is not { Length: 64 } || d.ChecksumSha256.Any(c => !char.IsAsciiHexDigit(c)) || d.SizeBytes is <= 0) ||
            declarations.Select(d => d.CaptureOriginId).Distinct().Count() != declarations.Length)
            throw new OfflineAdmissionRejectedException(409, "evidence_declaration_invalid");
        var validator = fieldAdmission as IOfflineEvidenceAdmissionValidator ?? new OfflineAdmissionValidator(db, clock);
        foreach (var declaration in declarations)
        {
            if (declaration is null) throw new ArgumentException("Signed capture declaration is required.");
            var facts = await validator.ResolveDeclaredEvidenceAsync(project, operation.TaskId, admission.Id,
                operation.OriginalActorId, declaration, token);
            if (facts is null)
                throw new EvidencePending(declaration.FileId is null && !await db.Set<OfflineEvidenceCaptureReference>()
                    .AnyAsync(c => c.ProjectId == project && c.TaskId == operation.TaskId && c.CaptureOriginId == declaration.CaptureOriginId, token)
                    ? "evidence_upload_required" : "evidence_verification_pending");
        }
        // Persist verified transport references inside the same transaction before FIELD resolves
        // them again. No business effect/ack is committed until the enclosing transaction succeeds.
        await db.SaveChangesAsync(token);
    }

    // Runs only in an authorized batch/origin transaction, never as a global lookup by admission ID.
    private async Task<OfflineAdmissionReadbackFact> AdmissionViewAsync(OfflineSyncBatch batch,
        OfflineOperationAdmission admission, OfflineOperationData operation, OfflineOperationResult? result,
        CancellationToken token)
    {
        var caller = new RoadGuardSystem.Repositories.Inspections.FieldAdmissionContext(admission.CurrentImporterId,
            admission.ImporterRole, operation.OriginalActorId, admission.GrantId.HasValue ? "HANDOVER" : "SYNC", false,
            admission.GrantId, admission.Id);
        var validator = fieldAdmission ?? new OfflineAdmissionValidator(db, clock);
        if (operation.Repair is not null)
            await ((validator as IOfflineRepairAdmissionValidator) ?? new OfflineAdmissionValidator(db, clock))
                .ValidateRepairAsync(operation, batch.ProjectId, caller, token);
        else
            await validator.ValidateAsync(new(batch.ProjectId, operation.TaskId, FieldAction(operation)!,
                FieldBody(operation), null, operation.TaskVersion, caller), token);
        var captures = new List<OfflineCaptureReadbackFact>();
        foreach (var declaration in CaptureDeclarations(operation))
        {
            if (declaration is null) throw new OfflineAdmissionRejectedException(409, "evidence_declaration_invalid");
            var capture = await db.Set<OfflineEvidenceCaptureReference>().AsNoTracking().SingleOrDefaultAsync(c =>
                c.ProjectId == batch.ProjectId && c.TaskId == operation.TaskId && c.CaptureOriginId == declaration.CaptureOriginId &&
                c.OriginalActorId == operation.OriginalActorId && c.Checksum == declaration.ChecksumSha256 &&
                c.Purpose == declaration.Purpose && c.MediaType == declaration.MediaType, token);
            if (declaration is null) throw new OfflineAdmissionRejectedException(409, "evidence_declaration_invalid");
            Guid? id = capture?.FileId;
            if (id is null && declaration.FileId is not null)
            {
                var legacyScope = await db.FileScopes.AsNoTracking().SingleOrDefaultAsync(s =>
                    s.FileId == declaration.FileId && s.ProjectId == batch.ProjectId && s.TargetId == operation.TaskId &&
                    s.OwnerUserId == operation.OriginalActorId && s.Purpose == declaration.Purpose, token);
                if (legacyScope is not null)
                    id = await db.Files.AsNoTracking().Where(f => f.Id == legacyScope.FileId &&
                        f.UploadedByUserId == operation.OriginalActorId && f.Checksum == declaration.ChecksumSha256 &&
                        f.MimeType == declaration.MediaType && (declaration.SizeBytes == null || f.SizeBytes == declaration.SizeBytes))
                        .Select(f => (Guid?)f.Id).SingleOrDefaultAsync(token);
            }
            var file = id is Guid fileId ? await db.Files.AsNoTracking().SingleOrDefaultAsync(f => f.Id == fileId, token) : null;
            var upload = id is Guid uploadFileId ? await db.UploadSessions.AsNoTracking().SingleOrDefaultAsync(u => u.FileId == uploadFileId, token) : null;
            var state = upload?.Status.ToString().ToUpperInvariant() ?? "NOT_CREATED";
            captures.Add(new(declaration.CaptureOriginId, id, upload?.Id, state, declaration.Purpose,
                declaration.MediaType, declaration.ChecksumSha256, declaration.SizeBytes, file?.SizeBytes));
        }
        var status = result?.DurableAck == true ? "COMMITTED"
            : result?.State is "REJECTED" or "CONFLICT" or "STALE_SNAPSHOT" ? result.State
            : captures.Any(c => c.UploadStatus == "NOT_CREATED") ? "WAITING_UPLOAD"
            : captures.Any(c => c.UploadStatus == "FAILED") ? "UPLOAD_FAILED"
            : captures.Any(c => c.UploadStatus != "VERIFIED") ? "WAITING_VERIFICATION"
            : result?.State == "PENDING_DEPENDENCY" && result.Code == "dependency_not_committed" ? "WAITING_DEPENDENCY" : "READY";
        return new(admission.Id, batch.ProjectId, batch.Id, batch.SourceBatchId, batch.SourceDeviceRegistrationId,
            operation.OriginId, operation.TaskId, operation.AssignmentId, operation.SnapshotId,
            operation.OriginalActorId, admission.CurrentImporterId, admission.GrantId, status, captures.ToArray());
    }
}
