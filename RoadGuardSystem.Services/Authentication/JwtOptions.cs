using Microsoft.Extensions.Options;

namespace RoadGuardSystem.Services.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string ActiveKeyId { get; set; } = string.Empty;

    public Dictionary<string, string> SigningKeys { get; set; } = new(StringComparer.Ordinal);

    public int AccessTokenLifetimeMinutes { get; set; }

    public int SessionLifetimeHours { get; set; }

    public int RefreshTokenLifetimeDays { get; set; }
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ActiveKeyId))
        {
            failures.Add("ActiveKeyId is required.");
        }

        if (options.AccessTokenLifetimeMinutes <= 0 ||
            options.SessionLifetimeHours <= 0 ||
            options.RefreshTokenLifetimeDays <= 0)
        {
            failures.Add("AccessTokenLifetimeMinutes, SessionLifetimeHours, and RefreshTokenLifetimeDays must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.ActiveKeyId) ||
            !options.SigningKeys.TryGetValue(options.ActiveKeyId, out var activeKey))
        {
            failures.Add("The active signing key must exist in the configured key ring.");
        }
        else if (!TryDecodeKey(activeKey, out _))
        {
            failures.Add("The active signing key must be valid Base64 and at least 32 bytes.");
        }

        foreach (var (keyId, encodedKey) in options.SigningKeys)
        {
            if (string.IsNullOrWhiteSpace(keyId) || !TryDecodeKey(encodedKey, out _))
            {
                failures.Add("Every configured signing key must have a non-empty id and valid Base64 content of at least 32 bytes.");
                break;
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    public static byte[] DecodeKey(string encodedKey)
    {
        if (!TryDecodeKey(encodedKey, out var key))
        {
            throw new OptionsValidationException(JwtOptions.SectionName, typeof(JwtOptions), ["The configured signing key is invalid."]);
        }

        return key;
    }

    private static bool TryDecodeKey(string? encodedKey, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            return false;
        }

        try
        {
            key = Convert.FromBase64String(encodedKey);
            return key.Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
