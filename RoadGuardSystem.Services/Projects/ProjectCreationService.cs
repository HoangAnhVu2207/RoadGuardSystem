using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public enum ProjectCreationStatus
{
    Success,
    Replayed,
    InvalidInput,
    Forbidden,
    ProjectManagerNotFound,
    HandoverFileNotFound,
    ProjectCodeConflict,
    IdempotentConflict
}

public sealed record CreateProjectCommand(
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid PrimaryProjectManagerUserId,
    string HandoverDocumentNo,
    DateOnly HandoverDate,
    Guid? HandoverFileId,
    string? HandoverNotes,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record CreatedProjectView(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string Status,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    DateOnly HandoverDate,
    string RowVersion);

public sealed record ProjectCreationResult(ProjectCreationStatus Status, CreatedProjectView? Project = null);

public interface IProjectCreationService
{
    Task<ProjectCreationResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateProjectCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ProjectCreationService : IProjectCreationService
{
    private readonly ProjectCreationPersistenceService _persistence;

    public ProjectCreationService(ProjectCreationPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<ProjectCreationResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(ProjectCreationStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || command.OperationId == Guid.Empty ||
            command.PrimaryProjectManagerUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ProjectCode) ||
            string.IsNullOrWhiteSpace(command.Name) ||
            string.IsNullOrWhiteSpace(command.HandoverDocumentNo))
        {
            return new(ProjectCreationStatus.InvalidInput);
        }

        var result = await _persistence.CreateAsync(new ProjectCreationPersistenceRequest(
            actorUserId,
            command.ProjectCode,
            command.Name,
            command.Description,
            command.EngineeringUtmSrid,
            command.StartDate,
            command.EndDate,
            command.PrimaryProjectManagerUserId,
            command.HandoverDocumentNo,
            command.HandoverDate,
            command.HandoverFileId,
            command.HandoverNotes,
            command.OperationId,
            command.CorrelationId), cancellationToken);

        return new ProjectCreationResult(
            MapStatus(result.Status),
            result.Project is null ? null : new CreatedProjectView(
                result.Project.ProjectId,
                result.Project.ProjectCode,
                result.Project.Name,
                result.Project.Status.ToString().ToUpperInvariant(),
                result.Project.PrimaryProjectManagerUserId,
                result.Project.HandoverDocumentId,
                result.Project.HandoverDate,
                Convert.ToBase64String(result.Project.RowVersion)));
    }

    private static ProjectCreationStatus MapStatus(ProjectCreationPersistenceStatus status) => status switch
    {
        ProjectCreationPersistenceStatus.Success => ProjectCreationStatus.Success,
        ProjectCreationPersistenceStatus.Replayed => ProjectCreationStatus.Replayed,
        ProjectCreationPersistenceStatus.ActorNotAuthorized => ProjectCreationStatus.Forbidden,
        ProjectCreationPersistenceStatus.ProjectManagerNotFound => ProjectCreationStatus.ProjectManagerNotFound,
        ProjectCreationPersistenceStatus.HandoverFileNotFound => ProjectCreationStatus.HandoverFileNotFound,
        ProjectCreationPersistenceStatus.ProjectCodeConflict => ProjectCreationStatus.ProjectCodeConflict,
        ProjectCreationPersistenceStatus.IdempotentConflict => ProjectCreationStatus.IdempotentConflict,
        _ => ProjectCreationStatus.InvalidInput
    };
}
