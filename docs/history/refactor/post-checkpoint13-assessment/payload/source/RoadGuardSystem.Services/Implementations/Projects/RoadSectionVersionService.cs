using RoadGuardSystem.BusinessObjects.Projects;
using NetTopologySuite.Geometries;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public sealed class RoadSectionVersionService : IRoadSectionVersionService
{
    private readonly IRoadSectionVersionRepository _repository;

    public RoadSectionVersionService(IRoadSectionVersionRepository repository)
    {
        _repository = repository;
    }

    public async Task<RoadSectionVersionServiceResult> CreateRoadSectionAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateRoadSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(RoadSectionVersionServiceStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || command.ProjectId == Guid.Empty || command.OperationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.ChangeReason) ||
            command.Coordinates.Count < 2 || !SpatialConstants.IsAllowedProjectUtmSrid(command.Srid) ||
            command.Coordinates.Any(coordinate => !double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y)))
        {
            return new(RoadSectionVersionServiceStatus.InvalidInput);
        }

        var geometry = new GeometryFactory(new PrecisionModel(), command.Srid).CreateLineString(
            command.Coordinates.Select(coordinate => new Coordinate(coordinate.X, coordinate.Y)).ToArray());
        var result = await _repository.CreateInitialAsync(
            new RoadSectionInitialVersionWriteRequest(
                actorUserId,
                command.ProjectId,
                command.Code,
                command.Name,
                geometry,
                command.EffectiveFrom,
                command.ChangeReason,
                command.OperationId,
                command.CorrelationId),
            cancellationToken);
        return new(MapStatus(result.Status), result.Version is null ? null : new(
            result.Version.ProjectId,
            result.Version.RoadSectionId,
            result.Version.RoadSectionVersionId,
            result.Version.Code,
            result.Version.VersionNo,
            result.Version.IsCurrent,
            result.Version.EffectiveFrom,
            result.Version.ChangeReason));
    }

    public async Task<RoadSectionVersionServiceResult> CreateRoadSectionVersionAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateRoadSectionVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(RoadSectionVersionServiceStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || command.ProjectId == Guid.Empty || command.RoadSectionId == Guid.Empty ||
            command.ExpectedCurrentVersionId == Guid.Empty || command.OperationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ChangeReason) || command.Coordinates.Count < 2 ||
            !SpatialConstants.IsAllowedProjectUtmSrid(command.Srid) ||
            command.Coordinates.Any(coordinate => !double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y)))
        {
            return new(RoadSectionVersionServiceStatus.InvalidInput);
        }

        var geometry = new GeometryFactory(new PrecisionModel(), command.Srid).CreateLineString(
            command.Coordinates.Select(coordinate => new Coordinate(coordinate.X, coordinate.Y)).ToArray());
        var result = await _repository.CreateNextAsync(
            new RoadSectionNextVersionWriteRequest(
                actorUserId,
                command.ProjectId,
                command.RoadSectionId,
                command.ExpectedCurrentVersionId,
                geometry,
                command.EffectiveFrom,
                command.ChangeReason,
                command.OperationId,
                command.CorrelationId),
            cancellationToken);
        return new(MapStatus(result.Status), result.Version is null ? null : new(
            result.Version.ProjectId,
            result.Version.RoadSectionId,
            result.Version.RoadSectionVersionId,
            result.Version.Code,
            result.Version.VersionNo,
            result.Version.IsCurrent,
            result.Version.EffectiveFrom,
            result.Version.ChangeReason));
    }

    public async Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(initialVersion);
        if (initialVersion.RoadSectionId != section.Id || initialVersion.VersionNo != 1 || !initialVersion.IsCurrent)
        {
            throw new ArgumentException("Initial version must be current version 1 for the supplied road section.");
        }

        var facts = await _repository.GetInitialFactsAsync(section.ProjectId, cancellationToken);
        if (facts?.EngineeringUtmSrid is not int projectUtmSrid)
        {
            throw new InvalidOperationException("Project UTM SRID must be configured before creating a road section version.");
        }

        SpatialValidation.EnsureProjectEngineeringGeometry(initialVersion.Geometry, projectUtmSrid);
        await _repository.CreateInitialAsync(section, initialVersion, cancellationToken);
    }

    public async Task AddVersionAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextVersion);
        if (roadSectionId == Guid.Empty || nextVersion.RoadSectionId != roadSectionId || !nextVersion.IsCurrent)
        {
            throw new ArgumentException("Next version must be current and belong to the supplied road section.");
        }

        var facts = await _repository.GetNextFactsAsync(roadSectionId, cancellationToken)
            ?? throw new InvalidOperationException("Road section has no current version.");
        if (facts.EngineeringUtmSrid is not int projectUtmSrid)
        {
            throw new InvalidOperationException("Project UTM SRID must be configured before creating a road section version.");
        }

        if (facts.CurrentVersionNo is not int currentVersionNo || nextVersion.VersionNo <= currentVersionNo)
        {
            throw new ArgumentException("Road section version number must increase.", nameof(nextVersion));
        }

        SpatialValidation.EnsureProjectEngineeringGeometry(nextVersion.Geometry, projectUtmSrid);
        await _repository.AddVersionAndMakeCurrentAsync(roadSectionId, nextVersion, cancellationToken);
    }

    private static RoadSectionVersionServiceStatus MapStatus(RoadSectionVersionWriteStatus status) => status switch
    {
        RoadSectionVersionWriteStatus.Success => RoadSectionVersionServiceStatus.Success,
        RoadSectionVersionWriteStatus.Replayed => RoadSectionVersionServiceStatus.Replayed,
        RoadSectionVersionWriteStatus.ProjectNotFound => RoadSectionVersionServiceStatus.ProjectNotFound,
        RoadSectionVersionWriteStatus.ProjectClosed => RoadSectionVersionServiceStatus.ProjectClosed,
        RoadSectionVersionWriteStatus.RoadSectionNotFound => RoadSectionVersionServiceStatus.RoadSectionNotFound,
        RoadSectionVersionWriteStatus.RoadSectionCodeConflict => RoadSectionVersionServiceStatus.RoadSectionCodeConflict,
        RoadSectionVersionWriteStatus.StaleConcurrency => RoadSectionVersionServiceStatus.StaleConcurrency,
        RoadSectionVersionWriteStatus.IdempotentConflict => RoadSectionVersionServiceStatus.IdempotentConflict,
        _ => RoadSectionVersionServiceStatus.InvalidInput
    };
}
