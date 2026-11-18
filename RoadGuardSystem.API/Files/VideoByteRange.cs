using System.Globalization;

namespace RoadGuardSystem.API.Files;

public static class VideoByteRange
{
    public static bool TryParse(string header, long size, out long first, out long last)
    {
        first = 0; last = size - 1;
        if (size <= 0 || !header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)) return false;
        var parts = header[6..].Trim().Split('-');
        if (parts.Length != 2) return false;
        static bool Number(string value, out long result) => long.TryParse(value,
            NumberStyles.None, CultureInfo.InvariantCulture, out result);
        if (parts[0].Length == 0)
        {
            if (!Number(parts[1], out var suffix) || suffix == 0) return false;
            first = suffix >= size ? 0 : size - suffix;
            return true;
        }
        if (!Number(parts[0], out first) || first >= size) return false;
        if (parts[1].Length != 0)
        {
            if (!Number(parts[1], out last) || last < first) return false;
            last = Math.Min(last, size - 1);
        }
        return true;
    }
}
