using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Services.Authentication;

public enum AuthStatus
{
    Success,
    InvalidInput,
    InvalidCredentials,
    PasswordChangeRequired,
    PasswordPolicyRejected,
    NotRequired,
    Unauthorized,
    SessionRevoked,
    Conflict
}

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthResult(AuthStatus Status, AuthTokens? Tokens = null, string? ErrorMessage = null);

public sealed record LoginCommand(string Username, string Password, string? DeviceMetadataJson);

public sealed record RefreshCommand(string RefreshToken, Guid? CorrelationId = null);

public sealed record ForcedPasswordChangeCommand(
    string Username,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword,
    Guid OperationId,
    Guid? CorrelationId = null);

public enum CredentialVerificationStatus
{
    Success,
    InvalidCredentials
}

public sealed record CredentialVerificationResult(
    CredentialVerificationStatus Status,
    UserSecurityState? User = null);

public enum PasswordChangePreparationStatus
{
    Success,
    InvalidCredentials,
    ReusedPassword,
    PolicyRejected
}

public sealed record PasswordChangePreparationResult(
    PasswordChangePreparationStatus Status,
    UserSecurityState? User = null,
    string? NewPasswordHash = null,
    string? NewSecurityStamp = null,
    string? ErrorMessage = null);

public interface ICredentialVerifier
{
    Task<CredentialVerificationResult> VerifyAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<PasswordChangePreparationResult> PrepareForcedPasswordChangeAsync(
        string username,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AuthResult> CompleteForcedPasswordChangeAsync(
        ForcedPasswordChangeCommand command,
        CancellationToken cancellationToken = default);
}
