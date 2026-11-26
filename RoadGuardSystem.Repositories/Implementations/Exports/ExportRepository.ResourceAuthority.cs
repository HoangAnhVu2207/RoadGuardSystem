using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Exports;

public sealed partial class ExportRepository
{
    public async Task<bool> HasRepairResourceScopeAsync(Guid actor, Guid project, CancellationToken token)
    {
        var ids = await _db.RepairItems.AsNoTracking().Where(i => i.ProjectId == project && i.ApprovedBy == actor ||
            _db.Set<NotificationSupervisorHead>().Any(h => h.ItemId == i.Id && h.ProjectId == project && h.SupervisorId == actor && h.RevokedAt == null))
            .Select(i => i.Id).ToArrayAsync(token);
        foreach (var id in ids)
            if (await CanReadRepairItemsAsync(actor, project, [id], token)) return true;
        return false;
    }

    public async Task<bool> CanReadRepairItemsAsync(Guid actor, Guid project, Guid[] items, CancellationToken token)
    {
        if (_db.Database.CurrentTransaction is null)
            return await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token);
                var result = await CanReadRepairItemsAsync(actor, project, items, token);
                await transaction.CommitAsync(token); return result;
            });
        if (items.Length is < 1 or > 100 || items.Contains(Guid.Empty)) return false;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actor, token);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword ||
            user.RoleCode is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) ||
            !await _db.Roles.AnyAsync(r => r.Code == user.RoleCode && r.IsActive, token) ||
            !await _db.Projects.AnyAsync(p => p.Id == project, token)) return false;
        var member = await HasCurrentProjectMembershipAsync(actor, project, token);
        if (user.RoleCode == UserRoleCode.ProjectManager && !member) return false;
        foreach (var id in items.Distinct())
        {
            var item = await _db.RepairItems.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, token);
            if (item is null || item.SupersededByItemId is not null) return false;
            var obligation = await _db.RepairObligations.AsNoTracking().SingleOrDefaultAsync(o =>
                o.Id == item.ObligationId && o.ProjectId == item.ProjectId && o.DefectId == item.DefectId &&
                (o.CurrentRepairItemId == item.Id || member && o.CurrentRepairItemId == null), token);
            var packageId = await _db.RepairItems.AsNoTracking().Where(i => i.Id == id)
                .Select(i => EF.Property<Guid?>(i, "PackageId")).SingleAsync(token);
            if (obligation is null || packageId is null || !await _db.RepairPackages.AnyAsync(p =>
                p.Id == packageId && p.ProjectId == item.ProjectId, token)) return false;
            if (await ObligationResponsibilityScope.ResolveAsync(_db, obligation.Id, obligation.ProjectId, token) != project) return false;
            if (user.RoleCode == UserRoleCode.ProjectManager || member) continue;
            if (item.State == RepairItemState.Cancelled || obligation.Kind != RepairObligationKind.FormalRepair) return false;
            // New owner-authorized proof: effective NORMAL plan approval by this Supervisor,
            // or an exact active B2 assignment for this FT item. Role/global reporting alone never grants export.
            var normal = item.Mode == RepairMode.Normal && item.ApprovedBy == actor &&
                item.ApprovedPlanHash is not null && item.ApprovedPlanHash == item.ProposalPlanHash &&
                item.State != RepairItemState.Cancelled && project == item.ProjectId;
            var assigned = item.Mode == RepairMode.FastTrack && await _db.Set<NotificationSupervisorHead>().AnyAsync(h =>
                h.ItemId == item.Id && h.PackageId == packageId && h.ProjectId == project && h.SupervisorId == actor && h.RevokedAt == null &&
                _db.Set<NotificationSupervisorGeneration>().Any(g => g.Id == h.AssignmentId && g.ItemId == item.Id &&
                    g.ProjectId == project && g.SupervisorId == actor), token);
            if (!normal && !assigned) return false;
        }
        return true;
    }

    public async Task<ReporterFileFacts?> ReadRepairFileAsync(Guid actor, Guid project, Guid[] items, Guid fileId, CancellationToken token)
    {
        if (_db.Database.CurrentTransaction is null)
            return await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token);
                var result = await ReadRepairFileAsync(actor, project, items, fileId, token);
                await transaction.CommitAsync(token); return result;
            });
        if (!await CanReadRepairItemsAsync(actor, project, items, token)) return null;
        var facts = await new AnhHuyFactsRepository(_db).GetFileAsync(fileId, token);
        if (facts is not { State: "VERIFIED" } || facts.ProjectId is null || facts.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT")) return null;
        var scope = await _db.FileScopes.AsNoTracking().SingleAsync(s => s.FileId == fileId, token);
        var file = await _db.Files.AsNoTracking().SingleAsync(f => f.Id == fileId, token);
        foreach (var itemId in items)
        {
            var work = await _db.RepairItems.AsNoTracking().Include(i => i.Attempts).Include(i => i.Decisions).SingleAsync(i => i.Id == itemId, token);
            if (work.ProjectId != facts.ProjectId || scope.ProjectId != work.ProjectId) continue;
            var task = await _db.FieldInspectionTasks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == scope.TargetId &&
                t.ProjectId == work.ProjectId && t.DefectId == work.DefectId && (t.RepairItemId == null || t.RepairItemId == work.Id), token);
            if (task is null) continue;
            var evidence = work.Attempts.Where(a => a.ProjectId == work.ProjectId && a.ObligationId == work.ObligationId &&
                a.DefectId == work.DefectId && a.TaskId == task.Id && a.CrewId == file.UploadedByUserId)
                .SelectMany(a => a.Evidence).Concat(work.Decisions.SelectMany(d => d.Basis?.Evidence ?? []));
            if (evidence.Any(e => e.FileId == fileId && e.Verified && e.Related && e.FileVersion == facts.FileVersion &&
                e.Hash == facts.Checksum && e.Purpose.ToString().Equals(facts.Purpose, StringComparison.OrdinalIgnoreCase))) return facts;
        }
        return null;
    }
}
