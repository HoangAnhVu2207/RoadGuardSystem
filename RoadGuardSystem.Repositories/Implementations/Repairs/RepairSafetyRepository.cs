using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed class RepairSafetyRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : IRepairSafetyRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class Denied(int status, string code) : Exception { public SafetyResult Result { get; } = new(status, code); }
    private sealed class ExistingReceipt : Exception { }

    public async Task<SafetyResult> ReadAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid packageId,
        Guid itemId, Guid measureId, CancellationToken cancellationToken)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                await Guard(actorId, role, projectId, packageId, itemId, measureId, false, cancellationToken);
                var monitoring = await Monitoring(measureId, cancellationToken);
                var fact = View(monitoring);
                await tx.CommitAsync(cancellationToken);
                return new SafetyResult(200, Value: fact);
            });
        }
        catch (Denied denied) { db.ChangeTracker.Clear(); return denied.Result; }
    }

    public Task<SafetyResult> CreateAsync(SafetyCreateCommand command, CancellationToken cancellationToken)
        => Produce(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId, null,
            "h4.repair.safety.create.v1", command.Key, command, async ct =>
            {
                var (item, binding, package) = await BoundNormal(command.ProjectId, command.PackageId, command.ItemId, ct);
                if (command.Role != UserRoleCode.ProjectManager || command.ResponsibleActorId != binding.CrewId ||
                    item.Mode != RepairMode.Normal || item.ApprovedBy is null || item.ApprovedPlanHash != item.ProposalPlanHash ||
                    item.State is RepairItemState.Confirmed or RepairItemState.Cancelled)
                    Deny(403, "safety_scope_not_authorized");
                if (Version(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                var formal = package.Obligations.SingleOrDefault(row => row.Id == command.FormalObligationId);
                var safety = package.Obligations.SingleOrDefault(row => row.Id == command.SafetyObligationId);
                if (formal is null || safety is null || formal.Id != item.ObligationId ||
                    formal.Kind != RepairObligationKind.FormalRepair || safety.Kind != RepairObligationKind.TemporarySafety ||
                    safety.IsResolved || formal.IsResolved || safety.ProjectId != item.ProjectId || safety.DefectId != item.DefectId ||
                    !Within(safety.Scope, formal.Scope)) Deny(409, "safety_obligation_source_conflict");
                if (await db.Set<RepairSafetyMonitoring>().AnyAsync(row => row.SafetyObligationId == safety.Id, ct))
                    Deny(409, "safety_measure_already_exists");
                var measure = TemporarySafetyMeasure.Create(Guid.NewGuid(), item.ProjectId, item.DefectId, formal.Id,
                    binding.CrewId, command.CheckSchedule, command.ReplacementCondition, command.RemovalCondition);
                var monitoring = RepairSafetyMonitoring.Create(measure, safety, formal);
                var now = clock.GetUtcNow();
                db.Add(measure);
                await db.SaveChangesAsync(ct);
                db.Add(monitoring);
                await db.SaveChangesAsync(ct);
                await ActionSource(measure.Id, command.ProjectId, item.Id, binding.Id, command.ActorId,
                    "ASSIGNED", now, measure.Id, command.Reason, "safety.measure_assigned.v1", "SAFETY_ASSIGNED", ct);
                Audit(command.ActorId, "repair_safety_assigned", measure.Id, command.Reason, new
                { itemId = item.Id, bindingId = binding.Id, safetyObligationId = safety.Id, formalObligationId = formal.Id });
                await db.SaveChangesAsync(ct);
                return (measure.Id, View(monitoring));
            }, cancellationToken);

    public Task<SafetyResult> InstallAsync(SafetyInstallCommand command, CancellationToken cancellationToken)
        => Produce(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId, command.MeasureId,
            "h4.repair.safety.install.v1", command.Key, command, async ct =>
            {
                var (monitoring, item, binding) = await ActiveMonitoring(command.ProjectId, command.PackageId,
                    command.ItemId, command.MeasureId, ct);
                if (command.Role != UserRoleCode.RepairCrew || command.ActorId != binding.CrewId ||
                    command.ActorId != monitoring.Measure.ResponsibleActorId) Deny(403, "safety_responsibility_not_current");
                if (Version(monitoring) != command.ExpectedVersion) Deny(409, "concurrency_conflict");
                if (monitoring.Measure.InstalledAt is not null) Deny(409, "safety_already_installed");
                var now = clock.GetUtcNow(); var eventId = Guid.NewGuid();
                monitoring.Measure.Install(eventId, command.ActorId, now, command.FirstCheckDueAt);
                db.Add(DeadlineClock.CreateFirstSafetyCheck(Guid.NewGuid(), command.ProjectId, monitoring.MeasureId,
                    eventId, now, command.FirstCheckDueAt));
                await db.SaveChangesAsync(ct);
                await ActionSource(monitoring.MeasureId, command.ProjectId, item.Id, binding.Id, command.ActorId,
                    "INSTALLED", now, eventId, command.Reason, null, null, ct);
                Audit(command.ActorId, "repair_safety_installed", monitoring.MeasureId, command.Reason,
                    new { eventId, command.FirstCheckDueAt });
                await db.SaveChangesAsync(ct);
                return (eventId, View(monitoring));
            }, cancellationToken);

    public Task<SafetyResult> CheckAsync(SafetyCheckCommand command, CancellationToken cancellationToken)
        => Produce(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId, command.MeasureId,
            "h4.repair.safety.check.v1", command.Key, command, async ct =>
            {
                var (monitoring, item, binding) = await ActiveMonitoring(command.ProjectId, command.PackageId,
                    command.ItemId, command.MeasureId, ct);
                if (command.Role != UserRoleCode.RepairCrew || command.ActorId != binding.CrewId ||
                    command.ActorId != monitoring.Measure.ResponsibleActorId) Deny(403, "safety_responsibility_not_current");
                if (Version(monitoring) != command.ExpectedVersion) Deny(409, "concurrency_conflict");
                if (!Enum.TryParse<RepairSafetyCheckResult>(command.Result, false, out var result) ||
                    !Enum.IsDefined(result) || result == RepairSafetyCheckResult.Unknown ||
                    command.EvidenceLinkIds is null || command.EvidenceLinkIds.Length == 0 ||
                    command.EvidenceLinkIds.Length > 20 || command.EvidenceLinkIds.Distinct().Count() != command.EvidenceLinkIds.Length)
                    Deny(400, "validation_error");
                var evidence = await VerifiedEvidence(binding, command.EvidenceLinkIds, ct);
                var now = clock.GetUtcNow(); var checkId = Guid.NewGuid();
                monitoring.RecordVerifiedCheck(checkId, command.ActorId, now, result, command.Findings,
                    command.EvidenceLinkIds, evidence);
                // Insert immutable check/evidence before setting its constrained current-head FK.
                var head = monitoring.CurrentCheckId;
                db.Entry(monitoring).Property(row => row.CurrentCheckId).CurrentValue = null;
                await db.SaveChangesAsync(ct);
                db.Entry(monitoring).Property(row => row.CurrentCheckId).CurrentValue = head;
                var first = await db.Set<DeadlineClock>().SingleOrDefaultAsync(row => row.ProjectId == command.ProjectId &&
                    row.TargetId == monitoring.MeasureId && row.Kind == DeadlineClockKind.FirstSafetyCheck &&
                    row.OriginEventId == monitoring.Measure.InstallationEventId, ct);
                if (first is null) Deny(409, "safety_first_check_clock_missing");
                if (first.CompletedAt is null) first.Complete(now);
                if (result == RepairSafetyCheckResult.Danger)
                {
                    var warningId = Guid.NewGuid();
                    var warning = monitoring.ReceiveDangerWarning(warningId, checkId, now, command.Findings);
                    db.Add(DeadlineClock.Create(Guid.NewGuid(), command.ProjectId, DeadlineClockKind.DangerAcknowledgment,
                        monitoring.MeasureId, warning.Id, warning.ServerReceivedAt));
                    await db.SaveChangesAsync(ct);
                    await ActionSource(monitoring.MeasureId, command.ProjectId, item.Id, binding.Id, command.ActorId,
                        "WARNING", now, warning.Id, command.Findings, "safety.warning.v1", "WARNING", ct);
                }
                Audit(command.ActorId, "repair_safety_checked", monitoring.MeasureId, command.Findings,
                    new { checkId, result = result.ToString(), command.EvidenceLinkIds });
                await db.SaveChangesAsync(ct);
                return (checkId, View(monitoring));
            }, cancellationToken);

    public Task<SafetyResult> AcknowledgeAsync(SafetyAckCommand command, CancellationToken cancellationToken)
        => Produce(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId, command.MeasureId,
            "h4.repair.safety.ack.v1", command.Key, command, async ct =>
            {
                var (monitoring, _, binding) = await ActiveMonitoring(command.ProjectId, command.PackageId,
                    command.ItemId, command.MeasureId, ct);
                if (command.Role != UserRoleCode.RepairCrew || command.ActorId != binding.CrewId ||
                    command.ActorId != monitoring.Measure.ResponsibleActorId) Deny(403, "safety_responsibility_not_current");
                if (Version(monitoring) != command.ExpectedVersion) Deny(409, "concurrency_conflict");
                var warning = monitoring.Warnings.SingleOrDefault(row => row.Id == command.WarningId);
                if (warning is null || warning.ResponsibleActorId != command.ActorId ||
                    monitoring.Acknowledgements.Any(row => row.WarningId == warning.Id))
                    Deny(409, "safety_warning_not_current");
                var deadline = await db.Set<DeadlineClock>().Include(row => row.Breaches)
                    .SingleOrDefaultAsync(row => row.ProjectId == command.ProjectId &&
                    row.TargetId == monitoring.MeasureId && row.Kind == DeadlineClockKind.DangerAcknowledgment &&
                    row.OriginEventId == warning.Id && row.OriginAt == warning.ServerReceivedAt, ct);
                if (deadline is null || deadline.CompletedAt is not null) Deny(409, "safety_warning_clock_conflict");
                var now = clock.GetUtcNow(); var ack = monitoring.AcknowledgeDanger(Guid.NewGuid(), warning.Id,
                    command.ActorId, now, command.Reason);
                deadline.Acknowledge(ack.Id, command.ActorId, now);
                Audit(command.ActorId, "repair_safety_danger_acknowledged", monitoring.MeasureId, command.Reason,
                    new { acknowledgementId = ack.Id, warningId = warning.Id, ack.AfterOriginalDue });
                await db.SaveChangesAsync(ct);
                return (ack.Id, View(monitoring));
            }, cancellationToken);

    private async Task<SafetyResult> Produce(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid? measure, string operation, string key, object command,
        Func<CancellationToken, Task<(Guid Id, SafetyFact Fact)>> apply, CancellationToken token)
    {
        try
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                new { schemaVersion = 1, command }, Json))).ToLowerInvariant();
            async Task GuardReceipt(CancellationToken ct)
            {
                await Guard(actor, role, project, package, item, measure, true, ct);
                var stored = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row =>
                    row.ActorUserId == actor && row.ProjectId == project && row.Operation == operation &&
                    row.IdempotencyKey == key, ct);
                if (stored is not null)
                {
                    var owned = operation switch
                    {
                        "h4.repair.safety.create.v1" => await db.Set<RepairSafetyActionSource>().AsNoTracking()
                            .AnyAsync(row => row.MeasureId == stored.OperationId && row.ProjectId == project &&
                                row.ItemId == item && row.Kind == "ASSIGNED", ct),
                        "h4.repair.safety.install.v1" => await db.Set<TemporarySafetyMeasure>().AsNoTracking()
                            .AnyAsync(row => row.Id == measure && row.InstallationEventId == stored.OperationId, ct),
                        "h4.repair.safety.check.v1" => await db.Set<RepairSafetyCheck>().AsNoTracking()
                            .AnyAsync(row => row.Id == stored.OperationId && row.MeasureId == measure, ct),
                        "h4.repair.safety.ack.v1" => await db.Set<RepairDangerAcknowledgement>().AsNoTracking()
                            .AnyAsync(row => row.Id == stored.OperationId && row.MonitoringId == measure, ct),
                        _ => false
                    };
                    if (!owned) Deny(403, "stored_receipt_access_forbidden");
                }
            }
            var receipt = await receipts.ExecuteSerializableAsync(actor, project, operation, key, fingerprint,
                async ct =>
                {
                    await GuardReceipt(ct);
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == actor &&
                        row.ProjectId == project && row.Operation == operation && row.IdempotencyKey == key, ct))
                        throw new ExistingReceipt();
                    var result = await apply(ct);
                    await GuardReceipt(ct);
                    return (result.Id, JsonSerializer.Serialize(result.Fact, Json));
                }, token, receiptAccessGuard: GuardReceipt);
            if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var fact = JsonSerializer.Deserialize<SafetyFact>(receipt.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable safety outcome missing.");
            return new(receipt.Status == IdempotencyOperationStatus.Replayed ? 200 : 201,
                Value: fact, Replayed: receipt.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await Produce(actor, role, project, package, item,
            measure, operation, key, command, apply, token); }
        catch (Denied denied) { db.ChangeTracker.Clear(); return denied.Result; }
        catch (ArgumentException) { db.ChangeTracker.Clear(); return new(400, "validation_error"); }
        catch (InvalidOperationException) { db.ChangeTracker.Clear(); return new(409, "invalid_state_transition"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }

    private async Task Guard(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid? measure, bool requireActive, CancellationToken ct)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew))
            Deny(403, "access_forbidden");
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, ct);
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, ct) ||
            !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == project && row.UserId == actor &&
                row.RoleCode == role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day &&
                (row.ValidTo == null || row.ValidTo >= day), ct)) Deny(403, "access_forbidden");
        var scope = await db.Set<RepairPackage>().FromSqlInterpolated($"SELECT * FROM [RepairPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={package}")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        if (scope is null || scope.ProjectId != project ||
            !await db.Set<RepairItem>().FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item}")
                .AsNoTracking().AnyAsync(row => row.ProjectId == project && row.DefectId == scope.DefectId &&
                    EF.Property<Guid?>(row, "PackageId") == package, ct)) Deny(404, "not_found");
        if (measure is Guid id)
        {
            var monitoring = await db.Set<RepairSafetyMonitoring>().Include(row => row.Measure)
                .SingleOrDefaultAsync(row => row.MeasureId == id, ct);
            if (monitoring is null) Deny(404, "not_found");
            if (monitoring.Measure.ProjectId != project || monitoring.Measure.DefectId != scope.DefectId ||
                monitoring.FormalObligationId != (await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == item)
                    .Select(row => row.ObligationId).SingleAsync(ct)) ||
                !await db.Set<RepairSafetyActionSource>().AsNoTracking().AnyAsync(row => row.MeasureId == id &&
                    row.ProjectId == project && row.ItemId == item && row.Kind == "ASSIGNED", ct) ||
                !await db.Set<RepairObligation>().AsNoTracking().AnyAsync(row => row.Id == monitoring.SafetyObligationId &&
                    row.ProjectId == project && row.DefectId == scope.DefectId &&
                    row.Kind == RepairObligationKind.TemporarySafety &&
                    row.EffectiveResolutionDecisionId == null, ct)) Deny(404, "not_found");
            if (role == UserRoleCode.RepairCrew && actor != monitoring.Measure.ResponsibleActorId)
                Deny(403, "safety_responsibility_not_current");
        }
        if (requireActive && !await db.Projects.AsNoTracking().AnyAsync(row => row.Id == project &&
            row.Status == ProjectStatus.Active, ct)) Deny(409, "project_not_active");
    }

    private async Task<(RepairItem Item, RepairFieldTaskBinding Binding, RepairPackage Package)> BoundNormal(
        Guid project, Guid packageId, Guid itemId, CancellationToken ct)
    {
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageId, ct);
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == itemId, ct);
        if (item.CurrentBindingId is not Guid bindingId) throw new Denied(409, "repair_binding_not_current");
        if (item.ProjectId != project || item.DefectId != package.DefectId ||
            item.SupersededByItemId is not null || item.Mode != RepairMode.Normal || item.CrewId is null ||
            !await db.Set<RepairObligation>().AnyAsync(row => row.Id == item.ObligationId &&
                row.CurrentRepairItemId == item.Id, ct)) Deny(409, "repair_binding_not_current");
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == bindingId, ct);
        var task = await db.FieldInspectionTasks.SingleAsync(row => row.Id == binding.TaskId, ct);
        var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.Id == binding.AssignmentId, ct);
        if (binding.ItemId != item.Id || binding.ProjectId != project || binding.ObligationId != item.ObligationId ||
            binding.CrewId != item.CrewId || binding.Mode != RepairMode.Normal ||
            task.ProjectId != project || task.RepairItemId != item.Id || task.TaskMode != "NORMAL" ||
            assignment.FieldInspectionTaskId != task.Id || assignment.AssignedToUserId != binding.CrewId ||
            assignment.Status != FieldInspectionAssignmentStatus.Active || assignment.EndedAt is not null)
            Deny(409, "repair_assignment_not_current");
        return (item, binding, package);
    }

    private async Task<(RepairSafetyMonitoring Monitoring, RepairItem Item, RepairFieldTaskBinding Binding)> ActiveMonitoring(
        Guid project, Guid package, Guid itemId, Guid measure, CancellationToken ct)
    {
        var monitoring = await Monitoring(measure, ct);
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == itemId, ct);
        var assigned = await db.Set<RepairSafetyActionSource>().SingleOrDefaultAsync(row => row.MeasureId == measure &&
            row.ProjectId == project && row.ItemId == itemId && row.Kind == "ASSIGNED", ct);
        if (assigned is null) Deny(409, "safety_assignment_source_conflict");
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == assigned.BindingId, ct);
        if (monitoring.Measure.ProjectId != project || monitoring.Measure.DefectId != item.DefectId ||
            monitoring.FormalObligationId != item.ObligationId || item.Mode != RepairMode.Normal ||
            item.ApprovedBy is null || item.ApprovedPlanHash != item.ProposalPlanHash ||
            binding.ItemId != item.Id || binding.ProjectId != project || binding.ObligationId != item.ObligationId ||
            binding.CrewId != monitoring.Measure.ResponsibleActorId || binding.Mode != RepairMode.Normal ||
            !await db.Set<RepairObligation>().AnyAsync(row => row.Id == monitoring.SafetyObligationId &&
                row.ProjectId == project && row.DefectId == item.DefectId &&
                row.Kind == RepairObligationKind.TemporarySafety &&
                row.EffectiveResolutionDecisionId == null, ct))
            Deny(409, "safety_responsibility_source_conflict");
        return (monitoring, item, binding);
    }

    private Task<RepairSafetyMonitoring> Monitoring(Guid measure, CancellationToken ct) => db.Set<RepairSafetyMonitoring>()
        .Include(row => row.Measure).Include(row => row.Checks).ThenInclude(row => row.Evidence)
        .Include(row => row.Warnings).Include(row => row.Acknowledgements)
        .SingleAsync(row => row.MeasureId == measure, ct);

    private async Task<RepairSafetyEvidenceReference[]> VerifiedEvidence(RepairFieldTaskBinding binding, Guid[] ids, CancellationToken ct)
    {
        var links = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(row => ids.Contains(row.Id))
            .ToArrayAsync(ct);
        if (links.Length != ids.Length || links.Select(row => row.FileId).Distinct().Count() != links.Length)
            Deny(409, "safety_evidence_source_conflict");
        var refs = new List<RepairSafetyEvidenceReference>(links.Length);
        var field = new FieldInspectionWorkflowRepository(db, receipts, clock);
        foreach (var id in ids)
        {
            var link = links.Single(row => row.Id == id);
            if (link.FileId is not Guid fileId) throw new Denied(403, "safety_evidence_scope_forbidden");
            if (link.ProjectId != binding.ProjectId || link.TaskId != binding.TaskId ||
                link.AssignmentId != binding.AssignmentId || link.Purpose != "MEASUREMENT")
                Deny(403, "safety_evidence_scope_forbidden");
            ReporterFileFacts file;
            try
            {
                file = await field.ValidateRepairEvidenceInTransactionAsync(binding.TaskId,
                    new RepairFieldEvidenceData(link.CaptureOriginId, fileId, link.Purpose,
                        link.DeclaredChecksum, link.MediaType, null, null), binding.CrewId, ct);
            }
            catch (RepairFieldCoreRejectedException rejection) { Deny(rejection.Status, rejection.Code ?? "safety_evidence_source_conflict"); throw; }
            DateTimeOffset? capturedAt = null;
            using (var facts = JsonDocument.Parse(link.CaptureFactsJson))
                if (facts.RootElement.TryGetProperty("declared", out var declaration) &&
                    declaration.TryGetProperty("capturedAt", out var captured) &&
                    captured.ValueKind == JsonValueKind.String && captured.TryGetDateTimeOffset(out var parsed)) capturedAt = parsed;
            refs.Add(new RepairSafetyEvidenceReference(fileId, file.FileVersion, file.Checksum, RepairEvidencePurpose.Safety,
                true, true, "FIELD_MEASUREMENT", link.Id, capturedAt, null, false, file.OwnerId));
        }
        return refs.ToArray();
    }

    private async Task ActionSource(Guid measure, Guid project, Guid item, Guid binding, Guid actor,
        string kind, DateTimeOffset at, Guid origin, string reason, string? messageType, string? action, CancellationToken ct)
    {
        var eventId = Guid.NewGuid();
        db.Add(new RepairSafetyActionSource(eventId, measure, project, item, binding, actor, kind, at,
            origin, reason));
        if (messageType is not null && action is not null)
            db.OutboxMessages.Add(OutboxMessage.Create(eventId, messageType, at, null, JsonSerializer.Serialize(
                new H6StoredEvent(1, eventId, action, project, "TemporarySafetyMeasure", measure, origin,
                    at, eventId, kind == "ASSIGNED" ? null : (Guid?)await Responsible(measure, ct)), Json)));
    }

    private async Task<Guid> Responsible(Guid measure, CancellationToken ct)
        => await db.Set<TemporarySafetyMeasure>().Where(row => row.Id == measure)
            .Select(row => row.ResponsibleActorId).SingleAsync(ct);

    private void Audit(Guid actor, string action, Guid measure, string reason, object source)
        => db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), action,
            "TemporarySafetyMeasure", measure, null, JsonSerializer.Serialize(source, Json), reason,
            "h4.repair.safety.v1", measure, ["safety"]));

    private SafetyFact View(RepairSafetyMonitoring monitoring)
        => new(monitoring.MeasureId, monitoring.Measure.ProjectId, monitoring.Measure.DefectId,
            monitoring.FormalObligationId, monitoring.SafetyObligationId, monitoring.Measure.ResponsibleActorId,
            monitoring.Measure.InstalledAt, monitoring.Measure.FirstCheckDueAt, monitoring.CurrentCheckId,
            monitoring.Checks.OrderBy(row => row.At).Select(row => new SafetyCheckFact(row.Id, row.ActorId, row.At,
                row.Result.ToString(), row.Findings, row.EvidenceIds.ToArray())).ToArray(),
            monitoring.Warnings.OrderBy(row => row.ServerReceivedAt).Select(row => new SafetyWarningFact(row.Id,
                row.SourceId, row.ResponsibleActorId, row.ServerReceivedAt, row.OriginalAcknowledgementDueAt,
                row.Reason)).ToArray(),
            monitoring.Acknowledgements.OrderBy(row => row.At).Select(row => new SafetyAckFact(row.Id,
                row.WarningId, row.ActorId, row.At, row.AfterOriginalDue)).ToArray(), Version(monitoring));

    private string Version<T>(T entity) where T : class
        => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);

    private static bool Within(RepairActualScope safety, RepairActualScope formal)
        => safety.PhysicalRoadId == formal.PhysicalRoadId && safety.LocationVersion == formal.LocationVersion &&
            safety.From >= formal.From && safety.To <= formal.To && safety.OffsetFrom >= formal.OffsetFrom &&
            safety.OffsetTo <= formal.OffsetTo;

    [DoesNotReturn]
    private static void Deny(int status, string code) => throw new Denied(status, code);
}
