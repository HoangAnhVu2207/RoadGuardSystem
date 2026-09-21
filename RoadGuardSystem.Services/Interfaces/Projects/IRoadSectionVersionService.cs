using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public interface IRoadSectionVersionService
{
    Task<RoadSectionVersionServiceResult> CreateRoadSectionAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateRoadSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<RoadSectionVersionServiceResult> CreateRoadSectionVersionAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateRoadSectionVersionCommand command,
        CancellationToken cancellationToken = default);

    Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default);

    Task AddVersionAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default);
}
