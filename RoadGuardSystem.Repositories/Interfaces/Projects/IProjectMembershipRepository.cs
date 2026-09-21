namespace RoadGuardSystem.Repositories.Projects;

public interface IProjectMembershipRepository
{
    Task<EffectiveProjectMembership?> FindByUserAndProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default);
}
