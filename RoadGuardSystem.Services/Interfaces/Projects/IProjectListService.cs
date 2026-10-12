using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public enum ProjectListStatus { Success, Unauthorized, Forbidden, InvalidInput }

public sealed record ProjectListResult(ProjectListStatus Status, ProjectListResponseDto? Page = null);

public interface IProjectListService
{
    Task<ProjectListResult> ListAsync(
        Guid actorUserId, UserRoleCode actorRole, string? search, string? status,
        string? cursor, int limit, CancellationToken cancellationToken = default);
}
