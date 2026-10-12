using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record ProjectListPersistenceItem(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    ProjectStatus Status,
    Guid? PrimaryProjectManagerUserId,
    string? PrimaryProjectManagerName,
    DateTimeOffset CreatedAt);

public interface IProjectListRepository
{
    Task<IReadOnlyList<ProjectListPersistenceItem>> ListAsync(
        Guid actorUserId, UserRoleCode actorRole, DateOnly effectiveOn,
        string? search, ProjectStatus? status, DateTimeOffset? afterCreatedAt,
        Guid? afterId, int limit, CancellationToken cancellationToken = default);
}
