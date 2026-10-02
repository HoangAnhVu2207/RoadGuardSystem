namespace RoadGuardSystem.Repositories.Files;
public interface IReporterEvidenceRepository
{
    Task<bool> IsActiveReporterAsync(Guid actorUserId, CancellationToken cancellationToken = default);
}
