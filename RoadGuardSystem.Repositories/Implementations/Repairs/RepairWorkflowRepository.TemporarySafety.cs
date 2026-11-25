using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository
{
    private const string SafetyObligationAddOperation = "h4.repair.temporary-safety-add.v1";

    public async Task<RepairWorkflowResult> ListTemporarySafetyAsync(Guid actor, UserRoleCode role, Guid project,
        Guid packageId, Guid itemId, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) return new(403, "access_forbidden");
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await CurrentProducerAuthority(actor, role, role, project, token);
                await GuardItemResource(project, packageId, itemId, token);
                var package = await LockedPackage(packageId, token);
                var item = package.Items.Single(x => x.Id == itemId);
                var formal = package.Obligations.Single(x => x.Id == item.ObligationId);
                var candidates = package.Obligations.Where(x => x.Kind == RepairObligationKind.TemporarySafety && !x.IsResolved &&
                    x.ProjectId == item.ProjectId && x.DefectId == item.DefectId && RepairSafetyRepository.Within(x.Scope, formal.Scope)).ToArray();
                var ids = candidates.Select(x => x.Id).ToArray();
                var used = await db.Set<RepairSafetyMonitoring>().Where(x => ids.Contains(x.SafetyObligationId))
                    .Select(x => x.SafetyObligationId).ToArrayAsync(token);
                var owners = await db.Set<ObligationResponsibility>().FromSqlRaw("SELECT * FROM [ObligationResponsibilities] WITH (UPDLOCK,HOLDLOCK)")
                    .AsNoTracking().Where(x => ids.Contains(x.ObligationId)).ToDictionaryAsync(x => x.ObligationId, token);
                var rows = new List<TemporarySafetyObligationFact>();
                if (formal.Kind == RepairObligationKind.FormalRepair && !formal.IsResolved && formal.CurrentRepairItemId == item.Id &&
                    item.Mode == RepairMode.Normal && item.SupersededByItemId is null && item.State is not (RepairItemState.Confirmed or RepairItemState.Cancelled))
                    foreach (var safety in candidates.OrderBy(x => x.Id))
                    {
                        var owner = owners.GetValueOrDefault(safety.Id);
                        if (owner is not null && owner.OriginProjectId != safety.ProjectId) Deny(409, "safety_obligation_source_conflict");
                        var effective = owner?.CurrentProjectId ?? safety.ProjectId;
                        if (effective == project && !used.Contains(safety.Id)) rows.Add(SafetySource(safety, formal, package, item, effective));
                    }
                var view = await SafetySources(package, item, formal, project, rows.ToArray(), token);
                await tx.CommitAsync(token);
                return new RepairWorkflowResult(200, Value: view, Version: view.PackageVersion);
            });
        }
        catch (Denied e) { db.ChangeTracker.Clear(); return e.Result; }
    }

    public Task<RepairWorkflowResult> AddTemporarySafetyAsync(TemporarySafetyObligationCommand command, CancellationToken token)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.ProjectManager, command.ProjectId, command.Key,
            SafetyObligationAddOperation, command, ct => GuardItemResource(command.ProjectId, command.PackageId, command.ItemId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                var item = package.Items.Single(x => x.Id == command.ItemId);
                var formal = package.Obligations.Single(x => x.Id == item.ObligationId);
                var defect = await FreshAnchor(package.ProjectId, package.DefectId, ct, item.Id);
                if (VersionOf(package) != command.ExpectedPackageVersion) Deny(409, "concurrency_conflict");
                if (package.IsComplete || formal.IsResolved || formal.Kind != RepairObligationKind.FormalRepair ||
                    formal.CurrentRepairItemId != item.Id || item.Mode != RepairMode.Normal || item.SupersededByItemId is not null ||
                    item.State is RepairItemState.Confirmed or RepairItemState.Cancelled)
                    Deny(409, "temporary_safety_not_allowed");
                var physical = command.Scope;
                if (physical.RouteVersionId != defect.RoadSectionVersionId) Deny(409, "geometry_version_mismatch");
                var route = await db.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == physical.RouteVersionId, ct);
                var road = await db.RoadSections.AsNoTracking().SingleAsync(x => x.Id == route.RoadSectionId, ct);
                if (road.ProjectId != package.ProjectId || physical.PhysicalRoadId != road.Id) Deny(409, "physical_road_source_mismatch");
                var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
                var check = await FieldCore().ValidateRepairLocationPinsInTransactionAsync(package.ProjectId,
                    new(defect.Id, VersionOf(defect), null, "REPORTER", route.Id, physical.SegmentSetId, physical.LayoutRevisionId,
                        physical.SlabId, "POST_REPAIR", 1, "{}", null, command.ActorId, clock.GetUtcNow().AddDays(1),
                        CrsProfileRevisionId: native?.CrsProfileRevisionId), ct);
                if (check.Code is not null) Deny(check.Status, check.Code);
                var frame = "h4-frame-v1:" + SourceHash(new
                {
                    routeVersionId = route.Id,
                    physical.SegmentSetId,
                    physical.LayoutRevisionId,
                    physical.SlabId,
                    crsProfileRevisionId = native?.CrsProfileRevisionId
                });
                var scope = RepairActualScope.Create(Guid.NewGuid(), road.Id, frame, road.Code,
                    physical.FromMeters, physical.ToMeters, physical.OffsetFromMeters, physical.OffsetToMeters);
                if (!RepairSafetyRepository.Within(scope, formal.Scope)) Deny(409, "safety_obligation_source_conflict");
                var active = await db.Set<RepairObligation>().Include(x => x.Scope).Where(x => x.DefectId == item.DefectId &&
                    x.Kind == RepairObligationKind.TemporarySafety && x.EffectiveResolutionDecisionId == null && x.SafetyResolutionId == null).ToArrayAsync(ct);
                if (active.Any(x => scope.Compare(x.Scope) != RepairScopeComparison.Disjoint)) Deny(409, "active_scope_conflict");
                var safety = RepairObligation.Create(Guid.NewGuid(), package.ProjectId, package.DefectId,
                    RepairObligationKind.TemporarySafety, command.Mandatory, scope);
                package.AddTemporarySafety(safety);
                Touch(package);
                db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, clock.GetUtcNow(), "repair_temporary_safety_added",
                    "RepairObligation", safety.Id, null, JsonSerializer.Serialize(new
                    {
                        safetyObligationId = safety.Id,
                        formalObligationId = formal.Id,
                        itemId = item.Id,
                        packageId = package.Id,
                        scopeId = scope.Id,
                        command.Mandatory
                    }, Json),
                    command.Reason, SafetyObligationAddOperation, null,
                    ["safetyObligationId", "formalObligationId", "itemId", "packageId", "scopeId", "mandatory"]));
                await db.SaveChangesAsync(ct);
                if (command.ProjectId != package.ProjectId)
                {
                    // A new, immutable creation action establishes the initial owner. It is not a transfer.
                    var owner = await db.Set<ObligationResponsibility>().AsNoTracking().SingleAsync(x => x.ObligationId == formal.Id, ct);
                    var creation = LD06LifecycleAction.Create(Guid.NewGuid(), command.ProjectId, command.ActorId,
                        LD06ActionKind.InitializeTemporarySafetyResponsibility, clock.GetUtcNow(), command.Reason,
                        JsonSerializer.Serialize(new { formalObligationId = formal.Id, itemId = item.Id, packageId = package.Id }, Json),
                        source: owner.AcceptanceActionId, defect: item.DefectId, obligation: safety.Id,
                        scopeHash: LD06LifecycleAction.HashScope(safety));
                    db.Add(creation);
                    await db.SaveChangesAsync(ct);
                    db.Add(ObligationResponsibility.InitializeSafety(safety.Id, package.ProjectId, command.ProjectId, creation.Id));
                    await db.SaveChangesAsync(ct);
                }
                var view = await SafetySources(package, item, formal, command.ProjectId,
                    [SafetySource(safety, formal, package, item, command.ProjectId)], ct);
                return (safety.Id, new ProducingOutcome<TemporarySafetyObligationSourcesFact>(view, view.PackageVersion));
            }, token);

    private static SafetyObligationScopeFact SafetyScope(RepairActualScope x)
        => new(x.PhysicalRoadId, x.LocationVersion, x.RouteLabel, x.From, x.To, x.OffsetFrom, x.OffsetTo);
    private TemporarySafetyObligationFact SafetySource(RepairObligation safety, RepairObligation formal,
        RepairPackage package, RepairItem item, Guid owner)
        => new(safety.Id, formal.Id, package.Id, item.Id, item.DefectId, "TEMPORARY_SAFETY", safety.Mandatory,
            SafetyScope(safety.Scope), safety.IsResolved, safety.ProjectId, owner, VersionOf(safety));
    private async Task<TemporarySafetyObligationSourcesFact> SafetySources(RepairPackage package, RepairItem item,
        RepairObligation formal, Guid owner, TemporarySafetyObligationFact[] rows, CancellationToken ct)
    {
        var binding = await (from b in db.Set<RepairFieldTaskBinding>()
                             join task in db.FieldInspectionTasks on b.TaskId equals task.Id
                             where b.Id == item.CurrentBindingId && b.ItemId == item.Id && b.ObligationId == formal.Id
                             select new RepairTaskBindingFact(item.Id, formal.Id, task.Id, b.AssignmentId, task.TaskMode,
                                 b.AuthorizationId, b.PolicyRevisionId, task.RoadSectionVersionId, task.SegmentSetId, task.LayoutRevisionId,
                                 task.MapPublicationId, task.CrsProfileRevisionId, task.SlabId, formal.OriginalCrewFirstStartId)).SingleOrDefaultAsync(ct);
        return new(package.Id, item.Id, item.DefectId, formal.Id, owner, VersionOf(package), VersionOf(item),
            SafetyScope(formal.Scope), binding, rows);
    }
}
