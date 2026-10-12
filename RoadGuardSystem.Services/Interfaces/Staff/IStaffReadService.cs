using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Staff;

namespace RoadGuardSystem.Services.Staff;

public enum StaffReadStatus { Success, Forbidden, NotFound, InvalidInput }
public sealed record StaffReadResult<T>(StaffReadStatus Status, T? Value = default);

public interface IStaffReadService
{
    Task<StaffReadResult<StaffPageResponseDto<StaffUserResponseDto>>> UsersAsync(Guid actor, UserRoleCode role,
        string? search, string? filterRole, string? status, int limit, string? cursor, CancellationToken token);
    Task<StaffReadResult<StaffAccountResponseDto>> AccountAsync(Guid actor, UserRoleCode role, Guid user, CancellationToken token);
    Task<StaffReadResult<StaffPageResponseDto<ProjectMemberResponseDto>>> MembersAsync(Guid actor, UserRoleCode role, Guid project,
        string? search, string? filterRole, string? status, bool includeHistory, int limit, string? cursor, CancellationToken token);
    Task<StaffReadResult<PrimaryProjectManagerResponseDto>> PrimaryAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
}
