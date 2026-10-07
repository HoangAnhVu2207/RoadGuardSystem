using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.DTOs.Reporting;
namespace RoadGuardSystem.Services.Reporting;

public static class DefectStatisticsCaptureConsumer
{
    public static ReportingCaptureDto Apply(ReportingCaptureDto capture, DefectStatisticsSnapshot statistics)
    {
        if (statistics.ProjectId != capture.Summary.ProjectId) throw new InvalidOperationException("Statistics are outside the requested project.");
        var refs = statistics.Sources.Select(row => new ReportingSourceRefDto("DefectStatisticsSource", row.Id, statistics.Version)).ToArray();
        var metrics = capture.Summary.Metrics.ToList();
        metrics.Add(new("projectDistinctDefects", new(), statistics.ProjectDistinctDefects, "count", null, null, false, "AVAILABLE", [], statistics.Defects.Select(row => new ReportingSourceRefDto("Defect", row.Id, row.Version)).ToArray()));
        metrics.Add(new("matchedDistinctDefects", new(), statistics.MatchedDistinctDefects, "count", null, null, false,
            statistics.MissingReasons.Length > 0 ? "PARTIAL" : "AVAILABLE", statistics.MissingReasons, refs));
        metrics.Add(new("explicitSharedParts", new(), statistics.SharedParts, "count", null, null, false,
            statistics.UnmappedDefects > 0 ? "PARTIAL" : "AVAILABLE", statistics.MissingReasons, refs));
        foreach (var segment in statistics.Segments)
            metrics.Add(new("segmentRelatedDefects", new(Status: segment.SegmentId.ToString("D")), segment.RelatedDefects, "count", null, null, false,
                segment.Availability, segment.Availability == "PARTIAL" ? ["DEFECT_SEGMENT_RELATION_UNKNOWN"] : [], refs));
        foreach (var group in statistics.Quantities.GroupBy(row => (row.Type, row.Unit, Allocated: row.SegmentId.HasValue)))
        {
            var rows = group.DistinctBy(row => row.MeasurementId).ToArray();
            var complete = rows.All(row => row.ValueState == "KNOWN") && statistics.MissingReasons.Length == 0;
            metrics.Add(new(group.Key.Allocated ? "sourcedAllocatedQuantity" : "sourcedUnallocatedQuantity", new(Status: group.Key.Type),
                complete ? rows.Sum(row => row.Value) : null, group.Key.Unit, null, null, false,
                complete ? "AVAILABLE" : "PARTIAL", complete ? [] : statistics.MissingReasons,
                rows.Where(row => row.MeasurementId != Guid.Empty).Select(row => new ReportingSourceRefDto("GroundTruthMeasurement", row.MeasurementId, row.SourceVersion)).ToArray()));
        }
        var items = capture.Items.Concat(statistics.Defects.Select(row => new ReportingItemDto(row.Id, "projectDistinctDefects", "Defect", row.Version)))
            .Concat(statistics.Sources.Select(row => new ReportingItemDto(row.Id, "matchedDistinctDefects", "DefectStatisticsSource", statistics.Version)))
            .Concat(statistics.Sources.DistinctBy(row => row.SharedPartId).Select(row => new ReportingItemDto(row.SharedPartId, "explicitSharedParts", "SharedRoadPart", statistics.Version)))
            .Concat(statistics.Segments.Select(row => new ReportingItemDto(row.SegmentId, "segmentRelatedDefects", "RoadSegment", statistics.Version)))
            .Concat(statistics.Quantities.Where(row => row.MeasurementId != Guid.Empty).Select(row => new ReportingItemDto(row.MeasurementId,
                row.SegmentId.HasValue ? "sourcedAllocatedQuantity" : "sourcedUnallocatedQuantity", "GroundTruthMeasurement", row.SourceVersion, Status: row.ValueState, SegmentId: row.SegmentId, Unit: row.Unit, MeasurementType: row.Type))).ToArray();
        return capture with { DefectStatistics = statistics, Items = items, Summary = capture.Summary with { Metrics = metrics.ToArray() } };
    }
}
