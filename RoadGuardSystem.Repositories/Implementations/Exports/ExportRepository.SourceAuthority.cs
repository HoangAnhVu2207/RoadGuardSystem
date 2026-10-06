using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Exports;

public sealed partial class ExportRepository
{
    public async Task<bool> CanReadSnapshotSourcesAsync(Guid actorId, Guid projectId,
        ExportSourceAuthorityQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.ManifestProjectId != projectId) return false;
        var repairRefs = query.RepairSources;
        var caseFacts = query.CaseFacts;
        var privateRepair = repairRefs.Length != 0 || query.HasAvailableRepairMetric;
        // Legacy general reports keep their existing scope; private source inventory is independently protected.
        if (!privateRepair && caseFacts is null) return true;
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        if (!await _db.Users.AsNoTracking().AnyAsync(user => user.Id == actorId &&
            user.Status == UserStatus.Active && !user.MustChangePassword &&
            (user.RoleCode == UserRoleCode.ProjectManager || user.RoleCode == UserRoleCode.Supervisor) &&
            _db.Roles.Any(role => role.Code == user.RoleCode && role.IsActive) &&
            _db.ProjectMembers.Any(member => member.UserId == actorId && member.ProjectId == projectId &&
                member.RoleCode == user.RoleCode && member.Status == ProjectMemberStatus.Active &&
                member.ValidFrom <= today && (member.ValidTo == null || member.ValidTo >= today)), ct)) return false;
        foreach (var source in repairRefs.DistinctBy(source => (source.Kind, source.Id)))
        {
            if (source.Id == Guid.Empty) return false;
            var exists = source.Kind switch
            {
                "RepairItem" => await _db.Set<RepairItem>().AsNoTracking().AnyAsync(item =>
                    item.Id == source.Id && item.ProjectId == projectId, ct),
                "RepairObligation" => await _db.Set<RepairObligation>().AsNoTracking().AnyAsync(obligation =>
                    obligation.Id == source.Id && obligation.ProjectId == projectId, ct),
                "RepairDecision" => await (from decision in _db.Set<RepairDecision>().AsNoTracking()
                    join item in _db.Set<RepairItem>().AsNoTracking() on decision.ItemId equals item.Id
                    join obligation in _db.Set<RepairObligation>().AsNoTracking() on decision.ObligationId equals obligation.Id
                    where decision.Id == source.Id && item.ProjectId == projectId && obligation.ProjectId == projectId &&
                        item.ObligationId == obligation.Id && decision.DefectId == item.DefectId && decision.DefectId == obligation.DefectId
                    select decision.Id).AnyAsync(ct),
                _ => false
            };
            if (!exists) return false;
        }
        if (caseFacts is not null)
        {
            if (caseFacts.ProjectId != projectId) return false;
            var cases = caseFacts.CaseIds.Distinct().ToArray();
            var defects = caseFacts.DefectIds.Distinct().ToArray();
            if (await _db.IncidentCases.AsNoTracking().CountAsync(value => cases.Contains(value.Id) && value.ProjectId == projectId, ct) != cases.Length ||
                await _db.Defects.AsNoTracking().CountAsync(value => defects.Contains(value.Id) && value.ProjectId == projectId, ct) != defects.Length) return false;
        }
        // Historical snapshot versions intentionally need not equal current rowversions or effective decision heads.
        return true;
    }
}
