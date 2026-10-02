using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public interface IProjectWorkPackageService
{
    Task<ProjectWorkPackageResponseDto?> GetAsync(
        Guid projectId,
        UserRoleCode accessRole,
        CancellationToken cancellationToken = default);
}
