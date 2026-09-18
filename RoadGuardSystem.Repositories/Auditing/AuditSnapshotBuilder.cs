using System.Text;
using RoadGuardSystem.BusinessObjects.Auditing;

namespace RoadGuardSystem.Repositories.Auditing;

public static class AuditSnapshotBuilder
{
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

        return SensitiveJsonSanitizer.ApplyAllowList(json, allowedPropertyNames);
    }
}
