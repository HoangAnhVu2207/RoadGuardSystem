using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public interface IProjectWorkPackageService
{
    Task<ProjectWorkPackageResponseDto?> GetAsync(
        Guid projectId,
        UserRoleCode accessRole,
        CancellationToken cancellationToken = default);
}

public sealed class ProjectWorkPackageService : IProjectWorkPackageService
{
    private readonly ProjectWorkPackageReadModel _readModel;

    public ProjectWorkPackageService(ProjectWorkPackageReadModel readModel)
    {
        _readModel = readModel;
    }

    public async Task<ProjectWorkPackageResponseDto?> GetAsync(
        Guid projectId,
        UserRoleCode accessRole,
        CancellationToken cancellationToken = default)
    {
        var result = await _readModel.ReadAsync(projectId, cancellationToken);
        return result is null
            ? null
            : new ProjectWorkPackageResponseDto(
                result.ProjectId,
                result.ProjectCode,
                result.Name,
                result.Description,
                result.EngineeringUtmSrid,
                result.Status.ToString().ToUpperInvariant(),
                result.StartDate,
                result.EndDate,
                accessRole.ToDbCode(),
                Convert.ToBase64String(result.RowVersion),
                result.RoadSections.Select(section => new RoadSectionWorkPackageDto(
                    section.RoadSectionId,
                    section.Code,
                    section.Name,
                    section.CurrentVersionId,
                    section.VersionNo,
                    section.GeometryWkt,
                    section.Srid,
                    section.EffectiveFrom,
                    section.ChangeReason)).ToArray(),
                result.Warranties.Select(warranty => new WarrantyWorkPackageDto(
                    warranty.WarrantyId,
                    warranty.RoadSectionId,
                    warranty.HandoverDocumentId,
                    warranty.HandoverDate,
                    warranty.WarrantyStartDate,
                    warranty.WarrantyEndDate,
                    warranty.RetainedValue,
                    warranty.Scope.ToApiCode(),
                    warranty.Terms,
                    warranty.SourceDocumentId,
                    warranty.Status.ToString().ToUpperInvariant())).ToArray());
    }
}
