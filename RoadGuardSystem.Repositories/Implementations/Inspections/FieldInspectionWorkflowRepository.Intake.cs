using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // The retained FIELD core owns its original validation and serialization; adapt only the new repair seam.
    private static GroundTruthMeasurement[] ValidateMeasurements(FieldInspectionTask task, RepairFieldSubmissionData input,
        Guid session, Guid actor, DateTimeOffset now)
        => ValidateMeasurements(task, LegacyRepairSubmission(input), session, actor, now);
    private Task<List<string>> ReadinessAsync(FieldInspectionTask task, RepairFieldSubmissionData input,
        FieldTaskStartOrigin start, CancellationToken token, Guid? expectedOwner = null,
        Dictionary<Guid, OfflineEvidenceAdmissionFacts>? resolved = null)
        => ReadinessAsync(task, LegacyRepairSubmission(input), start, token, expectedOwner, resolved);
    private Task<ReporterFileFacts> EvidenceFileAsync(FieldInspectionTask task, RepairFieldEvidenceData declaration,
        CancellationToken token, Guid? expectedOwner = null)
        => EvidenceFileAsync(task, LegacyRepairEvidence(declaration), token, expectedOwner);
    private Task<string> CaptureEvidenceFactsAsync(FieldInspectionTask task, RepairFieldEvidenceData declaration,
        CancellationToken token) => CaptureEvidenceFactsAsync(task, LegacyRepairEvidence(declaration), token);
    private static FieldEvidenceDeclarationFact LegacyRepairEvidence(RepairFieldEvidenceData value)
        => new(value.CaptureOriginId, value.FileId, value.Purpose, value.ChecksumSha256,
            value.MediaType, value.CapturedAt, value.AttemptChecklist);
    private static FieldSubmissionInputFact LegacyRepairSubmission(RepairFieldSubmissionData value)
        => new(value.OriginId, value.StartOriginId, value.ParentSubmissionId,
            value.Measurements?.Select(row => row is null ? null! : new FieldMeasurementInputFact(row.SampleId,
                row.Type, row.Value, row.State, row.UnknownReason, row.Dimension, row.Unit, row.Longitude,
                row.Latitude, row.LocationReason, row.Instrument, row.Method, row.Notes)).ToArray(),
            value.Evidence?.Select(row => row is null ? null! : LegacyRepairEvidence(row)).ToArray(),
            value.LocationProof is null ? null : new(value.LocationProof.Kind, value.LocationProof.Checklist,
                value.LocationProof.Longitude, value.LocationProof.Latitude, value.LocationProof.ObservedSlabId,
                value.LocationProof.ObservedSegmentId, value.LocationProof.ObservedRouteVersionId,
                value.LocationProof.ObservedChainageMeters), value.CaptureType, value.Repaired,
            value.UnrepairedReason, value.DeviceId);
    private async Task<FieldWorkflowResultFact> SubmitAsync(FieldWorkflowCommand c, FieldInspectionTask task, FieldInspectionAssignment assignment, CancellationToken token)
    {
        var input = c.Input as FieldSubmissionInputFact ?? throw new ArgumentException("Submission input required.");
        var hash = Hash(new { task.Id, input, c.Admission.OriginalActorId });
        var duplicate = await OriginAsync(c.ProjectId, input.OriginId, "FIELD_SUBMISSION", hash, c.Admission.OriginalActorId, task.Id, token);
        if (duplicate is not null) return new(200, Value: await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == duplicate.EffectId, token));
        if (task.Status is not (FieldInspectionTaskStatus.InProgress or FieldInspectionTaskStatus.SupplementRequired)) Deny(409, "invalid_state_transition");
        var start = await db.Set<FieldTaskStartOrigin>().SingleOrDefaultAsync(x => x.Id == input.StartOriginId && x.TaskId == task.Id, token);
        if (start is null) Deny(409, "first_start_required");
        var latest = await db.Set<FieldInspectionSubmission>().Where(x => x.TaskId == task.Id).OrderByDescending(x => x.Revision).FirstOrDefaultAsync(token);
        if (latest is null ? input.ParentSubmissionId is not null : input.ParentSubmissionId != latest.Id) Deny(409, "submission_lineage_conflict");
        var admitted = await ImportedFactsAsync(c, token);
        var resolved = new Dictionary<Guid, OfflineEvidenceAdmissionFacts>();
        if (admitted is not null)
        {
            if (evidenceAdmission is null) Deny(403, "signed_evidence_admission_required");
            foreach (var declaration in input.Evidence ?? [])
            {
                var facts = await evidenceAdmission.ResolveDeclaredEvidenceAsync(c.ProjectId, task.Id, admitted.AdmissionId,
                    c.Admission.OriginalActorId, new RepairFieldEvidenceData(declaration.CaptureOriginId, declaration.FileId,
                        declaration.Purpose, declaration.ChecksumSha256, declaration.MediaType, declaration.CapturedAt,
                        declaration.AttemptChecklist), token);
                if (facts is not null)
                {
                    if (facts.AdmissionId != admitted.AdmissionId || facts.OriginalActorId != c.Admission.OriginalActorId ||
                        !string.Equals(facts.Checksum, declaration.ChecksumSha256, StringComparison.OrdinalIgnoreCase) || facts.Purpose != declaration.Purpose ||
                        facts.MediaType != declaration.MediaType || declaration.FileId is Guid declared && declared != facts.FileId)
                        Deny(409, "evidence_version_mismatch");
                    resolved.Add(declaration.CaptureOriginId, facts);
                }
                else if (declaration.FileId is not null) Deny(403, "signed_evidence_admission_required");
            }
        }
        var now = clock.GetUtcNow(); var id = admitted?.EffectId ?? Guid.NewGuid();
        var session = FieldInspectionSession.Create(Guid.NewGuid(), task.Purpose, task.Id, c.ProjectId, task.RoadSectionVersionId, task.SurveyId,
            "FIELD-S-" + Guid.NewGuid().ToString("N"), c.Admission.OriginalActorId, "FIELD inspector", now, null, "FIELD capture",
            FieldInspectionSessionStatus.Completed, null);
        var measurements = ValidateMeasurements(task, input, session.Id, c.Admission.OriginalActorId, now);
        var missing = await ReadinessAsync(task, input, start, token, c.Admission.OriginalActorId, resolved);
        var row = FieldInspectionSubmission.Create(id, c.ProjectId, task.Id, latest?.RootId ?? id, latest?.Id, (latest?.Revision ?? 0) + 1,
            assignment.Id, start.Id, session.Id, input.OriginId, hash, c.Admission.OriginalActorId, now, JsonSerializer.Serialize(input, Json),
            missing.Count == 0 ? "READY" : "INCOMPLETE", JsonSerializer.Serialize(missing, Json));
        db.AddRange(FieldInspectionOperationOrigin.Create(id, c.ProjectId, input.OriginId, "FIELD_SUBMISSION", hash, c.Admission.OriginalActorId, input.DeviceId, task.Id, id, now), session, row);
        db.GroundTruthMeasurements.AddRange(measurements);
        foreach (var declaration in input.Evidence ?? [])
        {
            resolved.TryGetValue(declaration.CaptureOriginId, out var actual);
            var capture = actual is null ? await CaptureEvidenceFactsAsync(task, declaration, token) :
                await CaptureResolvedEvidenceFactsAsync(task, declaration, actual, token);
            db.Set<FieldInspectionEvidenceLink>().Add(FieldInspectionEvidenceLink.Create(Guid.NewGuid(), c.ProjectId, task.Id, assignment.Id, id,
                actual?.FileId ?? declaration.FileId, declaration.CaptureOriginId, declaration.Purpose, declaration.ChecksumSha256, declaration.MediaType, capture));
        }
        var proof = input.LocationProof;
        db.Set<FieldInspectionLocationProof>().Add(FieldInspectionLocationProof.Create(Guid.NewGuid(), c.ProjectId, task.Id, id,
            proof?.Kind ?? "UNKNOWN", JsonSerializer.Serialize(proof is null ? new { reason = "No location proof declared" } : (object)proof, Json)));
        if (latest is null) db.Set<DeadlineClock>().Add(DeadlineClock.Create(Guid.NewGuid(), c.ProjectId, DeadlineClockKind.ProjectManagerReview, task.Id, id, now));
        await RoadGuardSystem.Repositories.Messaging.BusinessRequestProducer.CompleteSupplements(db, c.ProjectId, task.Id, now, token);
        task.Transition(FieldInspectionTaskStatus.Submitted); Emit(c, task, assignment, "SUBMITTED", "Immutable FIELD intake", now, id);
        return new(201, Value: row);
    }
    private static GroundTruthMeasurement[] ValidateMeasurements(FieldInspectionTask task, FieldSubmissionInputFact input, Guid session, Guid actor, DateTimeOffset now)
    {
        if (input.CaptureType is not ("MEASUREMENT" or "REPAIR_CLAIM") || input.CaptureType == "MEASUREMENT" && (input.Repaired is not null || input.UnrepairedReason is not null) ||
            input.CaptureType == "REPAIR_CLAIM" && (task.Purpose != FieldInspectionPurpose.PostRepair || input.Repaired is null || input.Repaired == false && string.IsNullOrWhiteSpace(input.UnrepairedReason)))
            throw new ArgumentException("Measurement capture and actual repair claim are distinct.");
        if (input.Measurements?.Length > 200 || input.Evidence?.Length > 100) throw new ArgumentException("Bounded intake arrays required.");
        var rows = new List<GroundTruthMeasurement>(); var samples = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in input.Measurements ?? [])
        {
            if (item is null || string.IsNullOrWhiteSpace(item.SampleId) || !samples.Add(item.SampleId) || item.Value > 9999999999999.999999m ||
                item.Value is decimal v && decimal.Round(v, 6) != v || !Enum.TryParse<MeasurementType>(item.Type, true, out var type)) throw new ArgumentException("Malformed measurement.");
            if (item.Longitude.HasValue != item.Latitude.HasValue) throw new ArgumentException("Coordinates supplied together.");
            Point? point = item.Longitude is double lon ? new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(lon, item.Latitude!.Value)) : null;
            rows.Add(GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(), session, item.SampleId, task.RoadSectionVersionId, task.SurveyId, task.DefectId,
                type, item.Value, item.State, item.UnknownReason, item.Dimension, item.Unit, point, item.LocationReason, item.Instrument, item.Method,
                actor.ToString(), now, null, item.Notes ?? "Declared evidence remains independently linked to immutable submission"));
        }
        var captures = new HashSet<Guid>();
        foreach (var item in input.Evidence ?? [])
            if (item is null || !captures.Add(item.CaptureOriginId) || item.CaptureOriginId == Guid.Empty || item.FileId == Guid.Empty ||
                item.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT") || item.Purpose == "AFTER" && task.Purpose != FieldInspectionPurpose.PostRepair || string.IsNullOrEmpty(item.ChecksumSha256) || item.ChecksumSha256.Length != 64 ||
                item.ChecksumSha256.Any(x => !Uri.IsHexDigit(x)) || string.IsNullOrWhiteSpace(item.MediaType) || item.MediaType.Length > 120 || item.AttemptChecklist?.Length > 500)
                throw new ArgumentException("Malformed evidence declaration.");
        return rows.ToArray();
    }
    private async Task<List<string>> ReadinessAsync(FieldInspectionTask task, FieldSubmissionInputFact input, FieldTaskStartOrigin start, CancellationToken token, Guid? expectedOwner = null,
        Dictionary<Guid, OfflineEvidenceAdmissionFacts>? resolved = null)
    {
        var missing = new HashSet<string>(StringComparer.Ordinal);
        if (input.Measurements is null || input.Measurements.Length == 0) missing.Add("MEASUREMENT_MISSING");
        else
        {
            if (!input.Measurements.Any(x => Enum.TryParse<MeasurementType>(x.Type, true, out var type) && (byte)type == task.RequiredMeasurementType)) missing.Add("REQUIRED_MEASUREMENT_MISSING");
            if (input.Measurements.Any(x => x.State == "UNKNOWN")) missing.Add("MEASUREMENT_VALUE_UNKNOWN");
        }
        var proof = input.LocationProof;
        if (proof is null || proof.Kind == "UNKNOWN") missing.Add("LOCATION_UNKNOWN");
        else if (proof.Kind == "GPS_CAPTURE")
        {
            if (proof.Longitude is not double lon || proof.Latitude is not double lat || !double.IsFinite(lon) || !double.IsFinite(lat) || lon is < -180 or > 180 || lat is < -90 or > 90)
                throw new ArgumentException("Malformed GPS capture.");
            // Valid GPS is captured evidence. H2 candidate/legacy profiles cannot establish its metric binding.
            // No owner-sourced tolerance/accepted GPS mapping is configured in this package.
            missing.Add("GPS_SOURCE_BINDING_UNAVAILABLE");
            if (proof.Checklist is not null || proof.ObservedSlabId is not null || proof.ObservedSegmentId is not null)
                missing.Add("LOCATION_PROOF_CONTRADICTORY");
        }
        else if (proof.Kind == "POSITION_CHECKLIST")
        {
            if (proof.Longitude is not null || proof.Latitude is not null) missing.Add("LOCATION_PROOF_CONTRADICTORY");
            if (task.SlabId is not null)
            {
                if (proof.Checklist != "SLAB_MARKINGS_CONFIRMED" || task.LayoutRevisionId is null || proof.ObservedSlabId != task.SlabId)
                    missing.Add("POSITION_CHECKLIST_INSUFFICIENT");
            }
            else if (task.SegmentSetId is Guid set && proof.Checklist == "SEGMENT_MARKINGS_CONFIRMED")
            {
                var segment = await db.RoadSegments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == proof.ObservedSegmentId && x.SegmentSetId == set && x.RoadSectionVersionId == task.RoadSectionVersionId, token);
                if (segment is null || proof.ObservedChainageMeters is not double offset || !double.IsFinite(offset) ||
                    segment.FromOffsetMeters is not double from || segment.ToOffsetMeters is not double to || offset < from || offset > to)
                    missing.Add("POSITION_CHECKLIST_INSUFFICIENT");
            }
            else if (proof.Checklist == "ROUTE_CHAINAGE_MARKINGS_CONFIRMED" && proof.ObservedRouteVersionId == task.RoadSectionVersionId)
            {
                var route = await db.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == task.RoadSectionVersionId, token);
                var native = await db.Set<RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == task.RoadSectionVersionId, token);
                var length = native?.CanonicalLengthMeters ?? route.Geometry.Length;
                if (proof.ObservedChainageMeters is not double offset || !double.IsFinite(offset) || offset < 0 || offset > length)
                    missing.Add("POSITION_CHECKLIST_INSUFFICIENT");
            }
            else missing.Add("POSITION_CHECKLIST_INSUFFICIENT");
        }
        else throw new ArgumentException("Unsupported location proof kind.");
        if (input.Evidence is null || input.Evidence.Length == 0) missing.Add("EVIDENCE_MISSING");
        foreach (var item in input.Evidence ?? [])
        {
            var actual = resolved is not null && resolved.TryGetValue(item.CaptureOriginId, out var facts) ? facts : null;
            if (item.FileId is null && actual is null) { missing.Add("EVIDENCE_UPLOAD_NOT_CREATED"); continue; }
            var file = await EvidenceFileAsync(task, actual is null ? item : item with { FileId = actual.FileId }, token, actual?.ActualUploaderId ?? expectedOwner);
            if (file.State != "VERIFIED") missing.Add("EVIDENCE_PENDING");
            if (input.CaptureType == "REPAIR_CLAIM" && input.Repaired == true && item.Purpose == "AFTER")
            {
                if (item.AttemptChecklist != "POST_REPAIR_CAPTURE" || item.CapturedAt is null || item.CapturedAt > clock.GetUtcNow() ||
                    start.VerifiedOriginalAt is null || item.CapturedAt < start.VerifiedOriginalAt) missing.Add("AFTER_CAPTURE_PROVENANCE_INSUFFICIENT");
            }
        }
        if (input.CaptureType == "REPAIR_CLAIM" && input.Repaired == true)
        {
            if (!(input.Evidence ?? []).Any(x => x.Purpose == "AFTER")) missing.Add("AFTER_EVIDENCE_MISSING");
            // H3 has no trusted H4 attempt/finish binding: a declared capture/checklist is not freshness proof.
            missing.Add("AFTER_ATTEMPT_BINDING_UNAVAILABLE");
        }
        return missing.Order(StringComparer.Ordinal).ToList();
    }
    private async Task<ReporterFileFacts> EvidenceFileAsync(FieldInspectionTask task, FieldEvidenceDeclarationFact declaration, CancellationToken token, Guid? expectedOwner = null)
    {
        if (string.IsNullOrEmpty(declaration.ChecksumSha256) || declaration.ChecksumSha256.Length != 64 || !declaration.ChecksumSha256.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(declaration.MediaType))
            throw new ArgumentException("Malformed evidence declaration.");
        var id = declaration.FileId!.Value;
        await db.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsNoTracking().ToListAsync(token);
        await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}").AsNoTracking().ToListAsync(token);
        var file = await new AnhHuyFactsRepository(db).GetFileAsync(id, token);
        var scope = await db.FileScopes.AsNoTracking().SingleOrDefaultAsync(x => x.FileId == id, token);
        if (file is null || scope is null) Deny(404, "evidence_not_found");
        var normalizedChecksum = declaration.ChecksumSha256.ToLowerInvariant();
        var reuse = declaration.Purpose == "BEFORE" && await db.Set<FieldInspectionEvidenceReuseDecision>().AnyAsync(x => x.TaskId == task.Id && x.FileId == id && x.FileChecksum == normalizedChecksum, token);
        if (reuse)
        {
            var decision = await db.Set<FieldInspectionEvidenceReuseDecision>().AsNoTracking().Where(x => x.TaskId == task.Id && x.FileId == id && x.FileChecksum == normalizedChecksum).OrderByDescending(x => x.OccurredAt).FirstAsync(token);
            await ValidateHistoricalReuseAsync(task, decision, file, token);
        }
        if (!reuse && (scope.ProjectId != task.ProjectId || scope.TargetId != task.Id || scope.Purpose != declaration.Purpose || expectedOwner is Guid owner && file.OwnerId != owner)) Deny(403, "evidence_access_forbidden");
        if (file.Checksum != normalizedChecksum || file.MediaType != declaration.MediaType) Deny(409, "evidence_version_mismatch");
        return file;
    }
    private async Task<string> CaptureEvidenceFactsAsync(FieldInspectionTask task, FieldEvidenceDeclarationFact declaration, CancellationToken token)
    {
        if (declaration.FileId is null) return JsonSerializer.Serialize(new { declared = declaration, state = "UPLOAD_NOT_CREATED" }, Json);
        var file = await EvidenceFileAsync(task, declaration, token);
        return JsonSerializer.Serialize(new { declared = declaration, stateAtIntake = file.State, file.FileVersion, file.OwnerId, file.Purpose, file.UploadedAt }, Json);
    }
    private async Task<string> CaptureResolvedEvidenceFactsAsync(FieldInspectionTask task, FieldEvidenceDeclarationFact declaration,
        OfflineEvidenceAdmissionFacts admitted, CancellationToken cancellationToken)
    {
        var file = await EvidenceFileAsync(task, declaration with { FileId = admitted.FileId }, cancellationToken, admitted.ActualUploaderId);
        return JsonSerializer.Serialize(new
        {
            declared = declaration,
            stateAtIntake = file.State,
            file.FileVersion,
            file.OwnerId,
            file.Purpose,
            file.UploadedAt,
            actualFileId = admitted.FileId,
            offlineAdmission = admitted
        }, Json);
    }
    private async Task<FieldWorkflowResultFact> ReviewAsync(FieldWorkflowCommand c, FieldInspectionTask task, CancellationToken token)
    {
        var input = c.Input as FieldReviewInputFact ?? throw new ArgumentException("Review input required.");
        if (task.Status != FieldInspectionTaskStatus.Submitted) Deny(409, "invalid_state_transition");
        var row = await db.Set<FieldInspectionSubmission>().SingleOrDefaultAsync(x => x.Id == input.SubmissionId && x.TaskId == task.Id, token);
        if (row is null) Deny(404, "not_found");
        if (await db.Set<FieldInspectionSubmission>().AnyAsync(x => x.TaskId == task.Id && x.Revision > row.Revision, token)) Deny(409, "submission_lineage_conflict");
        var payload = Decode<FieldSubmissionInputFact>(row.PayloadJson);
        var start = await db.Set<FieldTaskStartOrigin>().SingleAsync(x => x.Id == row.StartOriginId, token);
        if (input.Decision != "SUPPLEMENT")
        {
            var retained = new Dictionary<Guid, OfflineEvidenceAdmissionFacts>();
            var links = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(x => x.SubmissionId == row.Id).ToArrayAsync(token);
            foreach (var link in links)
            {
                using var capture = JsonDocument.Parse(link.CaptureFactsJson);
                if (!capture.RootElement.TryGetProperty("offlineAdmission", out var admittedJson)) continue;
                if (!capture.RootElement.TryGetProperty("stateAtIntake", out var state) || state.GetString() != "VERIFIED") continue;
                var admitted = admittedJson.Deserialize<OfflineEvidenceAdmissionFacts>(Json) ?? throw new JsonException("Missing accepted evidence proof.");
                if (admitted.FileId != link.FileId || admitted.OriginalActorId != row.OriginalActorId || admitted.Checksum != link.DeclaredChecksum ||
                    admitted.Purpose != link.Purpose || admitted.MediaType != link.MediaType) Deny(409, "evidence_version_mismatch");
                retained.Add(link.CaptureOriginId, admitted);
            }
            if ((await ReadinessAsync(task, payload, start, token, row.OriginalActorId, retained)).Count != 0) Deny(409, "field_result_insufficient");
        }
        var now = clock.GetUtcNow(); var review = FieldInspectionReview.Create(Guid.NewGuid(), c.ProjectId, task.Id, row.Id, c.Admission.CallerId, input.Decision, input.Reason, now);
        db.Set<FieldInspectionReview>().Add(review);
        if (input.Decision == "SUPPLEMENT")
        {
            var assigned = await CurrentAssignmentAsync(task.Id, token);
            if (assigned is null) Deny(409, "current_assignment_missing");
            RoadGuardSystem.Repositories.Messaging.BusinessRequestProducer.Supplement(db, c.ProjectId,
                "FieldReview", review.Id, task.Id, assigned.AssignedToUserId, now);
        }
        task.Transition(input.Decision == "SUPPLEMENT" ? FieldInspectionTaskStatus.SupplementRequired : FieldInspectionTaskStatus.Completed);
        if (input.Decision != "SUPPLEMENT")
        {
            var reviewClock = await db.Set<DeadlineClock>().SingleAsync(x => x.ProjectId == c.ProjectId && x.TargetId == task.Id && x.Kind == DeadlineClockKind.ProjectManagerReview, token);
            reviewClock.Complete(now);
        }
        Emit(c, task, await CurrentAssignmentAsync(task.Id, token), input.Decision == "SUPPLEMENT" ? "SUPPLEMENT" : "REVIEWED", input.Reason, now, row.Id);
        return new(201, Value: new FieldReviewViewFact(review.Id, task.Id, row.Id, review.Decision, review.ReceiptActivation));
    }
}
