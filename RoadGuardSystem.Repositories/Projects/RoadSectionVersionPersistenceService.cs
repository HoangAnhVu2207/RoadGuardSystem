using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Spatial;
using RoadGuardSystem.Repositories.Transactions;

namespace RoadGuardSystem.Repositories.Projects;

/// <summary>
/// Persists the initial and replacement road geometry versions without exposing a transient second current version.
/// </summary>
public sealed class RoadSectionVersionPersistenceService
{
    private readonly RoadGuardDbContext _context;
    private readonly RoadGuardTransactionService _transactionService;

    public RoadSectionVersionPersistenceService(
        RoadGuardDbContext context,
        RoadGuardTransactionService transactionService)
    {
        _context = context;
        _transactionService = transactionService;
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
            throw new InvalidOperationException(
                "Initial road section version must belong to the new section, be version 1, and be current.");
        }

        await ValidateProjectSridAsync(section.ProjectId, initialVersion, cancellationToken);
        await _transactionService.ExecuteAsync(
            async operationCancellationToken =>
            {
                _context.RoadSections.Add(section);
                await _context.SaveChangesAsync(operationCancellationToken);
                _context.RoadSectionVersions.Add(initialVersion);
            },
            cancellationToken);
    }

    public async Task AddVersionAndMakeCurrentAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextVersion);
        if (roadSectionId == Guid.Empty || nextVersion.RoadSectionId != roadSectionId || !nextVersion.IsCurrent)
        {
            throw new InvalidOperationException("Next road section version must target the section and be marked current.");
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var section = await _context.RoadSections
                    .AsNoTracking()
                    .SingleOrDefaultAsync(candidate => candidate.Id == roadSectionId, cancellationToken)
                    ?? throw new InvalidOperationException("Road section was not found.");
                await ValidateProjectSridAsync(section.ProjectId, nextVersion, cancellationToken);

                var currentVersion = await _context.RoadSectionVersions
                    .SingleOrDefaultAsync(candidate => candidate.RoadSectionId == roadSectionId && candidate.IsCurrent, cancellationToken)
                    ?? throw new InvalidOperationException("Road section has no current version.");
                if (nextVersion.VersionNo <= currentVersion.VersionNo)
                {
                    throw new InvalidOperationException("Road section version number must increase.");
                }

                currentVersion.ClearCurrent();
                await _context.SaveChangesAsync(cancellationToken);
                _context.RoadSectionVersions.Add(nextVersion);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

    private async Task ValidateProjectSridAsync(
        Guid projectId,
        RoadSectionVersion version,
        CancellationToken cancellationToken)
    {
        var projectUtmSrid = await _context.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => project.EngineeringUtmSrid)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Project UTM SRID must be configured before creating a road section version.");
        SpatialValidation.EnsureProjectEngineeringGeometry(version.Geometry, projectUtmSrid);
    }
}
