using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;
namespace RoadGuardSystem.Repositories.Reporting;

public interface IDefectStatisticsRepository
{
    Task<DefectStatisticsResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, ReportingFiltersFact filters, CancellationToken token);
    Task<DefectStatisticsResult> ConfirmAsync(DefectStatisticsCommand command, CancellationToken token);
}
