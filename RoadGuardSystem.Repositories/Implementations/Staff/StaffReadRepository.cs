using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Staff;

public sealed class StaffReadRepository(RoadGuardDbContext db, IdempotencyOperationService idempotency, TimeProvider clock) : IStaffReadRepository
{
    private const string AddOperation = "ProjectMemberAdded";
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

    public async Task<IReadOnlyList<StaffUserRow>> EligibleAsync(Guid project, string? search,
        UserRoleCode? role, Guid? after, int take, CancellationToken token)
    {
        var users = db.Users.AsNoTracking().Where(u => u.Status == UserStatus.Active && !u.MustChangePassword &&
            (u.RoleCode == UserRoleCode.DroneOperator || u.RoleCode == UserRoleCode.RepairCrew) &&
            (role == null || u.RoleCode == role) && db.Roles.Any(r => r.Code == u.RoleCode && r.IsActive) &&
            !db.ProjectMembers.Any(m => m.ProjectId == project && m.UserId == u.Id &&
                m.Status == ProjectMemberStatus.Active));
        if (search is not null) users = users.Where(u => u.DisplayName.Contains(search));
        if (after is Guid id) users = users.Where(u => u.Id.CompareTo(id) > 0);
        return await users.OrderBy(u => u.Id).Take(take).Select(u => new StaffUserRow(u.Id, u.DisplayName,
            null, null, u.RoleCode, u.Status, u.RowVersion)).ToListAsync(token);
    }

    public async Task<StaffMemberWriteResult> AddMemberAsync(Guid actor, Guid project, Guid user, string key, CancellationToken token)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{project:D}:{user:D}"))).ToLowerInvariant();
        try
        {
            var result = await idempotency.ExecuteSerializableAsync(actor, project, AddOperation, key, fingerprint,
                async ct =>
                {
                    // Serialize all membership writes for this project/user pair, including distinct retry keys.
                    var resource = $"project-member:{project:N}:{user:N}";
                    await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction'; IF @result < 0 THROW 51000, 'Membership lock unavailable', 1;", ct);
                    await CheckWriteAccessAsync(actor, project, ct);
                    var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                    var target = await db.Users.AsNoTracking().Where(u => u.Id == user)
                        .Select(u => new { u.DisplayName, u.RoleCode, u.Status, u.MustChangePassword }).SingleOrDefaultAsync(ct);
                    if (target is null || target.Status != UserStatus.Active || target.MustChangePassword ||
                        target.RoleCode is not (UserRoleCode.DroneOperator or UserRoleCode.RepairCrew) ||
                        !await db.Roles.AnyAsync(r => r.Code == target.RoleCode && r.IsActive, ct))
                        throw new StaffWriteException(StaffMemberWriteStatus.UserNotFound);
                    if (await db.ProjectMembers.AnyAsync(m => m.ProjectId == project && m.UserId == user &&
                        m.Status == ProjectMemberStatus.Active, ct))
                        throw new StaffWriteException(StaffMemberWriteStatus.Conflict);
                    var member = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = user,
                        RoleCode = target.RoleCode, IsPrimary = false, Status = ProjectMemberStatus.Active, ValidFrom = day };
                    db.ProjectMembers.Add(member);
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), "project_member_added",
                        "ProjectMember", member.Id, null, null, null, "PROJECT_API", null));
                    await db.SaveChangesAsync(ct);
                    var row = new StaffMemberRow(member.Id, user, target.DisplayName, member.RoleCode, false,
                        member.Status, member.ValidFrom, member.ValidTo, member.RowVersion);
                    return (member.Id, JsonSerializer.Serialize(row));
                }, token, ct => CheckWriteAccessAsync(actor, project, ct));
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(StaffMemberWriteStatus.KeyConflict);
            var stored = JsonSerializer.Deserialize<StaffMemberRow>(result.OutcomeJson)
                ?? throw new InvalidOperationException("Stored membership outcome is invalid.");
            return new(StaffMemberWriteStatus.Success, stored);
        }
        catch (StaffWriteException ex)
        {
            return new(ex.Status);
        }
    }

    private async Task CheckWriteAccessAsync(Guid actor, Guid project, CancellationToken token)
    {
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await db.Users.AnyAsync(u => u.Id == actor && u.RoleCode == UserRoleCode.ProjectManager &&
            u.Status == UserStatus.Active && !u.MustChangePassword &&
            db.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), token))
            throw new StaffWriteException(StaffMemberWriteStatus.Forbidden);
        var projectRow = await db.Projects.AsNoTracking().Where(p => p.Id == project).Select(p => new { p.Status }).SingleOrDefaultAsync(token);
        if (projectRow is null) throw new StaffWriteException(StaffMemberWriteStatus.ProjectNotFound);
        if (projectRow.Status != ProjectStatus.Active) throw new StaffWriteException(StaffMemberWriteStatus.Conflict);
        if (!await db.ProjectMembers.AnyAsync(m => m.ProjectId == project && m.UserId == actor &&
            m.RoleCode == UserRoleCode.ProjectManager && m.Status == ProjectMemberStatus.Active &&
            m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day), token))
            throw new StaffWriteException(StaffMemberWriteStatus.Forbidden);
    }

    private sealed class StaffWriteException(StaffMemberWriteStatus status) : Exception
    {
        public StaffMemberWriteStatus Status { get; } = status;
    }
}
