namespace RoadGuardSystem.Services.Authentication;

public interface IIdentityOnboardingService
{
    Task<IdentityOnboardingResult> RegisterReporterAsync(
        string email,
        string password,
        string displayName,
        string reporterType,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdentityOnboardingResult> VerifyReporterOtpAsync(
        Guid intentId,
        string otp,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdentityOnboardingResult> ResendReporterOtpAsync(
        Guid intentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdentityOnboardingResult> CreateInvitationAsync(
        Guid actorUserId,
        string email,
        string displayName,
        string role,
        IReadOnlyList<Guid> projectIds,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdentityOnboardingResult> AcceptInvitationAsync(
        string invitationToken,
        string displayName,
        string password,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
