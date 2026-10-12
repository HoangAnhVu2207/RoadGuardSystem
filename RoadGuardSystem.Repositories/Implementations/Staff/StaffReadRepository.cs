using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Staff;

public sealed class StaffReadRepository(RoadGuardDbContext db) : IStaffReadRepository
{
    public async Task<StaffReadAccess> CheckAccessAsync(Guid actor, UserRoleCode role, Guid? project, DateOnly day, CancellationToken token)
    {
        if (role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager) ||
            !await db.Users.AnyAsync(u => u.Id == actor && u.RoleCode == role && u.Status == UserStatus.Active &&
                !u.MustChangePassword && db.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), token))
            return StaffReadAccess.Forbidden;
        if (project is not Guid projectId) return role == UserRoleCode.Supervisor ? StaffReadAccess.Allowed : StaffReadAccess.Forbidden;
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, token)) return StaffReadAccess.ProjectNotFound;
        if (role == UserRoleCode.Supervisor) return StaffReadAccess.Allowed;
        return await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == actor &&
            m.RoleCode == UserRoleCode.ProjectManager && m.Status == ProjectMemberStatus.Active &&
            m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day), token)
            ? StaffReadAccess.Allowed : StaffReadAccess.Forbidden;
    }

    public async Task<IReadOnlyList<StaffUserRow>> UsersAsync(string? search, UserRoleCode? role, UserStatus? status, Guid? after, int take, CancellationToken token)
    {
        var users = db.Users.AsNoTracking().Where(u => role != null ? u.RoleCode == role :
            u.RoleCode == UserRoleCode.Supervisor || u.RoleCode == UserRoleCode.ProjectManager ||
            u.RoleCode == UserRoleCode.DroneOperator || u.RoleCode == UserRoleCode.RepairCrew);
        if (status is not null) users = users.Where(u => u.Status == status);
        if (search is not null) users = users.Where(u => u.DisplayName.Contains(search) || u.Email != null && u.Email.Contains(search));
        if (after is Guid id) users = users.Where(u => u.Id.CompareTo(id) > 0);
        return await users.OrderBy(u => u.Id).Take(take).Select(u => new StaffUserRow(u.Id, u.DisplayName,
            u.Email, u.PhoneNumber, u.RoleCode, u.Status, u.RowVersion)).ToListAsync(token);
    }

    public Task<StaffUserRow?> AccountAsync(Guid user, CancellationToken token) => db.Users.AsNoTracking()
        .Where(u => u.Id == user).Select(u => new StaffUserRow(u.Id, u.DisplayName, u.Email, u.PhoneNumber,
            u.RoleCode, u.Status, u.RowVersion)).SingleOrDefaultAsync(token);

    public async Task<IReadOnlyList<StaffMemberRow>> MembersAsync(Guid project, DateOnly day, bool history, string? search,
        UserRoleCode? role, ProjectMemberStatus? status, Guid? after, int take, CancellationToken token)
    {
        var members = from m in db.ProjectMembers.AsNoTracking()
                      join u in db.Users.AsNoTracking() on m.UserId equals u.Id
                      where m.ProjectId == project && (history || m.Status == ProjectMemberStatus.Active &&
                          m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day) &&
                          u.Status == UserStatus.Active && u.RoleCode == m.RoleCode)
                      select new { Member = m, Name = u.DisplayName };
        if (search is not null) members = members.Where(x => x.Name.Contains(search));
        if (role is not null) members = members.Where(x => x.Member.RoleCode == role);
        if (status is not null) members = members.Where(x => x.Member.Status == status);
        if (after is Guid id) members = members.Where(x => x.Member.Id.CompareTo(id) > 0);
        return await members.OrderBy(x => x.Member.Id).Take(take).Select(x => new StaffMemberRow(x.Member.Id,
            x.Member.UserId, x.Name, x.Member.RoleCode, x.Member.IsPrimary, x.Member.Status,
            x.Member.ValidFrom, x.Member.ValidTo, x.Member.RowVersion)).ToListAsync(token);
    }

    public Task<StaffMemberRow?> PrimaryAsync(Guid project, DateOnly day, CancellationToken token) =>
        (from m in db.ProjectMembers.AsNoTracking()
         join u in db.Users.AsNoTracking() on m.UserId equals u.Id
         where m.ProjectId == project && m.IsPrimary && m.RoleCode == UserRoleCode.ProjectManager &&
             m.Status == ProjectMemberStatus.Active && m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day) &&
             u.Status == UserStatus.Active && u.RoleCode == UserRoleCode.ProjectManager
         orderby m.ValidFrom descending, m.Id
         select new StaffMemberRow(m.Id, m.UserId, u.DisplayName, m.RoleCode, m.IsPrimary, m.Status,
             m.ValidFrom, m.ValidTo, m.RowVersion)).FirstOrDefaultAsync(token);
}
