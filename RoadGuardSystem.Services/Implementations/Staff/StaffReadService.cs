using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Staff;
using RoadGuardSystem.Repositories.Staff;

namespace RoadGuardSystem.Services.Staff;

public sealed class StaffReadService(IStaffReadRepository repository, TimeProvider clock) : IStaffReadService
{
    public async Task<StaffReadResult<StaffPageResponseDto<StaffUserResponseDto>>> UsersAsync(Guid actor, UserRoleCode role,
        string? search, string? filterRole, string? status, int limit, string? cursor, CancellationToken token)
    {
        if (role != UserRoleCode.Supervisor) return new(StaffReadStatus.Forbidden);
        var asOf = clock.GetUtcNow();
        var access = await Access(actor, role, null, asOf, token);
        if (access != StaffReadStatus.Success) return new(access);
        search = Normalize(search); filterRole = Normalize(filterRole)?.ToUpperInvariant(); status = Normalize(status)?.ToUpperInvariant();
        var binding = Binding(actor, "users", search, filterRole, status);
        if (!ValidQuery(search, filterRole, limit, cursor, binding, out var selectedRole, out var after) || !TryStatus(status, out var selectedStatus))
            return new(StaffReadStatus.InvalidInput);
        var rows = await repository.UsersAsync(search, selectedRole, selectedStatus, after, limit + 1, token);
        var items = rows.Take(limit).Select(u => new StaffUserResponseDto(u.UserId, u.DisplayName, u.Email, u.PhoneNumber,
            RoleCode(u.Role), u.Status.ToString().ToUpperInvariant())).ToArray();
        return new(StaffReadStatus.Success, new(items, rows.Count > limit ? Cursor(rows[limit - 1].UserId, binding) : null, asOf));
    }

    public async Task<StaffReadResult<StaffAccountResponseDto>> AccountAsync(Guid actor, UserRoleCode role, Guid user, CancellationToken token)
    {
        if (role != UserRoleCode.Supervisor) return new(StaffReadStatus.Forbidden);
        var access = await Access(actor, role, null, clock.GetUtcNow(), token);
        if (access != StaffReadStatus.Success) return new(access);
        var row = await repository.AccountAsync(user, token);
        return row is null ? new(StaffReadStatus.NotFound) : new(StaffReadStatus.Success,
            new(row.UserId, row.DisplayName, RoleCode(row.Role), Convert.ToBase64String(row.RowVersion), row.Email, row.PhoneNumber));
    }

    public async Task<StaffReadResult<StaffPageResponseDto<ProjectMemberResponseDto>>> MembersAsync(Guid actor, UserRoleCode role, Guid project,
        string? search, string? filterRole, string? status, bool includeHistory, int limit, string? cursor, CancellationToken token)
    {
        if (role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager) || includeHistory && role != UserRoleCode.Supervisor)
            return new(StaffReadStatus.Forbidden);
        var asOf = clock.GetUtcNow();
        var access = await Access(actor, role, project, asOf, token);
        if (access != StaffReadStatus.Success) return new(access);
        search = Normalize(search); filterRole = Normalize(filterRole)?.ToUpperInvariant(); status = Normalize(status)?.ToUpperInvariant();
        var binding = Binding(actor, $"members:{project:D}:{includeHistory}", search, filterRole, status);
        ProjectMemberStatus? memberStatus = status switch { null => null, "ACTIVE" => ProjectMemberStatus.Active, "ENDED" => ProjectMemberStatus.Ended, _ => (ProjectMemberStatus)0 };
        if (!ValidQuery(search, filterRole, limit, cursor, binding, out var selectedRole, out var after) ||
            memberStatus == (ProjectMemberStatus)0 || status is not null && !includeHistory)
            return new(StaffReadStatus.InvalidInput);
        var rows = await repository.MembersAsync(project, DateOnly.FromDateTime(asOf.UtcDateTime), includeHistory,
            search, selectedRole, memberStatus, after, limit + 1, token);
        var items = rows.Take(limit).Select(m => new ProjectMemberResponseDto(m.MembershipId, m.UserId, m.DisplayName, RoleCode(m.Role),
            m.IsPrimary, m.Status.ToString().ToUpperInvariant(), m.ValidFrom, m.ValidTo, Convert.ToBase64String(m.RowVersion))).ToArray();
        return new(StaffReadStatus.Success, new(items, rows.Count > limit ? Cursor(rows[limit - 1].MembershipId, binding) : null, asOf));
    }

    public async Task<StaffReadResult<PrimaryProjectManagerResponseDto>> PrimaryAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token)
    {
        if (role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager)) return new(StaffReadStatus.Forbidden);
        var asOf = clock.GetUtcNow();
        var access = await Access(actor, role, project, asOf, token);
        if (access != StaffReadStatus.Success) return new(access);
        var m = await repository.PrimaryAsync(project, DateOnly.FromDateTime(asOf.UtcDateTime), token);
        return new(StaffReadStatus.Success, new(m is null ? null :
            new(m.MembershipId, m.UserId, m.DisplayName, m.ValidFrom, m.ValidTo, Convert.ToBase64String(m.RowVersion))));
    }

    private async Task<StaffReadStatus> Access(Guid actor, UserRoleCode role, Guid? project, DateTimeOffset asOf, CancellationToken token) =>
        await repository.CheckAccessAsync(actor, role, project, DateOnly.FromDateTime(asOf.UtcDateTime), token) switch
        {
            StaffReadAccess.Allowed => StaffReadStatus.Success,
            StaffReadAccess.ProjectNotFound => StaffReadStatus.NotFound,
            _ => StaffReadStatus.Forbidden
        };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool TryRole(string? value, out UserRoleCode? role)
    {
        role = value switch
        {
            null => null,
            "SUPERVISOR" => UserRoleCode.Supervisor,
            "PM" => UserRoleCode.ProjectManager,
            "OPERATOR" => UserRoleCode.DroneOperator,
            "CREW" => UserRoleCode.RepairCrew,
            "REPORTER" => UserRoleCode.Reporter,
            _ => UserRoleCode.Unknown
        };
        return role != UserRoleCode.Unknown;
    }
    private static bool TryStatus(string? value, out UserStatus? status)
    {
        status = value switch
        {
            null => null,
            "ACTIVE" => UserStatus.Active,
            "SUSPENDED" => UserStatus.Suspended,
            "PENDING" => UserStatus.Pending,
            _ => UserStatus.Unknown
        };
        return status != UserStatus.Unknown;
    }
    private static string RoleCode(UserRoleCode role) => role switch
    {
        UserRoleCode.DroneOperator => "OPERATOR",
        UserRoleCode.RepairCrew => "CREW",
        _ => role.ToDbCode()
    };
    private static string Binding(Guid actor, string scope, string? search, string? role, string? status) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new { actor, scope, search, role, status }))));
    private static string Cursor(Guid id, string binding) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"1|{id:D}|{binding}"));
    private static bool ValidQuery(string? search, string? filterRole, int limit, string? cursor, string binding,
        out UserRoleCode? role, out Guid? after)
    {
        after = null;
        if (!TryRole(filterRole, out role) || search?.Length > 255 || limit is < 1 or > 100) return false;
        if (cursor is null) return true;
        if (cursor.Length is 0 or > 256) return false;
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split('|');
            if (parts.Length != 3 || parts[0] != "1" || parts[2] != binding || !Guid.TryParseExact(parts[1], "D", out var id) || id == Guid.Empty) return false;
            after = id; return true;
        }
        catch (FormatException) { return false; }
    }
}
