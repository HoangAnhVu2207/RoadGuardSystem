namespace RoadGuardSystem.Services.Processing.Worker;

/// <summary>Converts worker pixel/time coordinates into existing BE result coordinates.</summary>
public static class AiWorkerCoordinates
{
    public static decimal[] DecimalNormalizedBox(IReadOnlyList<double> box)
    {
        ArgumentNullException.ThrowIfNull(box);
        if (box.Count != 4 || box.Any(v => !double.IsFinite(v) || v < 0 || v > 1) ||
            box[2] <= 0 || box[3] <= 0 || box[0] + box[2] > 1 || box[1] + box[3] > 1)
            throw new ArgumentException("Expected an in-frame positive normalized xywh box.", nameof(box));
        // Convert endpoints, not separately rounded origin/extent. A valid edge
        // at 1 must stay at 1 in decimal without clamping an invalid input.
        var x = (decimal)box[0]; var y = (decimal)box[1];
        var right = (decimal)(box[0] + box[2]); var bottom = (decimal)(box[1] + box[3]);
        if (right <= x || bottom <= y) throw new ArgumentException("Normalized box loses positive decimal area.", nameof(box));
        return [x, y, right - x, bottom - y];
    }
    public static decimal[] NormalizeBox(IReadOnlyList<double> box, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(box);
        if (width <= 0 || height <= 0 || box.Count != 4 || box.Any(v => !double.IsFinite(v)) ||
            box[0] < 0 || box[1] < 0 || box[2] <= box[0] || box[3] <= box[1] ||
            box[2] > width || box[3] > height)
            throw new ArgumentException("Expected an in-frame positive pixel xyxy box.", nameof(box));
        // Convert first so independent floating-point divisions cannot make x + w exceed one.
        var x = (decimal)box[0] / width;
        var y = (decimal)box[1] / height;
        var right = (decimal)box[2] / width;
        var bottom = (decimal)box[3] / height;
        if (right <= x || bottom <= y)
            throw new ArgumentException("Pixel box loses positive area in the decimal result contract.", nameof(box));
        return [x, y, right - x, bottom - y];
    }

    public static int TimestampMilliseconds(decimal seconds)
    {
        if (seconds < 0 || seconds > int.MaxValue / 1000m)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        return decimal.ToInt32(decimal.Round(seconds * 1000m, 0, MidpointRounding.AwayFromZero));
    }
}
