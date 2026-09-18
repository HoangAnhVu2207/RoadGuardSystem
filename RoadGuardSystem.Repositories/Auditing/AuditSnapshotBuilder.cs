using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RoadGuardSystem.Repositories.Auditing;

public static class AuditSnapshotBuilder
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

    public static string Build(
        string json,
        IReadOnlyCollection<string> allowedPropertyNames,
        int maxUtf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(allowedPropertyNames);
        if (maxUtf8Bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUtf8Bytes));
        }

        var byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > maxUtf8Bytes)
        {
            throw new ArgumentException(
                $"Snapshot exceeds maximum UTF-8 size {maxUtf8Bytes} bytes.",
                nameof(json));
        }

        var root = JsonNode.Parse(json) ?? throw new JsonException("Snapshot JSON cannot be null.");
        if (root is not (JsonObject or JsonArray))
        {
            throw new ArgumentException("Snapshot JSON root must be an object or array.", nameof(json));
        }

        var allowList = allowedPropertyNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Sanitize(root, allowList);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void Sanitize(JsonNode node, IReadOnlySet<string> allowList)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (SensitivePropertyNames.Contains(property.Key))
                {
                    jsonObject[property.Key] = RedactedValue;
                    continue;
                }

                if (!allowList.Contains(property.Key))
                {
                    jsonObject.Remove(property.Key);
                    continue;
                }

                if (property.Value is not null)
                {
                    Sanitize(property.Value, allowList);
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
                    Sanitize(item, allowList);
                }
            }
        }
    }
}
