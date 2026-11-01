using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Staff;

namespace RoadGuardSystem.Services.Staff;

public enum StaffReadStatus { Success, Forbidden, NotFound, UserNotFound, InvalidInput, Conflict, KeyConflict }
public sealed record StaffReadResult<T>(StaffReadStatus Status, T? Value = default);

public interface IStaffReadService
{
    Task<StaffReadResult<StaffPageResponseDto<StaffUserResponseDto>>> UsersAsync(Guid actor, UserRoleCode role,
        string? search, string? filterRole, string? status, int limit, string? cursor, CancellationToken token);
    Task<StaffReadResult<StaffAccountResponseDto>> AccountAsync(Guid actor, UserRoleCode role, Guid user, CancellationToken token);
    Task<StaffReadResult<StaffPageResponseDto<ProjectMemberResponseDto>>> MembersAsync(Guid actor, UserRoleCode role, Guid project,
        string? search, string? filterRole, string? status, bool includeHistory, int limit, string? cursor, CancellationToken token);
    Task<StaffReadResult<PrimaryProjectManagerResponseDto>> PrimaryAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
    Task<StaffReadResult<StaffPageResponseDto<EligibleProjectMemberDto>>> EligibleAsync(Guid actor, UserRoleCode role,
        Guid project, string? search, string? filterRole, int limit, string? cursor, CancellationToken token);
    Task<StaffReadResult<ProjectMemberResponseDto>> AddMemberAsync(Guid actor, UserRoleCode role,
        Guid project, Guid user, string? idempotencyKey, CancellationToken token);
}
