using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository : IOfflineRepairCommandAdapter
{
    private sealed record OfflineRepairContext(OfflineRepairCoreCommand Command);
    private OfflineRepairContext? offlineRepairContext;

    public string ComputeCoreHash(OfflineOperationData operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var repair = operation.Repair ?? throw new ArgumentException("Typed repair operation required.");
        if (repair.ResourceId == Guid.Empty || operation.FieldStart is not null || operation.FieldSubmission is not null ||
            operation.FieldAction is not null) throw new ArgumentException("One explicit repair operation required.");
        return (operation.Kind, repair.Action) switch
        {
            ("REPAIR_ASSESSMENT", "assessment") when repair.Assessment is not null && repair.Start is null && repair.Finish is null &&
                repair.Assessment.OriginId == operation.OriginId =>
                RepairCommandCoreHash.Assessment(operation.TaskId, repair.ResourceId, operation.OriginalActorId, repair.Assessment),
            ("REPAIR_EXECUTION_START", "execution-start") when repair.Start is not null && repair.Assessment is null && repair.Finish is null &&
                repair.Start.OriginId == operation.OriginId =>
                RepairCommandCoreHash.ExecutionStart(operation.TaskId, repair.ResourceId, operation.OriginalActorId, repair.Start),
            ("REPAIR_EXECUTION_FINISH", "execution-finish") when repair.Finish is not null && repair.Assessment is null && repair.Start is null &&
                repair.Finish.OriginId == operation.OriginId =>
                RepairCommandCoreHash.ExecutionFinish(operation.TaskId, repair.ResourceId, operation.OriginalActorId, repair.Finish),
            _ => throw new ArgumentException("Repair kind, action and immutable origin must match.")
        };
    }

    public async Task<OfflineRepairSnapshotFacts?> ReadSnapshotInTransactionAsync(OfflineRepairSnapshotQuery query, CancellationToken cancellationToken)
    {
        var token = cancellationToken;
        RequireOfflineTransaction();
        try
        {
            await CurrentProducerAuthority(query.CallerId, query.CallerRole, UserRoleCode.RepairCrew, query.ProjectId, token);
            var source = await db.Set<RepairFieldTaskBinding>().AsNoTracking().Where(row => row.ProjectId == query.ProjectId &&
                row.TaskId == query.TaskId).SingleOrDefaultAsync(token);
            if (source is null) return null;
            var packageId = await db.Set<RepairItem>().Where(row => row.Id == source.ItemId)
                .Select(row => EF.Property<Guid?>(row, "PackageId")).SingleAsync(token);
            if (packageId is null) return null;
            await GuardExecutionResource(query.CallerId, query.ProjectId, packageId.Value, source.ItemId, query.TaskId, token);
            var item = await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == source.ItemId, token);
            var version = Convert.ToBase64String(await db.Set<RepairItem>().Where(row => row.Id == item.Id)
                .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync(token));
            var policy = await db.Set<RepairPolicyRevision>().AsNoTracking().Include(row => row.Measurements).Include(row => row.Revocations)
                .SingleOrDefaultAsync(row => row.Id == source.PolicyRevisionId && row.ProjectId == query.ProjectId, token);
            var obligation = await db.Set<RepairObligation>().AsNoTracking().SingleAsync(row => row.Id == item.ObligationId, token);
            return new(item.Id, source.TaskId, source.AssignmentId, source.CrewId, version,
                source.AuthorizationId, source.PolicyRevisionId, source.PolicyContentHash,
                new
                {
                    schemaVersion = 1,
                    packageId,
                    itemId = item.Id,
                    itemVersion = version,
                    item.ObligationId,
                    mode = item.Mode.ToString(),
                    state = item.State.ToString(),
                    item.RepairPlan,
                    item.ChecklistVersion,
                    item.ProposalPlanHash,
                    item.ApprovedPlanHash,
                    item.ApprovedBy,
                    item.ApprovedAt,
                    bindingId = source.Id,
                    source.TaskId,
                    source.AssignmentId,
                    source.CrewId,
                    source.AuthorizationId,
                    source.PolicyRevisionId,
                    source.PolicyContentHash,
                    source.LocationVersion,
                    item.CurrentAssessmentId,
                    item.CurrentExecutionStartId,
                    item.CurrentExecutionFinishId,
                    obligation.OriginalCrewFirstStartId,
                    policy = policy is null ? null : new
                    {
                        policy.Id,
                        policy.Revision,
                        policy.DefectTypeCode,
                        policy.ChecklistVersion,
                        policy.PublishedBy,
                        policy.PublishedAt,
                        policy.IsRevoked,
                        measurements = policy.Measurements.OrderBy(row => row.Code, StringComparer.Ordinal).ToArray(),
                        stopConditions = policy.StopConditions.Order(StringComparer.Ordinal).ToArray()
                    },
                    eligibility = "UNKNOWN_OWNER_MAPPING",
                    executeAuthority = "REVALIDATE_CURRENT_SOURCE_AT_ADMISSION"
                });
        }
        catch (Denied) { return null; }
    }

    public async Task<OfflineRepairEffect> ApplyInTransactionAsync(OfflineRepairCoreCommand command, CancellationToken cancellationToken)
    {
        var token = cancellationToken;
        RequireOfflineTransaction();
        if (offlineRepairContext is not null) throw new InvalidOperationException("Nested offline repair execution is not supported.");
        ArgumentNullException.ThrowIfNull(command);
        await ValidateOfflineRepair(command, token);
        var repair = command.Operation.Repair!;
        var package = await db.Set<RepairItem>().Where(row => row.Id == repair.ResourceId && row.ProjectId == command.ProjectId)
            .Select(row => EF.Property<Guid?>(row, "PackageId")).SingleOrDefaultAsync(token);
        if (package is null) throw new OfflineAdmissionRejectedException(404, "not_found");
        offlineRepairContext = new(command);
        try
        {
            var operation = command.Operation;
            var key = "offline:" + command.Admission.AdmissionId.ToString("D");
            var result = operation.Kind switch
            {
                "REPAIR_ASSESSMENT" => await AssessAsync(new(operation.OriginalActorId, UserRoleCode.RepairCrew, command.ProjectId,
                    package.Value, repair.ResourceId, operation.TaskId, repair.Assessment!, key, command.ExpectedItemVersion), token),
                "REPAIR_EXECUTION_START" => await StartExecutionAsync(new(operation.OriginalActorId, UserRoleCode.RepairCrew, command.ProjectId,
                    package.Value, repair.ResourceId, operation.TaskId, repair.Start!, key, command.ExpectedItemVersion), token),
                "REPAIR_EXECUTION_FINISH" => await FinishExecutionAsync(new(operation.OriginalActorId, UserRoleCode.RepairCrew, command.ProjectId,
                    package.Value, repair.ResourceId, operation.TaskId, repair.Finish!, key, command.ExpectedItemVersion), token),
                _ => throw new OfflineAdmissionRejectedException(400, "validation_error")
            };
            if (result.Code is not null) throw new OfflineAdmissionRejectedException(result.Status, result.Code);
            await ValidateOfflineRepair(command, token);
            var canonical = await db.Set<FieldInspectionOperationOrigin>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.ProjectId == command.ProjectId && row.OriginId == operation.OriginId, token);
            if (canonical is null || canonical.EffectId != command.Admission.EffectId || canonical.Kind != operation.Kind ||
                canonical.ContentHash != operation.CorePayloadHash || canonical.OriginalActorId != operation.OriginalActorId ||
                canonical.TaskId != operation.TaskId)
                throw new OfflineAdmissionRejectedException(409, "offline_effect_identity_mismatch");
            DateTimeOffset? verifiedFinish = operation.Kind == "REPAIR_EXECUTION_FINISH"
                ? await db.Set<RepairExecutionFinish>().Where(row => row.Id == canonical.EffectId && row.ProjectId == command.ProjectId)
                    .Select(row => row.VerifiedOriginalAt).SingleAsync(token) : null;
            return new(result.Status, null, canonical.EffectId, canonical.Id, result.Version, result.Value, verifiedFinish);
        }
        finally { offlineRepairContext = null; }
    }

    private async Task<OfflineFieldAdmissionFacts> ValidateOfflineRepair(OfflineRepairCoreCommand command, CancellationToken token)
    {
        var operation = command.Operation;
        if (ComputeCoreHash(operation) != operation.CorePayloadHash || command.Admission.Kind != operation.Kind ||
            command.Admission.ProjectId != command.ProjectId || command.Admission.TaskId != operation.TaskId ||
            command.Admission.AssignmentId != operation.AssignmentId || command.Admission.EffectId != operation.EffectId ||
            command.Admission.OriginalActorId != operation.OriginalActorId || command.Admission.SourceDeviceId != operation.SourceDeviceId ||
            command.Admission.CallerId != command.CallerId || command.Admission.CallerRole != command.CallerRole ||
            command.Admission.CorePayloadHash != operation.CorePayloadHash || command.Admission.SnapshotId != operation.SnapshotId)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        var caller = new FieldAdmissionContext(command.CallerId, command.CallerRole, operation.OriginalActorId,
            command.Admission.GrantId is null ? "SYNC" : "HANDOVER", false, command.Admission.GrantId, command.Admission.AdmissionId);
        var actual = await new OfflineAdmissionValidator(db, clock).ValidateRepairAsync(operation, command.ProjectId, caller, token);
        if (actual is null || actual != command.Admission) throw new OfflineAdmissionRejectedException(403, "offline_admission_proof_unavailable");
        return actual;
    }

    private async Task<RepairWorkflowResult> ApplyOfflineProductionAsync<T>(Guid actor, UserRoleCode role,
        UserRoleCode required, Guid project, string operation, Func<CancellationToken, Task> target,
        Func<CancellationToken, Task<(Guid Id, ProducingOutcome<T> Outcome)>> apply, CancellationToken token)
    {
        RequireOfflineTransaction();
        var context = offlineRepairContext ?? throw new InvalidOperationException("Offline repair context required.");
        var expected = context.Command.Operation.Kind switch
        {
            "REPAIR_ASSESSMENT" => AssessmentOperation,
            "REPAIR_EXECUTION_START" => ExecutionStartOperation,
            "REPAIR_EXECUTION_FINISH" => ExecutionFinishOperation,
            _ => ""
        };
        if (operation != expected || actor != context.Command.Operation.OriginalActorId || project != context.Command.ProjectId)
            throw new OfflineAdmissionRejectedException(409, "origin_content_conflict");
        Task CurrentAdmissionActor(CancellationToken ct)
            => context.Command.Admission.GrantId is null
                ? CurrentProducerAuthority(actor, role, required, project, ct)
                : CurrentProducerAuthority(context.Command.CallerId, context.Command.CallerRole,
                    context.Command.CallerRole, project, ct);
        await ValidateOfflineRepair(context.Command, token);
        // A fully validated DATA handover uses the current recipient's authority; the original Crew identity
        // still scopes the unchanged binding/assignment/execution checks and every recorded effect.
        await CurrentAdmissionActor(token);
        await target(token);
        var effect = await apply(token);
        if (effect.Id != context.Command.Admission.EffectId)
            throw new OfflineAdmissionRejectedException(409, "offline_effect_identity_mismatch");
        await ValidateOfflineRepair(context.Command, token);
        await CurrentAdmissionActor(token);
        await target(token);
        return new(201, Value: effect.Outcome.Value, Version: effect.Outcome.Version);
    }

    private Guid RepairEffectId() => offlineRepairContext?.Command.Admission.EffectId ?? Guid.NewGuid();
    private Guid? RepairOriginDevice(Guid? declared)
    {
        if (offlineRepairContext is null) return declared;
        var device = offlineRepairContext.Command.Admission.SourceDeviceId;
        if (declared is not null && declared != device) Deny(409, "origin_content_conflict");
        return device;
    }
    private (DateTimeOffset? VerifiedAt, RepairTimeProvenance Provenance) RepairExecutionTime(DateTimeOffset received)
    {
        if (offlineRepairContext is null) return (received, RepairTimeProvenance.VerifiedOnline);
        // A signed payload proves identity/integrity only. New imports remain uncertain; independent reconciliation owns historical proof.
        return (null, RepairTimeProvenance.Uncertain);
    }
    private void RequireOfflineTransaction()
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned repair transaction required.");
    }
}
