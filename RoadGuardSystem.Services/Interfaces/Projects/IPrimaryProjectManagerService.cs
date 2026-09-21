using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public interface IPrimaryProjectManagerService
{
    Task<PrimaryProjectManagerReassignmentResult> ReassignAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        ReassignPrimaryProjectManagerCommand command,
        CancellationToken cancellationToken = default);
}
