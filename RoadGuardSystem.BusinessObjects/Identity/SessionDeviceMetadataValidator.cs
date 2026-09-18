using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Identity;

public sealed record ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string message) => new(false, message);
}

public static class SessionDeviceMetadataValidator
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "schema_version",
        "device_id",
        "platform",
        "app_version"
    };

    private static readonly string[] SensitiveKeywords =
    [
        "password",
        "secret",
        "bearer",
        "access_token",
        "refresh_token"
    ];

    public static readonly SessionDeviceMetadataOptions DefaultOptions = new();

    public static ValidationResult Validate(string? metadataJson, SessionDeviceMetadataOptions? options = null)
    {
        options ??= DefaultOptions;
        if (metadataJson is null)
        {
            return ValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return ValidationResult.Fail("device_metadata_json cannot be empty or whitespace when provided.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(metadataJson);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Fail($"device_metadata_json is not valid JSON: {ex.Message}");
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Fail("device_metadata_json root must be a JSON object.");
            }

            var root = doc.RootElement;

            if (!root.TryGetProperty("schema_version", out var schemaVersionProp))
            {
                return ValidationResult.Fail("device_metadata_json must contain required 'schema_version' integer property.");
            }

            if (schemaVersionProp.ValueKind != JsonValueKind.Number || !schemaVersionProp.TryGetInt32(out var schemaVersion) || schemaVersion != 1)
            {
                return ValidationResult.Fail("device_metadata_json 'schema_version' must be integer value 1.");
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (!AllowedProperties.Contains(prop.Name))
                {
                    return ValidationResult.Fail($"Unknown field '{prop.Name}' in device_metadata_json.");
                }

                if (prop.Name == "schema_version")
                {
                    continue;
                }

                if (prop.Value.ValueKind != JsonValueKind.String)
                {
                    return ValidationResult.Fail($"Field '{prop.Name}' must be a string value.");
                }

                var strVal = prop.Value.GetString()!;

                // Length checks
                if (prop.Name == "device_id" && strVal.Length > options.MaxDeviceIdLength)
                {
                    return ValidationResult.Fail($"Field '{prop.Name}' exceeds maximum length {options.MaxDeviceIdLength}.");
                }
                if (prop.Name == "platform" && strVal.Length > options.MaxPlatformLength)
                {
                    return ValidationResult.Fail($"Field '{prop.Name}' exceeds maximum length {options.MaxPlatformLength}.");
                }
                if (prop.Name == "app_version" && strVal.Length > options.MaxAppVersionLength)
                {
                    return ValidationResult.Fail($"Field '{prop.Name}' exceeds maximum length {options.MaxAppVersionLength}.");
                }

                // Sensitive keyword check
                foreach (var keyword in options.SensitiveKeywords)
                {
                    if (strVal.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return ValidationResult.Fail($"Field '{prop.Name}' contains forbidden sensitive keyword.");
                    }
                }
            }

            return ValidationResult.Success();
        }
    }
}
