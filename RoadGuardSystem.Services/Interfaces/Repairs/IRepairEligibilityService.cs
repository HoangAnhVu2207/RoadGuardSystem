using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
namespace RoadGuardSystem.Services.Repairs;
public interface IRepairEligibilityService
{
    Task<RepairEligibilityServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item, CancellationToken token);
}
