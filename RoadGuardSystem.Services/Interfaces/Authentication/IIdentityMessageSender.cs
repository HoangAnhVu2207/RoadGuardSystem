namespace RoadGuardSystem.Services.Authentication;

public interface IIdentityMessageSender
{
    Task<bool> SendReporterOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default);

    Task<bool> SendInvitationAsync(
        string email,
        string displayName,
        string roleName,
        string invitationToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
