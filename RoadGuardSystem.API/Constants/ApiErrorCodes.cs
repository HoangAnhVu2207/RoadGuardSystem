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
}
