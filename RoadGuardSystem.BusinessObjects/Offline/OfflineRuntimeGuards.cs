using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

internal static class OfflineRuntimeGuards
{
    internal static void Identity(params Guid[] ids)
    {
        if (ids.Any(x => x == Guid.Empty)) throw new ArgumentException("Offline identity is required.");
    }
    internal static void Hash(string value)
    {
        if (value is null || value.Length != 64 || value.Any(x => !char.IsAsciiHexDigit(x)))
            throw new ArgumentException("SHA256 is required.");
    }
    internal static void Json(string value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > limit)
            throw new ArgumentException("Offline payload is outside its bound.");
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("A typed object is required.");
        }
        catch (JsonException exception) { throw new ArgumentException("Malformed offline payload.", exception); }
    }
    internal static void Time(DateTimeOffset value)
    {
        if (value == default) throw new ArgumentException("Server time is required.");
    }
    internal static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
