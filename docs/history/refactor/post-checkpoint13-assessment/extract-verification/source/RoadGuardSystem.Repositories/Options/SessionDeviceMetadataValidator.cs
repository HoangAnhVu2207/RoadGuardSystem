using System.Text.Json;

namespace RoadGuardSystem.Repositories.Options;

public static class SessionDeviceMetadataValidator
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "schema_version",
        "device_id",
        "platform",
        "app_version"
    };

    public static readonly SessionDeviceMetadataOptions DefaultOptions = new();

    public static SessionDeviceMetadataValidationResult Validate(
        string? metadataJson,
        SessionDeviceMetadataOptions? options = null)
    {
        options ??= DefaultOptions;
        if (metadataJson is null)
        {
            return SessionDeviceMetadataValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return SessionDeviceMetadataValidationResult.Fail("device_metadata_json cannot be empty or whitespace when provided.");
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return SessionDeviceMetadataValidationResult.Fail("device_metadata_json root must be a JSON object.");
            }

            var root = document.RootElement;
            if (!root.TryGetProperty("schema_version", out var schemaVersion) ||
                schemaVersion.ValueKind != JsonValueKind.Number ||
                !schemaVersion.TryGetInt32(out var version) || version != 1)
            {
                return SessionDeviceMetadataValidationResult.Fail("device_metadata_json must contain required 'schema_version' integer property with value 1.");
            }

            foreach (var property in root.EnumerateObject())
            {
                if (!AllowedProperties.Contains(property.Name))
                {
                    return SessionDeviceMetadataValidationResult.Fail($"Unknown field '{property.Name}' in device_metadata_json.");
                }

                if (property.Name == "schema_version")
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    return SessionDeviceMetadataValidationResult.Fail($"Field '{property.Name}' must be a string value.");
                }

                var value = property.Value.GetString()!;
                var limit = property.Name switch
                {
                    "device_id" => options.MaxDeviceIdLength,
                    "platform" => options.MaxPlatformLength,
                    "app_version" => options.MaxAppVersionLength,
                    _ => int.MaxValue
                };
                if (value.Length > limit)
                {
                    return SessionDeviceMetadataValidationResult.Fail($"Field '{property.Name}' exceeds maximum length {limit}.");
                }

                if (options.SensitiveKeywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    return SessionDeviceMetadataValidationResult.Fail($"Field '{property.Name}' contains forbidden sensitive keyword.");
                }
            }

            return SessionDeviceMetadataValidationResult.Success();
        }
        catch (JsonException exception)
        {
            return SessionDeviceMetadataValidationResult.Fail($"device_metadata_json is not valid JSON: {exception.Message}");
        }
    }
}
