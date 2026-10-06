using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Projects;

namespace RoadGuardSystem.Services.Projects;

public interface IProjectLifecycleService
{
    Task<ProjectLifecycleMutationResult> ReadAsync(Guid actor,UserRoleCode role,Guid project,CancellationToken cancellationToken);
    Task<ProjectLifecycleMutationResult> RenewAsync(Guid actor,UserRoleCode role,Guid project,RenewedHandlingScopeInput input,
        string? key,string? expectedVersion,CancellationToken cancellationToken);
}
