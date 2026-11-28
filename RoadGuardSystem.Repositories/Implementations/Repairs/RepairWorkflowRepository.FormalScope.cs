using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository
{
    private async Task<RepairActualScope> RequestedScopeAsync(Guid project, Guid actor, Defect defect, string defectVersion, RepairActualScopeData physical, CancellationToken ct)
    {
        if (physical is null || physical.RouteVersionId != defect.RoadSectionVersionId) Deny(409, "geometry_version_mismatch");
        var route = await db.RoadSectionVersions.AsNoTracking().SingleAsync(row => row.Id == physical.RouteVersionId, ct);
        var road = await db.RoadSections.AsNoTracking().SingleAsync(row => row.Id == route.RoadSectionId, ct);
        if (road.ProjectId != project || physical.PhysicalRoadId != road.Id) Deny(409, "physical_road_source_mismatch");
        var locationInput = new RepairFieldTaskData(defect.Id, defectVersion, null, "REPORTER", route.Id,
            physical.SegmentSetId, physical.LayoutRevisionId, physical.SlabId, "POST_REPAIR", 1, "{}", null,
            actor, clock.GetUtcNow().AddDays(1));
        var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(row => row.RoadSectionVersionId == route.Id, ct);
        locationInput = locationInput with { CrsProfileRevisionId = native?.CrsProfileRevisionId };
        var check = await FieldCore().ValidateRepairLocationPinsInTransactionAsync(project, locationInput, ct);
        if (check.Code is not null) Deny(check.Status, check.Code);
        var frame = "h4-frame-v1:" + SourceHash(new
        {
            routeVersionId = route.Id,
            physical.SegmentSetId,
            physical.LayoutRevisionId,
            physical.SlabId,
            crsProfileRevisionId = native?.CrsProfileRevisionId
        });
        RepairActualScope scope;
        try
        {
            scope = RepairActualScope.Create(Guid.NewGuid(), road.Id, frame, road.Code,
            physical.FromMeters, physical.ToMeters, physical.OffsetFromMeters, physical.OffsetToMeters);
        }
        catch (ArgumentException) { Deny(400, "validation_error"); throw; }
        return scope;
    }

    public async Task<RepairWorkflowResult> FormalScopeEligibilityAsync(Guid actor, UserRoleCode role, Guid project, Guid defectId,
        RepairActualScopeData? requested, CancellationToken token)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await CurrentProducerAuthority(actor, role, UserRoleCode.ProjectManager, project, token);
                await GuardDefectResource(project, defectId, token);
                FormalRepairEligibilityFact value;
                if (requested is null) value = new(null, "scope_required", "A concrete pinned formal scope is required; no eligibility has been established.");
                else
                {
                    var defect = await FreshAnchor(project, defectId, token);
                    var scope = await RequestedScopeAsync(project, actor, defect, VersionOf(defect), requested, token);
                    var obligations = await db.Set<RepairObligation>().AsNoTracking().Include(x => x.Scope)
                        .Where(x => x.DefectId == defectId && x.Kind == RepairObligationKind.FormalRepair).ToArrayAsync(token);
                    var conflict = FormalRepairScopeAdmission.Evaluate(scope, obligations);
                    if (conflict is null) value = new(true, null, "FORMAL_SCOPE_AVAILABLE_NOT_EXECUTION_AUTHORITY");
                    else
                    {
                        var visible = await ObligationResponsibilityScope.ResolveAsync(db, conflict.Obligation.Id, conflict.Obligation.ProjectId, token) == project;
                        var current = visible && conflict.Obligation.CurrentRepairItemId is Guid currentItem
                            ? await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == currentItem && x.ObligationId == conflict.Obligation.Id, token) : null;
                        var package = visible ? await db.Set<RepairPackage>().AsNoTracking().Where(x => x.Obligations.Any(o => o.Id == conflict.Obligation.Id)).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(token) : null;
                        value = new(false, conflict.Code, conflict.Reason, visible ? conflict.Obligation.Id : null, package, current?.Id,
                            current is null ? null : current.Mode == RepairMode.Normal ? "NORMAL" : "FAST_TRACK", current?.State.ToString());
                    }
                }
                await tx.CommitAsync(token);
                return new RepairWorkflowResult(200, Value: value);
            });
        }
        catch (Denied error) { db.ChangeTracker.Clear(); return error.Result; }
        catch (ArgumentException) { return new(400, "validation_error"); }
    }
}
