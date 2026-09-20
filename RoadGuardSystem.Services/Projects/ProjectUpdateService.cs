using RoadGuardSystem.Repositories.Projects;
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

public sealed class ProjectUpdateService : IProjectUpdateService
{
    private readonly ProjectUpdatePersistenceService _persistence;

    public ProjectUpdateService(ProjectUpdatePersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<ProjectUpdateResult> UpdateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(ProjectUpdateStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || projectId == Guid.Empty || command.OperationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.ExpectedRowVersion))
        {
            return new(ProjectUpdateStatus.InvalidInput);
        }

        byte[] expectedRowVersion;
        try
        {
            expectedRowVersion = Convert.FromBase64String(command.ExpectedRowVersion);
        }
        catch (FormatException)
        {
            return new(ProjectUpdateStatus.InvalidInput);
        }

        if (expectedRowVersion.Length != 8)
        {
            return new(ProjectUpdateStatus.InvalidInput);
        }

        var result = await _persistence.UpdateAsync(new ProjectUpdatePersistenceRequest(
            actorUserId,
            projectId,
            command.Name,
            command.Description,
            command.EngineeringUtmSrid,
            command.StartDate,
            command.EndDate,
            expectedRowVersion,
            command.OperationId,
            command.CorrelationId), cancellationToken);

        return new ProjectUpdateResult(
            MapStatus(result.Status),
            result.Project is null ? null : new UpdatedProjectView(
                result.Project.ProjectId,
                result.Project.ProjectCode,
                result.Project.Name,
                result.Project.Description,
                result.Project.EngineeringUtmSrid,
                result.Project.StartDate,
                result.Project.EndDate,
                result.Project.Status.ToString().ToUpperInvariant(),
                Convert.ToBase64String(result.Project.RowVersion)));
    }

    private static ProjectUpdateStatus MapStatus(ProjectUpdatePersistenceStatus status) => status switch
    {
        ProjectUpdatePersistenceStatus.Success => ProjectUpdateStatus.Success,
        ProjectUpdatePersistenceStatus.Replayed => ProjectUpdateStatus.Replayed,
        ProjectUpdatePersistenceStatus.ActorNotAuthorized => ProjectUpdateStatus.Forbidden,
        ProjectUpdatePersistenceStatus.NotFound => ProjectUpdateStatus.NotFound,
        ProjectUpdatePersistenceStatus.StaleConcurrency => ProjectUpdateStatus.StaleConcurrency,
        ProjectUpdatePersistenceStatus.IdempotentConflict => ProjectUpdateStatus.IdempotentConflict,
        _ => ProjectUpdateStatus.InvalidInput
    };
}
