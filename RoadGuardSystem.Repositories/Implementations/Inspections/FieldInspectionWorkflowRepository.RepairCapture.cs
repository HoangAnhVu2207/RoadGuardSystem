using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Implementations.Offline;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    public async Task<ReporterFileFacts> ValidateRepairEvidenceInTransactionAsync(Guid taskId,
        RepairFieldEvidenceData declaration, Guid originalActor, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned repair transaction required.");
        try
        {
            var task = await TaskAsync(taskId, cancellationToken);
            var file = await EvidenceFileAsync(task, declaration, cancellationToken, originalActor);
            if (file.State != "VERIFIED") Deny(409, "evidence_pending");
            return file;
        }
        catch (Denied denial) { throw new RepairFieldCoreRejectedException(denial.Result.Status, denial.Result.Code); }
    }

    public async Task<RepairMeasurementAssessment> CaptureRepairAssessmentInTransactionAsync(
        RepairFieldTaskBinding binding, FieldTaskStartOrigin firstStart, Guid actor, Guid effectId,
        RepairMeasurementAssessmentData input, string contentHash, CancellationToken cancellationToken,
        Guid? offlineAdmissionId = null)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned repair transaction required.");
        try
        {
            var task = await TaskAsync(binding.TaskId, cancellationToken);
            if (task.RepairItemId != binding.ItemId || task.ProjectId != binding.ProjectId ||
                task.TaskMode is not ("NORMAL" or "CONDITIONAL_FT") || firstStart.TaskId != task.Id ||
                firstStart.AssignmentId != binding.AssignmentId || firstStart.OriginalActorId != actor ||
                task.Status is not (FieldInspectionTaskStatus.Accepted or FieldInspectionTaskStatus.InProgress))
                Deny(409, "repair_capture_source_conflict");
            if ((input.Evidence ?? []).Any(row => row is null || row.Purpose == "AFTER"))
                throw new ArgumentException("PRE_EXECUTION assessment cannot declare completed repair evidence.");
            if (input.StopConditions is { } stops && (stops.Count > 100 || stops.Keys.Any(code => string.IsNullOrWhiteSpace(code) || code.Length > 200)))
                throw new ArgumentException("Stop observations require bounded explicit policy codes.");
            var resolved = new Dictionary<Guid, OfflineEvidenceAdmissionFacts>();
            if (offlineAdmissionId is Guid admissionId)
            {
                var validator = new OfflineAdmissionValidator(db, clock);
                foreach (var declaration in input.Evidence ?? [])
                {
                    var actual = await validator.ResolveDeclaredEvidenceAsync(binding.ProjectId, task.Id,
                        admissionId, actor, declaration, cancellationToken);
                    if (actual is not null)
                    {
                        if (actual.AdmissionId != admissionId || actual.OriginalActorId != actor ||
                            !string.Equals(actual.Checksum, declaration.ChecksumSha256, StringComparison.OrdinalIgnoreCase) ||
                            actual.Purpose != declaration.Purpose || actual.MediaType != declaration.MediaType ||
                            declaration.FileId is Guid declared && declared != actual.FileId)
                            Deny(409, "evidence_version_mismatch");
                        resolved.Add(declaration.CaptureOriginId, actual);
                    }
                    else if (declaration.FileId is not null) Deny(403, "signed_evidence_admission_required");
                }
            }
            var now = clock.GetUtcNow();
            var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.PreMeasurement,
                task.Id, binding.ProjectId, task.RoadSectionVersionId, task.SurveyId, "REPAIR-PRE-" + Guid.NewGuid().ToString("N"),
                actor, "FIELD inspector", now, null, "PRE_EXECUTION assessment", FieldInspectionSessionStatus.Completed, null);
            var capture = new RepairFieldSubmissionData(input.OriginId, firstStart.Id, null, input.Measurements, input.Evidence,
                input.LocationProof, "MEASUREMENT", null, null, input.DeviceId);
            var measurements = ValidateMeasurements(task, capture, session.Id, actor, now);
            var missing = await ReadinessAsync(task, capture, firstStart, cancellationToken, actor, resolved);
            var evidence = new List<RepairAssessmentEvidenceReference>();
            foreach (var declaration in input.Evidence ?? [])
            {
                resolved.TryGetValue(declaration.CaptureOriginId, out var actual);
                var effectiveDeclaration = actual is null ? declaration : declaration with { FileId = actual.FileId };
                var facts = actual is null ? await CaptureEvidenceFactsAsync(task, declaration, cancellationToken)
                    : await CaptureResolvedEvidenceFactsAsync(task, LegacyRepairEvidence(declaration), actual, cancellationToken);
                string state = "UPLOAD_NOT_CREATED"; string? fileVersion = null; Guid? uploader = null;
                if (effectiveDeclaration.FileId is not null)
                {
                    var file = await EvidenceFileAsync(task, effectiveDeclaration, cancellationToken, actual?.ActualUploaderId ?? actor);
                    state = file.State; fileVersion = file.FileVersion; uploader = file.OwnerId;
                }
                var canonicalChecksum = declaration.ChecksumSha256.ToLowerInvariant();
                var reuse = effectiveDeclaration.FileId is Guid fileId && declaration.Purpose == "BEFORE"
                    ? await db.Set<FieldInspectionEvidenceReuseDecision>().Where(row => row.TaskId == task.Id && row.FileId == fileId &&
                        row.FileChecksum == canonicalChecksum).OrderByDescending(row => row.OccurredAt)
                        .Select(row => (Guid?)row.Id).FirstOrDefaultAsync(cancellationToken)
                    : null;
                evidence.Add(new(Guid.NewGuid(), declaration.CaptureOriginId, effectiveDeclaration.FileId, declaration.Purpose,
                    canonicalChecksum, declaration.MediaType, declaration.CapturedAt, state,
                    fileVersion, uploader, actor, reuse, facts));
            }
            db.Add(session); db.GroundTruthMeasurements.AddRange(measurements);
            return new RepairMeasurementAssessment(effectId, binding.ProjectId, binding.ItemId, binding.Id, task.Id,
                binding.AssignmentId, actor, firstStart.Id, session.Id, effectId, input.OriginId, contentHash,
                JsonSerializer.Serialize(input, Json), now, "PRE_EXECUTION", missing.Count == 0 ? "READY" : "INCOMPLETE",
                JsonSerializer.Serialize(missing, Json), JsonSerializer.Serialize(input.LocationProof, Json),
                missing.Any(reason => reason.StartsWith("LOCATION", StringComparison.Ordinal) || reason.StartsWith("GPS", StringComparison.Ordinal) ||
                    reason.StartsWith("POSITION", StringComparison.Ordinal)) ? "UNKNOWN" : "VERIFIED_CHECKLIST", null)
            {
                Measurements = measurements.Select(row => new RepairAssessmentMeasurementReference(row.Id)).ToArray(),
                Evidence = evidence
            };
        }
        catch (Denied denial) { throw new RepairFieldCoreRejectedException(denial.Result.Status, denial.Result.Code); }
    }
}
