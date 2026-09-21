namespace RoadGuardSystem.Repositories.Projects;

public interface IPrimaryProjectManagerRepository
{
    Task<PrimaryProjectManagerFacts?> GetFactsAsync(
        Guid projectId,
        Guid replacementProjectManagerUserId,
        CancellationToken cancellationToken = default);

    Task<PrimaryProjectManagerWriteResult> ReassignAsync(
        PrimaryProjectManagerWriteRequest request,
        CancellationToken cancellationToken = default);
}
