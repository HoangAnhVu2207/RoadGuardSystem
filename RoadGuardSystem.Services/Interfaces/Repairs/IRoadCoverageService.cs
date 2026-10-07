using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;

namespace RoadGuardSystem.Services.Repairs;

public interface IRoadCoverageService
{
    Task<RoadCoverageServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
    Task<RoadCoverageServiceResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project, RoadCoverageInputDto input,
        string? key, string? version, CancellationToken token);
}
