using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public interface IIdentityOnboardingRepository
{
    Task<ReporterRegistrationIntent?> GetReporterRegistrationIntentAsync(
        Guid intentId,
        CancellationToken cancellationToken = default);

    Task<StaffInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<ReporterRegistrationPersistenceResult> RegisterReporterAsync(
        ApplicationUser pendingUser,
        ReporterRegistrationIntent intent,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<ReporterRegistrationPersistenceResult> ResendReporterOtpAsync(
        Guid intentId,
        string otpHash,
        DateTimeOffset expiresAt,
        DateTimeOffset resendAvailableAt,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<ReporterRegistrationPersistenceResult> VerifyReporterOtpAsync(
        Guid intentId,
        string otpHash,
        int maxAttempts,
        UserSession session,
        RefreshToken refreshToken,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<InvitationPersistenceResult> CreateInvitationAsync(
        StaffInvitation invitation,
        IReadOnlyCollection<Guid> projectIds,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<InvitationPersistenceResult> AcceptInvitationAsync(
        string tokenHash,
        ApplicationUser user,
        UserSession session,
        RefreshToken refreshToken,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
