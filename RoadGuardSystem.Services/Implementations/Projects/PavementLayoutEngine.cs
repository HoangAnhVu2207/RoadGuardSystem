using RoadGuardSystem.DTOs.Projects;

namespace RoadGuardSystem.Services.Projects;

public static class PavementLayoutEngine
{
    public static PavementLayoutPreview Plan(decimal analyticLengthMeters, PavementLayoutInput input)
    {
        const int maximumCells = 100000;
        ArgumentNullException.ThrowIfNull(input);
        if (analyticLengthMeters <= 0 || input.StripWidthMeters <= 0 || input.SlabLengthMeters <= 0 ||
            input.WidthProfile is null || input.WidthProfile.Length == 0 || input.WidthProfile.Length > maximumCells)
            throw new ArgumentException("Positive dimensions and a complete width profile are required.", nameof(input));
        if (input.SlabLengthMeters < analyticLengthMeters / int.MaxValue)
            throw new ArgumentException("The longitudinal grid exceeds supported row numbering.", nameof(input));
        decimal previousEnd = 0;
        foreach (var interval in input.WidthProfile)
        {
            if (interval is null || interval.FromOffsetMeters != previousEnd || interval.ToOffsetMeters <= interval.FromOffsetMeters ||
                interval.ToOffsetMeters > analyticLengthMeters || interval.WidthMeters <= 0 ||
                interval.WidthMeters % input.StripWidthMeters != 0)
                throw new ArgumentException("Width intervals must cover the alignment contiguously and divide into equal strips exactly.", nameof(input));
            if (input.StripWidthMeters < interval.WidthMeters / maximumCells ||
                input.SlabLengthMeters < (interval.ToOffsetMeters - interval.FromOffsetMeters) / maximumCells)
                throw new ArgumentException("The layout exceeds the bounded cell count.", nameof(input));
            var strips = interval.WidthMeters / input.StripWidthMeters;
            var rows = decimal.Ceiling((interval.ToOffsetMeters - interval.FromOffsetMeters) / input.SlabLengthMeters);
            if (strips > maximumCells || rows > maximumCells / strips)
                throw new ArgumentException("The layout exceeds the bounded cell count.", nameof(input));
            previousEnd = interval.ToOffsetMeters;
        }
        if (previousEnd != analyticLengthMeters)
            throw new ArgumentException("The width profile must cover the complete analytic alignment.", nameof(input));

        var cells = new List<PlannedSlabCell>();
        foreach (var interval in input.WidthProfile)
        {
            var offset = interval.FromOffsetMeters;
            var strips = (int)(interval.WidthMeters / input.StripWidthMeters);
            while (offset < interval.ToOffsetMeters)
            {
                var rowIndex = decimal.Floor(offset / input.SlabLengthMeters);
                if (rowIndex >= int.MaxValue)
                    throw new ArgumentException("The longitudinal grid exceeds supported row numbering.", nameof(input));
                var rowStart = rowIndex * input.SlabLengthMeters;
                var rowEnd = rowStart > decimal.MaxValue - input.SlabLengthMeters
                    ? decimal.MaxValue : rowStart + input.SlabLengthMeters;
                var end = Math.Min(interval.ToOffsetMeters, rowEnd);
                var residual = end == analyticLengthMeters && analyticLengthMeters % input.SlabLengthMeters != 0;
                var transition = offset != rowStart || end != rowEnd && end != analyticLengthMeters;
                if (cells.Count > maximumCells - strips)
                    throw new ArgumentException("The layout exceeds the bounded cell count.", nameof(input));
                for (var strip = 0; strip < strips; strip++)
                    cells.Add(new(cells.Count + 1, (int)rowIndex + 1, strip + 1, offset, end,
                        strip * input.StripWidthMeters - interval.WidthMeters / 2,
                        (strip + 1) * input.StripWidthMeters - interval.WidthMeters / 2, residual, transition));
                offset = end;
            }
        }
        var warnings = new List<string>();
        if (cells.Any(c => c.IsTerminalResidual)) warnings.Add("EXPLICIT_TERMINAL_RESIDUAL");
        if (input.WidthProfile.Length > 1) warnings.Add("WIDTH_TRANSITION_BOUNDARIES");
        return new(analyticLengthMeters, input.StripWidthMeters, input.SlabLengthMeters, cells.ToArray(), warnings.ToArray());
    }
}
