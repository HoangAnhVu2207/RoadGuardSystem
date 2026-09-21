using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Spatial;
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
