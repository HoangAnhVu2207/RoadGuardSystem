namespace RoadGuardSystem.Repositories.Projects;

public interface IPrimaryProjectManagerRepository
{
    Task<PrimaryProjectManagerFacts?> GetFactsAsync(
        Guid projectId,
        Guid replacementProjectManagerUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasReplayAsync(
        Guid actorUserId,
        Guid projectId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PrimaryProjectManagerWriteResult> ReassignAsync(
        PrimaryProjectManagerWriteRequest request,
        CancellationToken cancellationToken = default);
}
