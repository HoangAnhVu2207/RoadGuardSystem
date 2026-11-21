using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RoadGuardSystem.Repositories.Mobile;

public sealed record MobileTaskListFilter(string? Status = null, string? StatusGroup = null, string? Mode = null, string? Search = null, string? Acceptance = null)
{
    public MobileTaskListFilter Normalize(bool survey)
    {
        var result = this with { Status = Clean(Status), StatusGroup = Clean(StatusGroup), Mode = Clean(Mode), Search = Search?.Trim().ToUpperInvariant(), Acceptance = Clean(Acceptance) };
        if (result.Search?.Length > 100 || result.Search?.Any(char.IsControl) == true) throw new ArgumentException("Search must be at most 100 characters without control characters.");
        var statuses = survey ? "NEW_ASSIGNED,ACCEPTED,REJECTED,REASSIGNED,IN_PROGRESS,SUBMITTED,SUPPLEMENT_REQUIRED,COMPLETED,CANCELLED,POSTPONED,UNKNOWN" : "NEWASSIGNED,ACCEPTED,REJECTED,INPROGRESS,SUPPLEMENTREQUIRED,SUBMITTED,COMPLETED,CANCELLED,UNKNOWN";
        var groups = survey ? "WAITING,IN_PROGRESS,SUBMITTED,SUPPLEMENT_REQUIRED,COMPLETED,OTHER" : "INCOMPLETE,COMPLETED,OTHER";
        if (result.Status is not null && !statuses.Split(',').Contains(result.Status) ||
            result.StatusGroup is not null && !groups.Split(',').Contains(result.StatusGroup) ||
            result.Mode is not null && (survey || result.Mode is not ("NORMAL" or "CONDITIONAL_FT" or "MEASURE_ONLY")) ||
            result.Acceptance is not null && (survey || result.Acceptance is not ("PENDING" or "ACCEPTED" or "REJECTED"))) throw new ArgumentException("Unknown task filter.");
        return result with { Search = string.IsNullOrEmpty(result.Search) ? null : result.Search };
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    public string Binding(Guid actor, string collection, int limit) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { actor, collection, limit, filter = this }))));
    public static string Encode(string binding, DateTimeOffset at, Guid id) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"mobile1|{binding}|{at.UtcTicks.ToString(CultureInfo.InvariantCulture)}|{id:D}"));
    public static (DateTimeOffset? At, Guid? Id) Decode(string? cursor, string binding)
    {
        if (string.IsNullOrEmpty(cursor)) return (null, null);
        if (cursor.Length > 1024) throw new ArgumentException("Invalid cursor.");
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split('|');
            if (parts.Length != 4 || parts[0] != "mobile1" || parts[1] != binding || !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) || !Guid.TryParse(parts[3], out var id)) throw new ArgumentException("Cursor does not match this query.");
            return (new DateTimeOffset(ticks, TimeSpan.Zero), id);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException) { throw new ArgumentException("Invalid cursor.", ex); }
    }
}
