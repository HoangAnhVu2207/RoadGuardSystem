using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Offline;

namespace RoadGuardSystem.Services.Offline;

public interface IOfflineWorkflowService
{
    Task<OfflineWorkflowResult> ExecuteAsync(Guid actorId, UserRoleCode role, Guid projectId, string action,
        object? input, Guid? resourceId, string? operationKey, string? expectedVersion, CancellationToken cancellationToken);
}
