using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public sealed class ProjectCreationService : IProjectCreationService
{
    private readonly IProjectCreationRepository _persistence;

    public ProjectCreationService(IProjectCreationRepository persistence)
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
            string.IsNullOrWhiteSpace(command.HandoverDocumentNo) ||
            command.EngineeringUtmSrid is int srid && !SpatialConstants.IsAllowedProjectUtmSrid(srid))
        {
            return new(ProjectCreationStatus.InvalidInput);
        }

        var request = new ProjectCreationPersistenceRequest(
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
            command.CorrelationId);
        var replay = await _persistence.TryGetReplayAsync(request, cancellationToken);
        if (replay is not null)
        {
            return ToResult(replay);
        }

        var facts = await _persistence.GetFactsAsync(
            command.PrimaryProjectManagerUserId,
            command.HandoverFileId,
            cancellationToken);
        if (!facts.PrimaryProjectManagerIsEligible)
        {
            return new(ProjectCreationStatus.ProjectManagerNotFound);
        }

        if (!facts.HandoverFileExists)
        {
            return new(ProjectCreationStatus.HandoverFileNotFound);
        }

        var result = await _persistence.CreateAsync(request, cancellationToken);

        return ToResult(result);
    }

    private static ProjectCreationResult ToResult(ProjectCreationPersistenceResult result)
    {
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
