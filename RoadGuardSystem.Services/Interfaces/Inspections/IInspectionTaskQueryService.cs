using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Inspections;

public interface IInspectionTaskQueryService
{
    Task<InspectionTaskQueryResult> ListAssignedAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        string? cursor,
        int limit,
        RoadGuardSystem.Repositories.Mobile.MobileTaskListFilter? filter = null, CancellationToken cancellationToken = default);
}

public sealed record InspectionTaskQueryResult(
    InspectionTaskQueryStatus Status,
    InspectionTaskPageResponseDto? Page);

public enum InspectionTaskQueryStatus
{
    Success,
    Unauthorized,
    Forbidden,
    InvalidCursor,
    InvalidInput
}
