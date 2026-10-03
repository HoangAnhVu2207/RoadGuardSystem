using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public sealed record ReportingCursorKey(string FilterHash, DateTimeOffset Time, Guid Id);
public static class ReportingCursor
{
    public static string FilterHash(Guid project, ReportingFiltersDto filters, string metric, string? type = null, Guid? aggregate = null) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { project, from = filters.From?.ToUniversalTime(), to = filters.To?.ToUniversalTime(),
            filters.RouteVersionId, filters.SegmentSetId, segments = (filters.SegmentIds ?? []).Distinct().Order().ToArray(), metric, type, aggregate }))).ToLowerInvariant();
    public static string Encode(string hash, DateTimeOffset time, Guid id) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new ReportingCursorKey(hash, time, id)));
    public static bool TryDecode(string? cursor, string hash, out ReportingCursorKey? key)
    {
        key = null; if (cursor is null) return true; if (cursor.Length > 2048) return false;
        try { key = JsonSerializer.Deserialize<ReportingCursorKey>(Convert.FromBase64String(cursor)); return key is not null && key.FilterHash == hash && key.Id != Guid.Empty; }
        catch (Exception e) when (e is FormatException or JsonException or ArgumentException) { return false; }
    }
}
