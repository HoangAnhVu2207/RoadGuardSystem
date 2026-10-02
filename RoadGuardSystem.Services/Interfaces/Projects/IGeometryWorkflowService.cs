using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Projects;
public interface IGeometryWorkflowService
{
    Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, GeometryWorkflowCommand command, CancellationToken cancellationToken);
}
