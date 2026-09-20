namespace RoadGuardSystem.API.Constants;

/// <summary>
/// Central machine-readable API error codes conforming to RoadGuard API platform standards.
/// Values are stable, lowercase snake_case strings intended for client error envelope parsing.
/// </summary>
public static class ApiErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string UnsupportedApiVersion = "unsupported_api_version";
    public const string NotFound = "not_found";
    public const string InternalError = "internal_error";
    public const string MethodNotAllowed = "method_not_allowed";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string InvalidCredentials = "auth_invalid_credentials";
    public const string PasswordChangeRequired = "auth_password_change_required";
    public const string Unauthorized = "auth_unauthorized";
    public const string SessionRevoked = "auth_session_revoked";
    public const string ConcurrencyConflict = "auth_concurrency_conflict";
    public const string DuplicateRequest = "duplicate_request";
    public const string EmailConflict = "identity_email_conflict";
    public const string AccessForbidden = "access_forbidden";
    public const string IdentityUserNotFound = "identity_user_not_found";
    public const string IdentityUserInactive = "identity_user_inactive";
}
