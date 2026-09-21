namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectCreationRepository
{
    Task<ProjectCreationPersistenceResult?> TryGetReplayAsync(
        ProjectCreationPersistenceRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectCreationFacts> GetFactsAsync(
        Guid primaryProjectManagerUserId,
        Guid? handoverFileId,
        CancellationToken cancellationToken = default);

    Task<ProjectCreationPersistenceResult> CreateAsync(
        ProjectCreationPersistenceRequest request,
        CancellationToken cancellationToken = default);
}
