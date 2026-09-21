using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Services.Projects;

public interface IRoadSectionVersionService
{
    Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default);

    Task AddVersionAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default);
}
