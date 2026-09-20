using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Warranties;

namespace RoadGuardSystem.Repositories.Warranties;

/// <summary>
/// Validates project ownership across optional Warranty references before persisting the aggregate.
/// </summary>
public sealed class WarrantyPersistenceService
{
    private readonly RoadGuardDbContext _context;

    public WarrantyPersistenceService(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Warranty warranty, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warranty);

        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == warranty.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new InvalidOperationException("Warranty project was not found.");
        }

        if (warranty.RoadSectionId is Guid roadSectionId)
        {
            var roadMatchesProject = await _context.RoadSections
                .AsNoTracking()
                .AnyAsync(
                    section => section.Id == roadSectionId && section.ProjectId == warranty.ProjectId,
                    cancellationToken);
            if (!roadMatchesProject)
            {
                throw new InvalidOperationException("Warranty road section must belong to the warranty project.");
            }
        }

        if (warranty.HandoverDocumentId is Guid handoverDocumentId)
        {
            var handoverMatchesProject = await _context.HandoverDocuments
                .AsNoTracking()
                .AnyAsync(
                    document => document.Id == handoverDocumentId && document.ProjectId == warranty.ProjectId,
                    cancellationToken);
            if (!handoverMatchesProject)
            {
                throw new InvalidOperationException("Warranty handover document must belong to the warranty project.");
            }
        }

        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
