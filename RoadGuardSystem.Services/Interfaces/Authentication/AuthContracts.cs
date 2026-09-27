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
    RefreshTokenInvalid,
    RefreshTokenExpired,
    IdempotentConflict,
    Conflict
}

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    int ExpiresIn,
    UserSecurityState User);

public sealed record AuthResult(AuthStatus Status, AuthTokens? Tokens = null, string? ErrorMessage = null);

public sealed record LoginCommand(string Email, string Password);

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
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<PasswordChangePreparationResult> PrepareForcedPasswordChangeAsync(
        string username,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<PasswordChangePreparationResult> PreparePasswordChangeAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken = default);

    Task<AuthResult> LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AuthResult> LogoutAsync(
        Guid userId,
        Guid sessionId,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<PasswordRecoveryResult> RequestPasswordRecoveryAsync(
        PasswordRecoveryCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthResult> CompleteForcedPasswordChangeAsync(
        ForcedPasswordChangeCommand command,
        CancellationToken cancellationToken = default);
}
