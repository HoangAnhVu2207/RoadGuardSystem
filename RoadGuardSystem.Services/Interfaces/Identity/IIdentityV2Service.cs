namespace RoadGuardSystem.Services.Identity;

public interface IIdentityV2Service
{
    Task<IdentityV2Result> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IdentityV2Result> UpdateMeAsync(
        Guid userId,
        string displayName,
        string? ifMatch,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<IdentityV2Result> GetAccountAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);

    Task<IdentityV2Result> ResetPasswordAsync(
        Guid actorUserId,
        Guid targetUserId,
        string temporaryPassword,
        string reason,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<IdentityV2Result> UpdateAccountAsync(
        Guid actorUserId,
        Guid targetUserId,
        string status,
        string role,
        string reason,
        string? ifMatch,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);
}
