using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record ProjectWorkPackageReadResult(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    byte[] RowVersion,
    IReadOnlyList<RoadSectionWorkPackageReadResult> RoadSections,
    IReadOnlyList<WarrantyWorkPackageReadResult> Warranties);

public sealed record RoadSectionWorkPackageReadResult(
    Guid RoadSectionId,
    string Code,
    string? Name,
    Guid CurrentVersionId,
    int VersionNo,
    string GeometryWkt,
    int Srid,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);

public sealed record WarrantyWorkPackageReadResult(
    Guid WarrantyId,
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    WarrantyScope Scope,
    string? Terms,
    Guid? SourceDocumentId,
    WarrantyStatus Status);

public sealed class ProjectWorkPackageReadModel
{
    private readonly RoadGuardDbContext _context;

    public ProjectWorkPackageReadModel(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectWorkPackageReadResult?> ReadAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(candidate => candidate.Id == projectId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.ProjectCode,
                candidate.Name,
                candidate.Description,
                candidate.EngineeringUtmSrid,
                candidate.Status,
                candidate.StartDate,
                candidate.EndDate,
                candidate.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return null;
        }

        var roadSections = await (
                from section in _context.RoadSections.AsNoTracking()
                join version in _context.RoadSectionVersions.AsNoTracking()
                    on section.Id equals version.RoadSectionId
                where section.ProjectId == projectId && version.IsCurrent
                orderby section.Code
                select new
                {
                    section.Id,
                    section.Code,
                    section.Name,
                    VersionId = version.Id,
                    version.VersionNo,
                    version.Geometry,
                    version.EffectiveFrom,
                    version.ChangeReason
                })
            .ToListAsync(cancellationToken);

        var warranties = await _context.Warranties
            .AsNoTracking()
            .Where(warranty => warranty.ProjectId == projectId)
            .OrderBy(warranty => warranty.WarrantyStartDate)
            .ThenBy(warranty => warranty.Id)
            .Select(warranty => new WarrantyWorkPackageReadResult(
                warranty.Id,
                warranty.RoadSectionId,
                warranty.HandoverDocumentId,
                warranty.HandoverDate,
                warranty.WarrantyStartDate,
                warranty.WarrantyEndDate,
                warranty.RetainedValue,
                warranty.Scope,
                warranty.Terms,
                warranty.SourceDocumentId,
                warranty.Status))
            .ToListAsync(cancellationToken);

        return new ProjectWorkPackageReadResult(
            project.Id,
            project.ProjectCode,
            project.Name,
            project.Description,
            project.EngineeringUtmSrid,
            project.Status,
            project.StartDate,
            project.EndDate,
            project.RowVersion,
            roadSections.Select(section => new RoadSectionWorkPackageReadResult(
                section.Id,
                section.Code,
                section.Name,
                section.VersionId,
                section.VersionNo,
                section.Geometry.AsText(),
                section.Geometry.SRID,
                section.EffectiveFrom,
                section.ChangeReason)).ToArray(),
            warranties);
    }
}
