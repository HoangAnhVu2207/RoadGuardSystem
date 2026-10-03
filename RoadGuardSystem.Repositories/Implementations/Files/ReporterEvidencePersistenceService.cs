using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Implementations.Files;
public sealed class ReporterEvidencePersistenceService(RoadGuardDbContext context) : IReporterEvidenceRepository
{
    public Task<bool> IsActiveReporterAsync(Guid actorUserId, CancellationToken cancellationToken = default)
        => new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(context)
            .IsCurrentActorAsync(actorUserId, UserRoleCode.Reporter, cancellationToken);
}
