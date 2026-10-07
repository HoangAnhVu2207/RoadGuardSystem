using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RoadCoverageCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    RoadCoverageInputFact Input, string Key, string ExpectedVersion);
public sealed record RoadCoverageResult(int Status, string? Code = null, RoadCoverageReadFact? Value = null);
public interface IRoadCoverageRepository
{
    Task<RoadCoverageResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token);
    Task<RoadCoverageResult> ConfirmAsync(RoadCoverageCommand command, CancellationToken token);
}
