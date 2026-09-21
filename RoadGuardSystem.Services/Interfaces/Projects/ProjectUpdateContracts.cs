using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public enum ProjectUpdateStatus
{
    Success,
    Replayed,
    InvalidInput,
    Forbidden,
    NotFound,
    StaleConcurrency,
    IdempotentConflict
}

public sealed record UpdateProjectCommand(
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string ExpectedRowVersion,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record UpdatedProjectView(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    string RowVersion);

public sealed record ProjectUpdateResult(ProjectUpdateStatus Status, UpdatedProjectView? Project = null);

public interface IProjectUpdateService
{
    Task<ProjectUpdateResult> UpdateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default);
}
