using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Staff;

public enum StaffReadAccess { Allowed, Forbidden, ProjectNotFound }

public sealed record StaffUserRow(Guid UserId, string DisplayName, string? Email, string? PhoneNumber,
    UserRoleCode Role, UserStatus Status, byte[] RowVersion);

public sealed record StaffMemberRow(Guid MembershipId, Guid UserId, string DisplayName, UserRoleCode Role,
    bool IsPrimary, ProjectMemberStatus Status, DateOnly ValidFrom, DateOnly? ValidTo, byte[] RowVersion);

public interface IStaffReadRepository
{
    Task<StaffReadAccess> CheckAccessAsync(Guid actor, UserRoleCode role, Guid? project, DateOnly day, CancellationToken token);
    Task<IReadOnlyList<StaffUserRow>> UsersAsync(string? search, UserRoleCode? role, UserStatus? status, Guid? after, int take, CancellationToken token);
    Task<StaffUserRow?> AccountAsync(Guid user, CancellationToken token);
    Task<IReadOnlyList<StaffMemberRow>> MembersAsync(Guid project, DateOnly day, bool history, string? search,
        UserRoleCode? role, ProjectMemberStatus? status, Guid? after, int take, CancellationToken token);
    Task<StaffMemberRow?> PrimaryAsync(Guid project, DateOnly day, CancellationToken token);
}
