using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Identity;

public interface IIdentityV2Repository
{
    Task<V2AccountUpdatePersistenceResult> UpdateAccountV2AtomicAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserStatus status,
        UserRoleCode roleCode,
        string reason,
        byte[] expectedRowVersion,
        string idempotencyKey,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);
}
