using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.Repositories.Projects;

namespace RoadGuardSystem.Services.Projects;

public sealed class RoadSectionVersionService : IRoadSectionVersionService
{
    private readonly IRoadSectionVersionRepository _repository;

    public RoadSectionVersionService(IRoadSectionVersionRepository repository)
    {
        _repository = repository;
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
}
