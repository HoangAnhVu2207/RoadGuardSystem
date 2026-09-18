using System.Text.Json;
using System.Text.Json.Nodes;

namespace RoadGuardSystem.BusinessObjects.Auditing;

public static class SensitiveJsonSanitizer
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "token",
        "secret",
        "authorization",
        "cookie",
        "connectionString"
    };

    public static string Redact(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var root = JsonNode.Parse(json) ?? throw new JsonException("JSON cannot be null.");
        if (root is not (JsonObject or JsonArray))
        {
            throw new ArgumentException("JSON root must be an object or array.", nameof(json));
        }

        RedactNode(root);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (SensitivePropertyNames.Contains(property.Key))
                {
                    jsonObject[property.Key] = RedactedValue;
                }
                else if (property.Value is not null)
                {
                    RedactNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    RedactNode(item);
                }
            }
        }
    }
}
