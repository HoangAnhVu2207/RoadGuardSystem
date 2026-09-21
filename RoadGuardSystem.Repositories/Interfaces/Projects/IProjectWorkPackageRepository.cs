namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectWorkPackageRepository
{
    Task<ProjectWorkPackageReadResult?> ReadAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
