using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    private async Task RecordRepairFirstStartAsync(FieldInspectionTask task, FieldTaskStartOrigin first, CancellationToken token)
    {
        var item = await db.Set<RepairItem>().FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={task.RepairItemId}")
            .SingleOrDefaultAsync(token);
        if (item is null || item.ProjectId != first.ProjectId || item.DefectId != task.DefectId || item.CurrentBindingId is null ||
            item.SupersededByItemId is not null) Deny(409, "repair_binding_not_current");
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId, token);
        if (binding.TaskId != task.Id || binding.AssignmentId != first.AssignmentId || binding.CrewId != first.OriginalActorId ||
            binding.ItemId != item.Id || binding.Mode != item.Mode || binding.PlanHash != item.ProposalPlanHash)
            Deny(409, "repair_binding_not_current");
        var obligation = await db.Set<RepairObligation>().FromSqlInterpolated($"SELECT * FROM [RepairObligations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item.ObligationId}")
            .SingleAsync(token);
        if (obligation.CurrentRepairItemId != item.Id) Deny(409, "repair_binding_not_current");
        var changed = false;
        if (obligation.OriginalCrewFirstStartId is null)
        {
            db.Entry(obligation).Property(row => row.OriginalCrewFirstStartId).CurrentValue = first.Id;
            changed = true;
        }
        if (item.Mode == RepairMode.FastTrack)
        {
            if (obligation.OriginalCrewFirstStartId != first.Id || binding.AuthorizationId is null)
                Deny(409, "fast_track_original_start_locked");
            var authorization = await db.Set<RepairExecutionAuthorization>().SingleAsync(row => row.Id == binding.AuthorizationId, token);
            if (authorization.ProjectId != binding.ProjectId || authorization.DefectId != binding.DefectId ||
                authorization.TaskId != task.Id || authorization.AssignmentId != first.AssignmentId ||
                authorization.CrewId != first.OriginalActorId || authorization.PolicyRevisionId != binding.PolicyRevisionId ||
                authorization.LocationVersion != binding.LocationVersion || authorization.Permission != RepairTaskMode.ConditionalFastTrack)
                Deny(409, "repair_authorization_scope_conflict");
            changed |= authorization.FirstStartOriginId is null;
            authorization.RecordFirstStart(first.Id, first.ContentHash, first.ClaimedAt, first.ServerReceivedAt, RepairTimeProvenance.Uncertain);
            if (first.VerifiedOriginalAt is DateTimeOffset verified)
            {
                var provenance = first.TimeProvenance == "SERVER_ONLINE" ? RepairTimeProvenance.VerifiedOnline : RepairTimeProvenance.VerifiedOffline;
                authorization.VerifyOriginalStart(first.Id, first.ContentHash, first.ClaimedAt, verified, provenance);
                var existing = await db.Set<DeadlineClock>().SingleOrDefaultAsync(row => row.TargetId == item.Id &&
                    row.Kind == DeadlineClockKind.FastTrackExecution, token);
                if (existing is null)
                    db.Add(DeadlineClock.Create(Guid.NewGuid(), item.ProjectId, DeadlineClockKind.FastTrackExecution, item.Id, first.Id, verified));
                else if (existing.OriginEventId != first.Id || existing.OriginAt != verified) Deny(409, "fast_track_clock_origin_locked");
            }
        }
        if (changed)
        {
            var packageId = db.Entry(item).Property<Guid?>("PackageId").CurrentValue;
            if (packageId is null) Deny(409, "repair_package_source_missing");
            var package = await db.Set<RepairPackage>().FromSqlInterpolated($"SELECT * FROM [RepairPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={packageId}")
                .SingleAsync(token);
            db.Entry(package).Property<long>("MutationRevision").CurrentValue++;
        }
    }
}
