using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // Explicit compatibility bridge into the existing FIELD validators; new repair callers use internal contracts.
    private Task ValidateSourceAsync(Guid project, RepairFieldTaskData input, Guid? ai, CancellationToken token)
        => ValidateSourceAsync(project, LegacyRepairTask(input), ai, token);
    private Task ValidatePinsAsync(Guid project, RepairFieldTaskData input, CancellationToken token)
        => ValidatePinsAsync(project, LegacyRepairTask(input), token);
    private static FieldTaskCreateInputFact LegacyRepairTask(RepairFieldTaskData value)
        => new(value.DefectId, value.DefectVersion, value.SurveyId, value.SourceKind, value.RouteVersionId,
            value.SegmentSetId, value.LayoutRevisionId, value.SlabId, value.Purpose, value.RequiredMeasurementType,
            value.MeasurementScope, value.Instructions, value.AssignedToUserId, value.DueAt,
            value.MapPublicationId, value.CrsProfileRevisionId);
    private async Task<FieldWorkflowResultFact> ApplyBusinessAsync(FieldWorkflowCommand c, CancellationToken token)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == c.ProjectId && x.Status == ProjectStatus.Active, token)) Deny(409, "project_not_active");
        if (c.Action == "create") return await CreateTaskAsync(c, token);
        var task = await TaskAsync(c.TaskId!.Value, token);
        if (task.LifecycleVersion != 2) Deny(409, "legacy_task_read_only");
        if ((task.TaskMode is "NORMAL" or "CONDITIONAL_FT") && (c.Action is "cancel" or "reassign" or "assign" or "review" or "impact"))
            Deny(409, "repair_binding_required");
        var admitted = await ImportedFactsAsync(c, token);
        if (c.Action == "accept" && admitted is not null)
        {
            var replay = await OriginAsync(c.ProjectId, admitted.EffectId, "FIELD_ACCEPT", admitted.CorePayloadHash, c.Admission.OriginalActorId, task.Id, token);
            if (replay is not null) return new(200, Value: task);
        }
        if (c.Input is FieldStartInputFact retryStart)
        {
            var replay = await OriginAsync(c.ProjectId, retryStart.OriginId, "FIELD_START", Hash(new { task.Id, input = retryStart, c.Admission.OriginalActorId }), c.Admission.OriginalActorId, task.Id, token);
            if (replay is not null) return new(200, Value: await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x => x.Id == replay.EffectId, token));
        }
        if (c.Input is FieldSubmissionInputFact retrySubmission)
        {
            var replay = await OriginAsync(c.ProjectId, retrySubmission.OriginId, "FIELD_SUBMISSION", Hash(new { task.Id, input = retrySubmission, c.Admission.OriginalActorId }), c.Admission.OriginalActorId, task.Id, token);
            if (replay is not null) return new(200, Value: await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == replay.EffectId, token));
        }
        if (c.ExpectedVersion != Convert.ToBase64String(task.RowVersion)) Deny(409, "concurrency_conflict");
        var assignment = await CurrentAssignmentAsync(task.Id, token);
        if (c.Action is "accept" or "reject" or "start" or "submit")
            if (assignment is null || assignment.AssignedToUserId != c.Admission.OriginalActorId ||
                admitted is not null && assignment.Id != admitted.AssignmentId) Deny(403, "access_forbidden");
        var now = clock.GetUtcNow();
        switch (c.Action)
        {
            case "accept":
                task.Transition(FieldInspectionTaskStatus.Accepted);
                Emit(c, task, assignment, "ACCEPTED", Reason(c.Input), now, eventId: admitted?.EffectId);
                if (admitted is not null) db.Add(FieldInspectionOperationOrigin.Create(admitted.EffectId, c.ProjectId, admitted.EffectId,
                    "FIELD_ACCEPT", admitted.CorePayloadHash, c.Admission.OriginalActorId, admitted.SourceDeviceId, task.Id, admitted.EffectId, now));
                break;
            case "reject": task.Transition(FieldInspectionTaskStatus.Rejected); assignment!.End(now, Reason(c.Input), true); Emit(c, task, assignment, "REJECTED", Reason(c.Input), now); break;
            case "cancel":
                var cancelFacts = await HandoverFactsAsync(task, c.Input as FieldTaskActionInputFact, token);
                task.Transition(FieldInspectionTaskStatus.Cancelled); assignment?.End(now, Reason(c.Input)); Emit(c, task, assignment, "CANCELLED", Reason(c.Input), now, facts: cancelFacts); break;
            case "assign":
            case "reassign":
                var action = c.Input as FieldTaskActionInputFact ?? throw new ArgumentException("Assignment input required.");
                var next = action.AssignedToUserId.GetValueOrDefault();
                if (next == Guid.Empty) Deny(400, "validation_error");
                await GuardCrewAsync(c.ProjectId, next, token);
                var handoverFacts = await HandoverFactsAsync(task, action, token);
                assignment?.End(now, Reason(action)); task.Reassign();
                var replacement = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, next, c.Admission.CallerId, now, null, FieldInspectionAssignmentStatus.Active, null);
                db.FieldInspectionAssignments.Add(replacement);
                await RoadGuardSystem.Repositories.Messaging.BusinessRequestProducer.ReassignSupplements(db, c.ProjectId,
                    task.Id, next, c.Admission.CallerId, Reason(action), now, token);
                Emit(c, task, replacement, "REASSIGNED", Reason(action), now, facts: handoverFacts); break;
            case "start": return await StartAsync(c, task, assignment!, token);
            case "submit": return await SubmitAsync(c, task, assignment!, token);
            case "review": return await ReviewAsync(c, task, token);
            case "reuse": return await ReuseAsync(c, task, token);
            case "impact": return await ApplyImpactAsync(c, task, assignment, token);
            default: Deny(400, "validation_error"); break;
        }
        return new(201, Value: task);
    }
    private static string Reason(object? input) => input is FieldTaskActionInputFact a && !string.IsNullOrWhiteSpace(a.Reason) && a.Reason.Trim().Length <= 2000 ? a.Reason.Trim() : throw new ArgumentException("Bounded reason required.");
    private async Task GuardCrewAsync(Guid project, Guid crew, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, crew, project, token);
        var date = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(crew, UserRoleCode.RepairCrew, token) ||
            !await db.ProjectMembers.AnyAsync(x => x.ProjectId == project && x.UserId == crew && x.RoleCode == UserRoleCode.RepairCrew &&
                x.Status == ProjectMemberStatus.Active && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date), token)) Deny(403, "assignee_not_authorized");
    }
    private async Task<FieldWorkflowResultFact> CreateTaskAsync(FieldWorkflowCommand c, CancellationToken token)
    {
        var input = c.Input as FieldTaskCreateInputFact ?? throw new ArgumentException("Task input required.");
        if (string.IsNullOrWhiteSpace(input.DefectVersion) || input.DueAt == default || input.AssignedToUserId == Guid.Empty) Deny(400, "validation_error");
        var defect = await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.DefectId}").SingleOrDefaultAsync(token);
        if (defect is null || defect.ProjectId != c.ProjectId) Deny(404, "not_found");
        if (defect.Status != DefectStatus.Open || defect.RoadSectionVersionId != input.RouteVersionId) Deny(409, "source_not_ready");
        if (input.DefectVersion != Convert.ToBase64String(db.Entry(defect).Property<byte[]>("RowVersion").CurrentValue!)) Deny(409, "concurrency_conflict");
        await ValidateSourceAsync(c.ProjectId, input, defect.SourceAIDetectionId, token);
        await ValidatePinsAsync(c.ProjectId, input, token);
        await GuardCrewAsync(c.ProjectId, input.AssignedToUserId, token);
        var purpose = input.Purpose switch { "PRE_MEASUREMENT" => FieldInspectionPurpose.PreMeasurement, "POST_REPAIR" => FieldInspectionPurpose.PostRepair, "VERIFICATION" => FieldInspectionPurpose.Verification, _ => FieldInspectionPurpose.Unknown };
        var now = clock.GetUtcNow();
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "FIELD-" + Guid.NewGuid().ToString("N"), c.ProjectId, input.DefectId,
            input.SurveyId, input.SourceKind, input.RouteVersionId, input.SegmentSetId, input.LayoutRevisionId, input.SlabId, purpose,
            input.RequiredMeasurementType, input.MeasurementScope, input.Instructions, input.DueAt, c.Admission.CallerId, input.MapPublicationId, input.CrsProfileRevisionId);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, input.AssignedToUserId, c.Admission.CallerId, now, null, FieldInspectionAssignmentStatus.Active, null);
        db.AddRange(task, assignment); Emit(c, task, assignment, "ASSIGNED", "FIELD measurement assignment", now);
        return new(201, Value: task);
    }
    private async Task ValidateSourceAsync(Guid project, FieldTaskCreateInputFact input, Guid? ai, CancellationToken token)
    {
        if (input.SourceKind == "REPORTER")
        {
            if (input.SurveyId is not null || !await (from link in db.Set<HuyDefectSourceLink>()
                                                      join decision in db.SourceDecisions on link.DecisionId equals decision.Id
                                                      where link.ProjectId == project && link.DefectId == input.DefectId && link.EndedAt == null &&
                                                          link.SourceKind == CandidateSourceKind.Report && decision.Decision == CandidateDecisionKind.KeepNew
                                                      select link.Id).AnyAsync(token)) Deny(409, "source_not_ready");
        }
        else if (input.SourceKind == "SURVEY")
        {
            if (input.SurveyId is not Guid survey || !await db.Surveys.AnyAsync(x => x.Id == survey && x.ProjectId == project && x.RoadSectionVersionId == input.RouteVersionId, token)) Deny(409, "source_not_ready");
            if (ai is not Guid confirmedDetection || !await (from link in db.Set<HuyDefectSourceLink>()
                                                             join decision in db.SourceDecisions on link.DecisionId equals decision.Id
                                                             where link.ProjectId == project && link.DefectId == input.DefectId && link.EndedAt == null && link.SourceKind == CandidateSourceKind.AiDetection &&
                                                                 link.AIDetectionSourceId == confirmedDetection && decision.Decision == CandidateDecisionKind.KeepNew
                                                             select link.Id).AnyAsync(token)) Deny(409, "source_not_ready");
            if (ai is not Guid detection || !await (from a in db.AIDetections
                                                    join job in db.ProcessingJobs on a.ProcessingJobId equals job.Id
                                                    join block in db.ProcessingBlocks on job.ProcessingBlockId equals block.Id
                                                    join version in db.SurveyDataVersions on block.SurveyDataVersionId equals version.Id
                                                    where a.Id == detection && version.SurveyId == input.SurveyId
                                                    select a.Id).AnyAsync(token)) Deny(409, "source_not_ready");
        }
        else Deny(400, "validation_error");
    }
    private async Task ValidatePinsAsync(Guid project, FieldTaskCreateInputFact input, CancellationToken token)
    {
        if (!await (from route in db.RoadSectionVersions
                    join road in db.RoadSections on route.RoadSectionId equals road.Id
                    where route.Id == input.RouteVersionId && road.ProjectId == project
                    select route.Id).AnyAsync(token)) Deny(409, "geometry_version_mismatch");
        if (input.SegmentSetId is Guid set && !await db.RoadSegmentSets.AnyAsync(x => x.Id == set && x.RoadSectionVersionId == input.RouteVersionId && x.Status == "PUBLISHED", token)) Deny(409, "geometry_version_mismatch");
        if (input.LayoutRevisionId is Guid layout)
        {
            var row = await db.Set<RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == layout, token);
            if (row is null || row.ProjectId != project || row.RouteVersionId != input.RouteVersionId || row.SegmentSetId != input.SegmentSetId || row.CrsProfileRevisionId != input.CrsProfileRevisionId) Deny(409, "geometry_version_mismatch");
            if (input.SlabId is not null)
            {
                var geometry = Decode<RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementGeometryPreviewFact>(row.SnapshotJson);
                if (!geometry.Slabs.Any(x => x.Key == input.SlabId)) Deny(409, "geometry_version_mismatch");
            }
        }
        else if (input.SlabId is not null) Deny(400, "validation_error");
        if (input.MapPublicationId is Guid map && !await db.Set<RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication>().AnyAsync(x => x.Id == map && x.ProjectId == project &&
            x.RouteVersionId == input.RouteVersionId && x.SegmentSetId == input.SegmentSetId && x.LayoutRevisionId == input.LayoutRevisionId && x.CrsProfileRevisionId == input.CrsProfileRevisionId, token)) Deny(409, "geometry_version_mismatch");
        var native = await db.Set<RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == input.RouteVersionId, token);
        if (native?.CrsProfileRevisionId != input.CrsProfileRevisionId) Deny(409, "geometry_version_mismatch");
    }
    private async Task<FieldWorkflowResultFact> StartAsync(FieldWorkflowCommand c, FieldInspectionTask task, FieldInspectionAssignment assignment, CancellationToken token)
    {
        var input = c.Input as FieldStartInputFact ?? throw new ArgumentException("First-start input required.");
        var hash = Hash(new { task.Id, input, c.Admission.OriginalActorId });
        var duplicate = await OriginAsync(c.ProjectId, input.OriginId, "FIELD_START", hash, c.Admission.OriginalActorId, task.Id, token);
        if (duplicate is not null) return new(200, Value: await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x => x.Id == duplicate.EffectId, token));
        if (task.Status != FieldInspectionTaskStatus.Accepted) Deny(409, "invalid_state_transition");
        var first = await db.Set<FieldTaskStartOrigin>().SingleOrDefaultAsync(x => x.TaskId == task.Id, token);
        task.Transition(FieldInspectionTaskStatus.InProgress);
        if (first is not null) { Emit(c, task, assignment, "STARTED", "Resume immutable first-start after handover", clock.GetUtcNow()); return new(200, Value: first); }
        var admitted = await ImportedFactsAsync(c, token);
        var now = clock.GetUtcNow(); var id = admitted?.EffectId ?? Guid.NewGuid();
        var origin = FieldTaskStartOrigin.Create(id, c.ProjectId, task.Id, assignment.Id, input.OriginId, hash, c.Admission.OriginalActorId, input.DeviceId,
            input.ClaimedAt, input.MonotonicMilliseconds, input.BootId, now, task.RoadSectionVersionId, task.SegmentSetId, task.LayoutRevisionId,
            c.Admission.TrustedOnlineOrigin, task.MapPublicationId, task.CrsProfileRevisionId, task.SlabId, JsonSerializer.Serialize(new { input.OfflineProof }, Json));
        db.AddRange(FieldInspectionOperationOrigin.Create(id, c.ProjectId, input.OriginId, "FIELD_START", hash, c.Admission.OriginalActorId, input.DeviceId, task.Id, id, now), origin);
        Emit(c, task, assignment, "STARTED", "Immutable first FIELD start", now);
        if (task.TaskMode is "NORMAL" or "CONDITIONAL_FT")
        {
            // Existing obligation/auth updates must not precede their new immutable source row.
            await db.SaveChangesAsync(token);
            await RecordRepairFirstStartAsync(task, origin, token);
        }
        return new(201, Value: origin);
    }
    private async Task<FieldInspectionOperationOrigin?> OriginAsync(Guid project, Guid origin, string kind, string hash, Guid originalActor, Guid taskId, CancellationToken token)
    {
        if (origin == Guid.Empty) Deny(400, "validation_error");
        if (offlineAdmission is not null) await offlineAdmission.GuardOriginBindingAsync(project, origin, kind, hash, originalActor, taskId, token);
        var row = await db.Set<FieldInspectionOperationOrigin>().FromSqlInterpolated($"SELECT * FROM [FieldInspectionOperationOrigins] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={project} AND [OriginId]={origin}").AsNoTracking().SingleOrDefaultAsync(token);
        if (row is not null && (row.Kind != kind || row.ContentHash != hash)) Deny(409, "origin_content_conflict");
        return row;
    }
    private void Emit(FieldWorkflowCommand c, FieldInspectionTask task, FieldInspectionAssignment? assignment, string kind, string reason, DateTimeOffset now, Guid? revision = null, object? facts = null, Guid? impactId = null, Guid? impactDecisionId = null, Guid? eventId = null)
    {
        var id = eventId ?? Guid.NewGuid(); db.Set<FieldInspectionTaskEvent>().Add(FieldInspectionTaskEvent.Create(id, c.ProjectId, task.Id, assignment?.Id, c.Admission.OriginalActorId, kind, reason, now, JsonSerializer.Serialize(facts ?? new { }, Json), impactId, impactDecisionId));
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), c.Admission.CallerId, now, "field_" + kind.ToLowerInvariant(), "FieldInspectionTask", task.Id, null, null, reason, "h3.field", null));
        var messageType = kind switch { "ASSIGNED" or "REASSIGNED" => "field.task.assigned.v1", "SUBMITTED" => "field.task.submitted.v1", "SUPPLEMENT" => "field.task.supplement_requested.v1", _ => "field.task.lifecycle.v1" };
        db.OutboxMessages.Add(OutboxMessage.Create(id, messageType, now, null, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventId = id,
            kind,
            projectId = c.ProjectId,
            sourceKind = "FieldTask",
            sourceId = task.Id,
            originEventId = id,
            occurredAtUtc = now,
            sourceRevisionId = revision,
            responsibleUserId = kind is "ASSIGNED" or "REASSIGNED" or "SUPPLEMENT" ? assignment?.AssignedToUserId : null
        }, Json)));
    }
    private async Task<FieldWorkflowResultFact> FinalizeResultAsync(FieldWorkflowCommand c, FieldWorkflowResultFact result, CancellationToken token)
    {
        if (result.Value is FieldInspectionTask task) return result with { Value = await ViewAsync(task, token), Version = Convert.ToBase64String(task.RowVersion) };
        if (result.Value is FieldInspectionSubmission submission) return result with { Value = await SubmissionViewAsync(submission, token), Version = submission.ContentHash };
        if (result.Value is FieldTaskStartOrigin start) return result with
        {
            Value = new
            {
                start.Id,
                start.TaskId,
                start.AssignmentId,
                start.OriginId,
                start.ContentHash,
                start.ClaimedAt,
                start.ServerReceivedAt,
                start.VerifiedOriginalAt,
                start.TimeProvenance,
                start.RouteVersionId,
                start.SegmentSetId,
                start.LayoutRevisionId,
                start.MapPublicationId,
                start.CrsProfileRevisionId,
                start.SlabId,
                start.LocationPolicyVersion
            }
        };
        return result;
    }
}
