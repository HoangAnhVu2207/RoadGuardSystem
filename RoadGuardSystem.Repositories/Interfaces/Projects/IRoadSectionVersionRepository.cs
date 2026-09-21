using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Projects;

public interface IRoadSectionVersionRepository
{
    Task<RoadSectionVersionWriteResult> CreateInitialAsync(
        RoadSectionInitialVersionWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<RoadSectionVersionWriteResult> CreateNextAsync(
        RoadSectionNextVersionWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<RoadSectionVersionFacts?> GetInitialFactsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<RoadSectionVersionFacts?> GetNextFactsAsync(
        Guid roadSectionId,
        CancellationToken cancellationToken = default);

    Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default);

    Task AddVersionAndMakeCurrentAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default);
}
