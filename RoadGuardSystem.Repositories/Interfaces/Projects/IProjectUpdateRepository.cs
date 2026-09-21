namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectUpdateRepository
{
    Task<ProjectUpdatePersistenceResult> UpdateAsync(
        ProjectUpdatePersistenceRequest request,
        CancellationToken cancellationToken = default);
}
