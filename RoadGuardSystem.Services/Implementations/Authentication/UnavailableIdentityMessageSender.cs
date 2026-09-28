namespace RoadGuardSystem.Services.Authentication;

public sealed class UnavailableIdentityMessageSender : IIdentityMessageSender
{
    public Task<bool> SendReporterOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> SendInvitationAsync(
        string email,
        string displayName,
        string roleName,
        string invitationToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
