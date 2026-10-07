using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Projects;

public interface IPavementWorkflowService
{
    Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, PavementWorkflowCommand command,
        CancellationToken cancellationToken = default);
}
