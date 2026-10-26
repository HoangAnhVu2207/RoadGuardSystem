using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
namespace RoadGuardSystem.Repositories.Cases;

public interface IPublicPublicationRepository
{
    Task GuardAsync(Guid actor, UserRoleCode role, Guid project, Guid target, bool withdraw,
        Func<Guid, CancellationToken, Task<bool>> access, CancellationToken ct);
    Task<PublicPublication> CreateAsync(Guid actor, Guid project, Guid defect, CancellationToken ct);
    Task<PublicPublication> WithdrawAsync(Guid actor, Guid project, Guid id, CancellationToken ct);
    Task<PublicPublication?> ReadAsync(Guid id, CancellationToken ct);
}
