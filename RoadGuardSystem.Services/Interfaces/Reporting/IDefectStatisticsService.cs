using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.DTOs.Reporting;
namespace RoadGuardSystem.Services.Reporting;

public interface IDefectStatisticsService
{
    Task<DefectStatisticsResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, ReportingFiltersDto filters, CancellationToken token);
    Task<DefectStatisticsResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project, DefectStatisticsInputDto input, string? key, string? version, CancellationToken token);
}
