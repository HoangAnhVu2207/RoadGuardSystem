using Microsoft.Extensions.Options;

namespace RoadGuardSystem.Services.Options;

public sealed class PasswordChangeFingerprintOptions
{
    public const string SectionName = "PasswordChangeFingerprint";

    public string Key { get; set; } = string.Empty;
}

public sealed class PasswordChangeFingerprintOptionsValidator : IValidateOptions<PasswordChangeFingerprintOptions>
{
    public ValidateOptionsResult Validate(string? name, PasswordChangeFingerprintOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Key))
        {
            return ValidateOptionsResult.Fail("PasswordChangeFingerprint:Key is required.");
        }

        try
        {
            return Convert.FromBase64String(options.Key).Length >= 32
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail("PasswordChangeFingerprint:Key must contain at least 32 bytes.");
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail("PasswordChangeFingerprint:Key must be valid Base64.");
        }
    }

    internal static byte[] DecodeKey(PasswordChangeFingerprintOptions options)
    {
        var validation = new PasswordChangeFingerprintOptionsValidator().Validate(
            PasswordChangeFingerprintOptions.SectionName,
            options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(
                PasswordChangeFingerprintOptions.SectionName,
                typeof(PasswordChangeFingerprintOptions),
                validation.Failures!);
        }

        return Convert.FromBase64String(options.Key);
    }
}
