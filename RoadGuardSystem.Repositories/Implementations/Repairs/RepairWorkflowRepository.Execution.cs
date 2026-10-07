using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository : IRepairExecutionRepository
{
    private const string AssessmentOperation = "h4.repair.assessment.v1";
    private const string ExecutionStartOperation = "h4.repair.execution-start.v1";
    private const string ExecutionFinishOperation = "h4.repair.execution-finish.v1";
    public Task<RepairWorkflowResult> AssessAsync(RepairAssessmentCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.RepairCrew, command.ProjectId, command.Key,
            AssessmentOperation, command, ct => GuardExecutionResource(command.ActorId, command.ProjectId, command.PackageId,
                command.ItemId, command.TaskId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct, command.ItemId);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId, ct);
                var hash = RepairCommandCoreHash.Assessment(command.TaskId, item.Id, command.ActorId, command.Input);
                var canonical = await RepairOrigin(command.ProjectId, command.Input.OriginId, "REPAIR_ASSESSMENT", hash,
                    command.ActorId, command.TaskId, ct);
                if (canonical is not null)
                {
                    var original = await db.Set<RepairMeasurementAssessment>().SingleAsync(row => row.Id == canonical.EffectId, ct);
                    return (original.Id, new ProducingOutcome<RepairAssessmentFact>(AssessmentView(original), VersionOf(item)));
                }
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.State != RepairItemState.Assigned || item.CurrentExecutionStartId is not null)
                    Deny(409, "invalid_state_transition");
                var first = await db.Set<FieldTaskStartOrigin>().SingleOrDefaultAsync(row => row.Id == command.Input.FieldFirstStartId &&
                    row.ProjectId == command.ProjectId && row.TaskId == binding.TaskId && row.AssignmentId == binding.AssignmentId &&
                    row.OriginalActorId == command.ActorId, ct);
                if (first is null) Deny(409, "first_start_source_conflict");
                var id = RepairEffectId();
                RepairMeasurementAssessment assessment;
                try
                {
                    assessment = await FieldCore().CaptureRepairAssessmentInTransactionAsync(binding, first, command.ActorId,
                        id, command.Input, hash, ct, offlineRepairContext?.Command.Admission.AdmissionId);
                }
                catch (RepairFieldCoreRejectedException rejection) { throw new Denied(rejection.Status, rejection.Code ?? "repair_capture_rejected"); }
                catch (ArgumentException) { throw new Denied(400, "validation_error"); }
                db.AddRange(FieldInspectionOperationOrigin.Create(id, command.ProjectId, command.Input.OriginId,
                    "REPAIR_ASSESSMENT", hash, command.ActorId, RepairOriginDevice(command.Input.DeviceId), binding.TaskId, id, assessment.ServerReceivedAt), assessment);
                await db.SaveChangesAsync(ct);
                db.Entry(item).Property(row => row.CurrentAssessmentId).CurrentValue = id;
                var obligation = package.Obligations.Single(row => row.Id == item.ObligationId);
                if (obligation.OriginalCrewFirstStartId is null)
                    db.Entry(obligation).Property(row => row.OriginalCrewFirstStartId).CurrentValue = first.Id;
                Touch(package);
                AuditProducer(command.ActorId, "repair_assessment", "RepairItem", item.Id,
                    "Immutable PRE_EXECUTION source; no formal intake", new { assessment.Id, assessment.ContentHash, firstStartId = first.Id });
                await db.SaveChangesAsync(ct);
                return (id, new ProducingOutcome<RepairAssessmentFact>(AssessmentView(assessment), VersionOf(item)));
            }, cancellationToken);

    public Task<RepairWorkflowResult> StartExecutionAsync(RepairExecutionStartCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.RepairCrew, command.ProjectId, command.Key,
            ExecutionStartOperation, command, ct => GuardExecutionResource(command.ActorId, command.ProjectId, command.PackageId,
                command.ItemId, command.TaskId, ct), async ct =>
        {
            var package = await LockedPackage(command.PackageId, ct);
            var item = package.Items.Single(row => row.Id == command.ItemId);
            await FreshAnchor(command.ProjectId, item.DefectId, ct, command.ItemId);
            var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId, ct);
            var hash = RepairCommandCoreHash.ExecutionStart(command.TaskId, item.Id, command.ActorId, command.Input);
            var canonical = await RepairOrigin(command.ProjectId, command.Input.OriginId, "REPAIR_EXECUTION_START", hash, command.ActorId, command.TaskId, ct);
            if (canonical is not null)
            {
                var existing = await db.Set<RepairExecutionStart>().SingleAsync(row => row.Id == canonical.EffectId, ct);
                return (existing.Id, new ProducingOutcome<RepairExecutionStartFact>(StartView(existing, binding.TaskId), VersionOf(item)));
            }
            if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
            var assessment = await db.Set<RepairMeasurementAssessment>().SingleOrDefaultAsync(row => row.Id == command.Input.AssessmentId &&
                row.ItemId == item.Id && row.TaskId == command.TaskId && row.BindingId == binding.Id && row.ProjectId == command.ProjectId, ct);
            if (assessment is null) Deny(409, "assessment_required");
            if (item.CurrentAssessmentId != assessment.Id || assessment.FirstStartId != command.Input.FieldFirstStartId ||
                assessment.OriginalActorId != command.ActorId || assessment.AssignmentId != binding.AssignmentId ||
                assessment.Stage != "PRE_EXECUTION" || binding.PlanHash != item.ProposalPlanHash || binding.ChecklistVersion != item.ChecklistVersion)
                Deny(409, "assessment_source_conflict");
            if (item.State != RepairItemState.Assigned || item.CurrentExecutionStartId is not null) Deny(409, "invalid_state_transition");
            Guid? eligibilityId = null;
            if (item.Mode == RepairMode.FastTrack)
            {
                if (offlineRepairContext is not null) Deny(409, "fast_track_offline_authority_not_activated");
                var obligation = package.Obligations.Single(row => row.Id == item.ObligationId);
                var responsibleProject = await ObligationResponsibilityScope.ResolveAsync(db, obligation.Id, obligation.ProjectId, ct);
                var coverage = await RoadCoverageResolver.Resolve(db, responsibleProject, obligation.Scope, clock.GetUtcNow(), ct);
                var policy = await db.Set<RepairPolicyRevision>().AsNoTracking().Include(row => row.Measurements).Include(row => row.Revocations)
                    .SingleOrDefaultAsync(row => row.Id == binding.PolicyRevisionId, ct);
                var authorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == binding.AuthorizationId, ct);
                await db.Entry(assessment).Collection(row => row.Measurements).LoadAsync(ct);
                var missing = await RepairFtEligibility.Missing(db, clock, item, binding, assessment, policy, authorization, coverage, ct);
                if (missing.Length != 0) Deny(409, coverage.State == "UNKNOWN_OWNER_MAPPING" ? "fast_track_coverage_mapping_unknown" :
                    coverage.State == "TEST_ONLY_MAPPING" ? "test_only_source_not_executable" : "fast_track_prerequisites_not_met");
                eligibilityId = Guid.NewGuid();
                db.Add(new RepairEligibilityAssessment(eligibilityId.Value, item.ProjectId, item.Id, binding.Id, assessment.Id,
                    obligation.Scope.PhysicalRoadId, policy!.Id, binding.PolicyContentHash!, RepairFactState.Confirmed, RepairFactState.Confirmed,
                    "LD07_MAPPING:" + coverage.Mapping!.Id.ToString("D"), RoadCoverageResolver.Hash(coverage.Mapping), clock.GetUtcNow(), "[]"));
                await db.SaveChangesAsync(ct);
            }
            else if (item.ApprovedPlanHash != binding.PlanHash || item.ApprovedBy is null) Deny(409, "normal_plan_not_approved");
            var native = await db.FieldInspectionTasks.SingleAsync(row => row.Id == binding.TaskId, ct);
            if (native.Status != FieldInspectionTaskStatus.InProgress) Deny(409, "field_first_start_required");
            var now = clock.GetUtcNow(); var id = RepairEffectId();
            var admittedTime = RepairExecutionTime(now);
            var start = new RepairExecutionStart(id, command.ProjectId, item.Id, binding.Id, assessment.Id,
                assessment.FirstStartId, command.ActorId, id, command.Input.OriginId, hash, command.Input.ClaimedAt,
                now, admittedTime.VerifiedAt, admittedTime.Provenance, assessment.ContentHash, binding.ChecklistVersion,
                offlineRepairContext is null ? JsonSerializer.Serialize(new
                {
                    bindingId = binding.Id,
                    binding.AssignmentId,
                    binding.PlanHash,
                    item.ApprovedBy,
                    item.ApprovedPlanHash,
                    assessmentId = assessment.Id,
                    assessment.ContentHash,
                    admission = "DIRECT_CURRENT_CREW"
                }, Json)
                    : JsonSerializer.Serialize(new
                    {
                        bindingId = binding.Id,
                        binding.AssignmentId,
                        binding.PlanHash,
                        item.ApprovedBy,
                        item.ApprovedPlanHash,
                        assessmentId = assessment.Id,
                        assessment.ContentHash,
                        admission = "SIGNED_OFFLINE_ORIGINAL_CREW",
                        offlineAdmissionId = offlineRepairContext.Command.Admission.AdmissionId,
                        importerId = offlineRepairContext.Command.CallerId,
                        verifiedOriginalAt = admittedTime.VerifiedAt
                    }, Json), eligibilityId);
            db.AddRange(FieldInspectionOperationOrigin.Create(id, command.ProjectId, command.Input.OriginId,
                "REPAIR_EXECUTION_START", hash, command.ActorId, RepairOriginDevice(command.Input.DeviceId), binding.TaskId, id, now), start);
            await db.SaveChangesAsync(ct);
            item.Start(command.ActorId, now); db.Entry(item).Property(row => row.CurrentExecutionStartId).CurrentValue = id;
            Touch(package); AuditProducer(command.ActorId, "repair_execution_start", "RepairItem", item.Id,
                offlineRepairContext is null ? "Actual normal execution event; client time retained separately" :
                    "Signed offline normal execution claim; original time remains uncertain", new { start.Id, start.AssessmentId, start.AssessmentContentHash });
            await db.SaveChangesAsync(ct);
            return (id, new ProducingOutcome<RepairExecutionStartFact>(StartView(start, binding.TaskId), VersionOf(item)));
        }, cancellationToken);
    public Task<RepairWorkflowResult> FinishExecutionAsync(RepairExecutionFinishCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.RepairCrew, command.ProjectId, command.Key,
            ExecutionFinishOperation, command, ct => GuardExecutionResource(command.ActorId, command.ProjectId, command.PackageId,
                command.ItemId, command.TaskId, ct), async ct =>
        {
            var package = await LockedPackage(command.PackageId, ct); var item = package.Items.Single(row => row.Id == command.ItemId);
            await FreshAnchor(command.ProjectId, item.DefectId, ct, command.ItemId);
            var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId, ct);
            var hash = RepairCommandCoreHash.ExecutionFinish(command.TaskId, item.Id, command.ActorId, command.Input);
            var canonical = await RepairOrigin(command.ProjectId, command.Input.OriginId, "REPAIR_EXECUTION_FINISH", hash, command.ActorId, command.TaskId, ct);
            if (canonical is not null)
            {
                var existing = await db.Set<RepairExecutionFinish>().SingleAsync(row => row.Id == canonical.EffectId, ct);
                return (existing.Id, new ProducingOutcome<RepairExecutionFinishFact>(await FinishView(existing, binding.TaskId, ct), VersionOf(item)));
            }
            if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
            var start = await db.Set<RepairExecutionStart>().SingleOrDefaultAsync(row => row.Id == command.Input.ExecutionStartId &&
                row.ItemId == item.Id && row.ProjectId == command.ProjectId && row.BindingId == binding.Id && row.OriginalActorId == command.ActorId, ct);
            if (start is null) Deny(409, "execution_start_required");
            if (item.CurrentExecutionStartId != start.Id || item.State != RepairItemState.InProgress || item.CurrentExecutionFinishId is not null)
                Deny(409, "execution_start_source_conflict");
            var now = clock.GetUtcNow(); if (start.VerifiedOriginalAt > now) Deny(409, "execution_time_source_conflict");
            var id = RepairEffectId(); var admittedTime = RepairExecutionTime(now);
            var finish = new RepairExecutionFinish(id, command.ProjectId, item.Id, binding.Id, start.Id,
                command.ActorId, id, command.Input.OriginId, hash, command.Input.ClaimedAt, now, admittedTime.VerifiedAt, admittedTime.Provenance,
                offlineRepairContext is null ? JsonSerializer.Serialize(new
                {
                    source = "ACTUAL_SERVER_EXECUTION_FINISH",
                    originalEventId = id,
                    executionStartId = start.Id,
                    clientClaim = command.Input.ClaimedAt
                }, Json)
                    : JsonSerializer.Serialize(new
                    {
                        source = "SIGNED_OFFLINE_EXECUTION_FINISH_CLAIM",
                        originalEventId = id,
                        executionStartId = start.Id,
                        clientClaim = command.Input.ClaimedAt,
                        offlineAdmissionId = offlineRepairContext.Command.Admission.AdmissionId,
                        importerId = offlineRepairContext.Command.CallerId,
                        verifiedOriginalAt = admittedTime.VerifiedAt
                    }, Json));
            db.AddRange(FieldInspectionOperationOrigin.Create(id, command.ProjectId, command.Input.OriginId,
                "REPAIR_EXECUTION_FINISH", hash, command.ActorId, RepairOriginDevice(command.Input.DeviceId), binding.TaskId, id, now), finish);
            await db.SaveChangesAsync(ct);
            db.Entry(item).Property(row => row.CurrentExecutionFinishId).CurrentValue = id; Touch(package);
            if (await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == item.Id && row.Kind == DeadlineClockKind.FinishedDataSync, ct))
                Deny(409, "sync_clock_origin_locked");
            if (admittedTime.VerifiedAt is DateTimeOffset verifiedFinish)
                db.Add(DeadlineClock.Create(Guid.NewGuid(), command.ProjectId, DeadlineClockKind.FinishedDataSync, item.Id, id, verifiedFinish));
            AuditProducer(command.ActorId, "repair_execution_finish", "RepairItem", item.Id,
                offlineRepairContext is null ? "Actual finish and one original sync clock; no formal intake" :
                    "Signed offline finish claim; no verified original sync deadline", new { finish.Id, finish.ExecutionStartId });
            await db.SaveChangesAsync(ct);
            return (id, new ProducingOutcome<RepairExecutionFinishFact>(await FinishView(finish, binding.TaskId, ct), VersionOf(item)));
        }, cancellationToken);

    private async Task GuardExecutionResource(Guid actor, Guid project, Guid package, Guid item, Guid task, CancellationToken token)
    {
        await GuardItemResource(project, package, item, token, originalCrew: true);
        var current = await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == item, token);
        if (current.CurrentBindingId is null || current.SupersededByItemId is not null) Deny(403, "repair_binding_not_current");
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == current.CurrentBindingId, token);
        if (binding.ItemId != item || binding.ProjectId != project || binding.TaskId != task || binding.CrewId != actor ||
            !await db.Set<RepairObligation>().AnyAsync(row => row.Id == current.ObligationId && row.CurrentRepairItemId == item, token))
            Deny(403, "repair_binding_not_current");
        var native = await db.FieldInspectionTasks.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={task}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        var assignment = await db.FieldInspectionAssignments.FromSqlInterpolated($"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.AssignmentId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (native is null || native.ProjectId != project || native.RepairItemId != item ||
            native.TaskMode != (current.Mode == RepairMode.Normal ? "NORMAL" : "CONDITIONAL_FT") ||
            assignment is null || assignment.FieldInspectionTaskId != task || assignment.AssignedToUserId != actor ||
            assignment.Status != FieldInspectionAssignmentStatus.Active || assignment.EndedAt is not null)
            Deny(403, "repair_assignment_not_current");
    }
    private async Task<FieldInspectionOperationOrigin?> RepairOrigin(Guid project, Guid origin, string kind, string hash,
        Guid actor, Guid task, CancellationToken token)
    {
        if (origin == Guid.Empty) Deny(400, "validation_error");
        var row = await db.Set<FieldInspectionOperationOrigin>().FromSqlInterpolated($"SELECT * FROM [FieldInspectionOperationOrigins] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={project} AND [OriginId]={origin}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (row is not null && (row.Kind != kind || row.ContentHash != hash || row.OriginalActorId != actor || row.TaskId != task))
            Deny(409, "origin_content_conflict");
        if (row is null && await db.Set<OfflineOperationBinding>().FromSqlInterpolated($"SELECT * FROM [OfflineOperationBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={project} AND [OriginId]={origin}")
            .AsNoTracking().AnyAsync(token))
        {
            if (offlineRepairContext is null) Deny(403, "signed_origin_admission_required");
            var admitted = offlineRepairContext.Command;
            if (admitted.ProjectId != project || admitted.Operation.OriginId != origin || admitted.Operation.Kind != kind ||
                admitted.Operation.CorePayloadHash != hash || admitted.Operation.OriginalActorId != actor || admitted.Operation.TaskId != task)
                Deny(403, "signed_origin_admission_required");
        }
        return row;
    }
    private static RepairAssessmentFact AssessmentView(RepairMeasurementAssessment source) => new(source.Id, source.ItemId,
        source.TaskId, source.FirstStartId, source.SessionId, source.Stage, source.Readiness,
        JsonSerializer.Deserialize<string[]>(source.MissingReasonsJson, Json) ?? [], source.LocationState, source.ContentHash);
    private static RepairExecutionStartFact StartView(RepairExecutionStart source, Guid task) => new(source.Id, source.ItemId,
        task, source.AssessmentId, source.FirstStartId, source.ClaimedAt, source.ServerReceivedAt, source.VerifiedOriginalAt,
        source.TimeProvenance == RepairTimeProvenance.VerifiedOnline ? "SERVER_ONLINE" : source.TimeProvenance == RepairTimeProvenance.VerifiedOffline ? "VERIFIED_ORIGINAL_SOURCE" : "UNKNOWN");
    private async Task<RepairExecutionFinishFact> FinishView(RepairExecutionFinish source, Guid task, CancellationToken token)
    {
        var sync = await db.Set<DeadlineClock>().AsNoTracking().SingleOrDefaultAsync(row => row.TargetId == source.ItemId &&
            row.Kind == DeadlineClockKind.FinishedDataSync && row.OriginEventId == source.Id, token);
        return new(source.Id, source.ItemId, task, source.ExecutionStartId, source.ClaimedAt, source.ServerReceivedAt,
            source.VerifiedOriginalAt, source.TimeProvenance == RepairTimeProvenance.VerifiedOnline ? "SERVER_ONLINE" :
                source.TimeProvenance == RepairTimeProvenance.VerifiedOffline ? "VERIFIED_ORIGINAL_SOURCE" : "UNKNOWN", sync?.Id, sync?.OriginalDueAt);
    }
}
