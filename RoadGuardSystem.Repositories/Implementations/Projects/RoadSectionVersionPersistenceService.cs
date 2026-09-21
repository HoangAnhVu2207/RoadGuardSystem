using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Transactions;

namespace RoadGuardSystem.Repositories.Projects;

/// <summary>
/// Persists the initial and replacement road geometry versions without exposing a transient second current version.
/// </summary>
public sealed class RoadSectionVersionPersistenceService : IRoadSectionVersionRepository
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

    public Task<RoadSectionVersionFacts?> GetInitialFactsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new RoadSectionVersionFacts(
                project.Id,
                project.EngineeringUtmSrid,
                null,
                null))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RoadSectionVersionFacts?> GetNextFactsAsync(
        Guid roadSectionId,
        CancellationToken cancellationToken = default)
    {
        return (
                from section in _context.RoadSections.AsNoTracking()
                where section.Id == roadSectionId
                join version in _context.RoadSectionVersions.AsNoTracking()
                    on section.Id equals version.RoadSectionId
                where version.IsCurrent
                select new RoadSectionVersionFacts(
                    section.ProjectId,
                    _context.Projects
                        .Where(project => project.Id == section.ProjectId)
                        .Select(project => project.EngineeringUtmSrid)
                        .SingleOrDefault(),
                    version.Id,
                    version.VersionNo))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(initialVersion);
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
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
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

}
