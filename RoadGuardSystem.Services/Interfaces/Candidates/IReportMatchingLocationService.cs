using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Services.Candidates;

public interface IReportMatchingLocationService
{
    Task<ReportMatchingLocationResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid report, CancellationToken token);
    Task<ReportMatchingLocationResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project, Guid report,
        ReportMatchingLocationInput input, string? key, CancellationToken token);
}
