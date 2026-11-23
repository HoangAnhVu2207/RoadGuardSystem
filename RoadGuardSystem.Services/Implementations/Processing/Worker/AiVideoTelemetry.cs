using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed record AiDroneInterval(long StartMs, long EndMs, decimal Latitude, decimal Longitude);

/// <summary>Drone reference positions only. Never ground coordinates, accuracy, interpolation or physical measurements.</summary>
public static class AiVideoTelemetry
{
    public const int MaximumSrtBytes = 2 * 1024 * 1024;
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(250);
    private static readonly Regex Timing = new(@"^(\d{2}):([0-5]\d):([0-5]\d),(\d{3})\s*-->\s*(\d{2}):([0-5]\d):([0-5]\d),(\d{3})$", RegexOptions.CultureInvariant, Timeout);
    private static readonly Regex Gps = new(@"(?<![A-Za-z])GPS\s*\(\s*([-+]?\d+(?:\.\d+)?)\s*,\s*([-+]?\d+(?:\.\d+)?)(?:\s*,\s*[-+]?\d+(?:\.\d+)?)?\s*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);
    private static readonly Regex Lat = new(@"\[(?:latitude|lat)\s*:\s*([-+]?\d+(?:\.\d+)?)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);
    private static readonly Regex Lon = new(@"\[(?:longitude|lon)\s*:\s*([-+]?\d+(?:\.\d+)?)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);

    public static IReadOnlyList<AiDroneInterval> ParseSrt(string srt, long durationMs, long offsetMs)
    {
        if (string.IsNullOrWhiteSpace(srt) || Encoding.UTF8.GetByteCount(srt) > MaximumSrtBytes || durationMs <= 0 || offsetMs is < -86400000 or > 86400000)
            throw new ArgumentException("Invalid bounded SRT input.");
        var normalized = srt.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var blocks = Regex.Split(normalized, @"\n(?:[ \t]*\n)+", RegexOptions.CultureInvariant, Timeout);
        if (blocks.Length > 20000) throw new ArgumentException("Too many telemetry cues.");
        var cues = new List<AiDroneInterval>();
        foreach (var block in blocks)
        {
            var lines = block.Split('\n');
            if (lines.Length < 3 || !int.TryParse(lines[0], NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index != cues.Count + 1)
                throw new ArgumentException("Invalid SRT cue numbering.");
            var timing = Timing.Match(lines[1]);
            if (!timing.Success) throw new ArgumentException("Invalid SRT cue interval.");
            var start = Milliseconds(timing, 1) + offsetMs;
            var end = Milliseconds(timing, 5) + offsetMs;
            var text = string.Join('\n', lines.Skip(2));
            var gps = Gps.Matches(text); var lat = Lat.Matches(text); var lon = Lon.Matches(text);
            decimal latitude; decimal longitude;
            if (gps.Count == 1 && lat.Count == 0 && lon.Count == 0)
            { longitude = Number(gps[0].Groups[1].Value); latitude = Number(gps[0].Groups[2].Value); }
            else if (gps.Count == 0 && lat.Count == 1 && lon.Count == 1)
            { latitude = Number(lat[0].Groups[1].Value); longitude = Number(lon[0].Groups[1].Value); }
            else throw new ArgumentException("Missing or ambiguous drone GPS.");
            Validate(start, end, latitude, longitude, durationMs);
            if (cues.Count > 0 && start < cues[^1].EndMs) throw new ArgumentException("Overlapping telemetry cues.");
            cues.Add(new(start, end, latitude, longitude));
        }
        return cues;
    }
    public static AiDroneInterval Estimate(long startMs, long endMs, decimal latitude, decimal longitude, string reason, long durationMs)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000 || reason.Any(char.IsControl)) throw new ArgumentException("An estimate reason is required.");
        Validate(startMs, endMs, latitude, longitude, durationMs);
        return new(startMs, endMs, latitude, longitude);
    }
    public static AiDroneInterval? At(IReadOnlyList<AiDroneInterval> cues, long timestampMs)
        => cues.SingleOrDefault(c => timestampMs >= c.StartMs && timestampMs < c.EndMs);
    private static decimal Number(string value) => decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
        ? number : throw new ArgumentException("Invalid GPS number.");
    private static long Milliseconds(Match match, int first)
        => long.Parse(match.Groups[first].Value, CultureInfo.InvariantCulture) * 3600000 + long.Parse(match.Groups[first + 1].Value, CultureInfo.InvariantCulture) * 60000 +
            long.Parse(match.Groups[first + 2].Value, CultureInfo.InvariantCulture) * 1000 + long.Parse(match.Groups[first + 3].Value, CultureInfo.InvariantCulture);
    private static void Validate(long start, long end, decimal latitude, decimal longitude, long duration)
    {
        if (duration <= 0 || start < 0 || end <= start || end > duration || latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentException("Drone pin or interval is out of bounds.");
    }
}
