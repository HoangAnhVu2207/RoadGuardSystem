namespace RoadGuardSystem.Services.Candidates;

// Repository/producer facts only. MetricDistanceMeters must originate in a valid
// shared metric CRS; capture lat/lon must never be passed as metric coordinates.
public sealed record CandidateMatchFact(Guid DefectId, Guid ProjectId, string Version, Guid? SegmentId,
    double? MetricDistanceMeters, bool HistoricalLink, bool AccuracyOverlap = false);
public sealed record CandidateMatchItem(Guid DefectId, string Version, Guid? SegmentId, int PriorityGroup,
    double? DistanceMeters, IReadOnlyList<string> ReasonCodes);
public static class CandidateMatcher
{
    public static IReadOnlyList<CandidateMatchItem> Match(Guid project, IReadOnlyCollection<Guid> assignedSegments,
        IReadOnlyCollection<Guid> neighborSegments, IReadOnlyCollection<CandidateMatchFact> facts, bool expand, bool metricGpsAvailable)
    {
        if (project == Guid.Empty) throw new ArgumentException("Project is required.", nameof(project));
        if (facts.Any(f => f.DefectId == Guid.Empty || string.IsNullOrWhiteSpace(f.Version) ||
            f.MetricDistanceMeters is double d && (!double.IsFinite(d) || d < 0))) throw new ArgumentException("Invalid authoritative candidate facts.", nameof(facts));
        return facts.Where(f => f.ProjectId == project).Select(f =>
        {
            var group = f.SegmentId is Guid s && assignedSegments.Contains(s) ? 0 : f.SegmentId is Guid n && neighborSegments.Contains(n) ? 1 : 2;
            var reasons = new List<string> { group == 0 ? "ASSIGNED_SEGMENT" : group == 1 ? "NEIGHBOR_SEGMENT" : "PROJECT_EXPANSION" };
            if (!metricGpsAvailable) reasons.Add("GPS_MISSING");
            if (metricGpsAvailable && f.AccuracyOverlap) reasons.Add("ACCURACY_OVERLAP");
            if (f.HistoricalLink) reasons.Add("HISTORICAL_LINK");
            return new { Item = new CandidateMatchItem(f.DefectId, f.Version, f.SegmentId, group,
                metricGpsAvailable ? f.MetricDistanceMeters : null, reasons), Overlap = metricGpsAvailable && f.AccuracyOverlap, f.HistoricalLink };
        }).Where(x => expand || x.Item.PriorityGroup < 2).OrderBy(x => x.Item.PriorityGroup).ThenByDescending(x => x.Overlap)
            .ThenBy(x => x.Item.DistanceMeters ?? double.PositiveInfinity).ThenByDescending(x => x.HistoricalLink).ThenBy(x => x.Item.DefectId)
            .Select(x => x.Item).ToArray();
    }
}
