using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Implementations.Files;
public sealed class ReporterEvidencePersistenceService(RoadGuardDbContext context) : IReporterEvidenceRepository
{
    public Task<bool> IsActiveReporterAsync(Guid actorUserId, CancellationToken cancellationToken = default)
        => context.Users.AsNoTracking().AnyAsync(u => u.Id == actorUserId && u.RoleCode == UserRoleCode.Reporter && u.Status == UserStatus.Active && !u.MustChangePassword, cancellationToken);
}
