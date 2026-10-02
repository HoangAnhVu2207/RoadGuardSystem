using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public static class ReportingDefinitions
{
    public const string Schema = "anh02.reporting.v1";
    public static readonly string[] Codes = ["reportsReceived", "casesByStatus", "defectsByStatus", "surveyTasksByStatus", "legacyUnclassified", "baselineCoverageByBand", "verifiedSourceBytes", "repairItemsByStatus", "repairAcceptanceRate", "validationMetrics"];
    public static readonly ReportingAvailabilityDto TimelineAvailability = new("PARTIAL", ["MAPPED_ANH01_AUDITS_ONLY", "HUY_TIMELINE_READER_UNAVAILABLE"]);
    public static ReportingCaptureDto Create(Guid project, DateTimeOffset readAt, ReportingFiltersDto filters, ReportingFacts facts)
    {
        var metrics = new List<ReportingMetricDto>(); var items = new List<ReportingItemDto>();
        foreach (var code in new[] { "defectsByStatus", "repairItemsByStatus", "repairAcceptanceRate" })
            metrics.Add(new(code, new(), null, code == "repairAcceptanceRate" ? "percent" : "count", null, null,
                code is "reportsReceived" or "repairAcceptanceRate", "UNAVAILABLE", [code.StartsWith("repair", StringComparison.Ordinal) ? "HUY02_REPAIR_FACTS_UNAVAILABLE" : "HUY01_REPORTING_READER_UNAVAILABLE"], []));
        if (facts.Intake is { } intake)
        {
            string[] incomplete = ["HUY_CASE_LIFECYCLE_NOT_INTEGRATED"];
            var reports = intake.Reports.DistinctBy(r => r.Id).ToArray();
            metrics.Add(new("reportsReceived", new(), reports.LongLength, "count", null, null, true, "PARTIAL", incomplete,
                reports.SelectMany(r => new[] { new ReportingSourceRefDto("Report", r.Id, r.Version), new ReportingSourceRefDto("IncidentCase", r.CaseId, r.CaseVersion) }).Distinct().ToArray()));
            items.AddRange(reports.Select(r => new ReportingItemDto(r.Id, "reportsReceived", "Report", r.Version)));
            var cases = intake.Cases.DistinctBy(c => c.Id).ToArray();
            foreach (var group in cases.GroupBy(c => c.Status).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                metrics.Add(new("casesByStatus", new(Status: group.Key), group.LongCount(), "count", null, null, false, "PARTIAL", incomplete,
                    group.Select(c => new ReportingSourceRefDto("IncidentCase", c.Id, c.Version)).ToArray()));
                items.AddRange(group.Select(c => new ReportingItemDto(c.Id, "casesByStatus", "IncidentCase", c.Version, c.Status)));
            }
            if (cases.Length == 0) metrics.Add(new("casesByStatus", new(), 0, "count", null, null, false, "PARTIAL", incomplete, []));
        }
        else
        {
            var reason = facts.Warnings.Contains("REPORTER_SPATIAL_SCOPE_UNAVAILABLE") ? "REPORTER_SPATIAL_SCOPE_UNAVAILABLE" : "HUY01_REPORTING_READER_UNAVAILABLE";
            foreach (var code in new[] { "reportsReceived", "casesByStatus" })
                metrics.Add(new(code, new(), null, "count", null, null, code == "reportsReceived", "UNAVAILABLE", [reason], []));
        }
        var tasks = facts.Tasks.DistinctBy(t => t.Id).ToArray();
        foreach (var group in tasks.Where(t => !t.Legacy).GroupBy(t => t.Status).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            metrics.Add(new("surveyTasksByStatus", new(Status: group.Key), group.LongCount(), "count", null, null, false, "AVAILABLE", [],
                group.Select(t => new ReportingSourceRefDto("SurveyRequest", t.Id, t.Version)).ToArray()));
            items.AddRange(group.Select(t => new ReportingItemDto(t.Id, "surveyTasksByStatus", "SurveyRequest", t.Version, t.Status, t.ParentTaskId)));
        }
        if (!tasks.Any(t => !t.Legacy)) metrics.Add(new("surveyTasksByStatus", new(), 0, "count", null, null, false, "AVAILABLE", [], []));
        var legacy = tasks.Where(t => t.Legacy).ToArray();
        var legacySpatialUnavailable = facts.Warnings.Contains("LEGACY_SPATIAL_SCOPE_UNAVAILABLE");
        metrics.Add(new("legacyUnclassified", new(), legacySpatialUnavailable ? null : legacy.LongLength, "count", null, null, false,
            legacySpatialUnavailable ? "UNAVAILABLE" : "AVAILABLE", legacySpatialUnavailable ? ["LEGACY_SPATIAL_SCOPE_UNAVAILABLE"] : [],
            legacy.Select(t => new ReportingSourceRefDto("SurveyRequest", t.Id, t.Version)).ToArray()));
        items.AddRange(legacy.Select(t => new ReportingItemDto(t.Id, "legacyUnclassified", "SurveyRequest", t.Version, t.Status, t.ParentTaskId)));
        var scopes = facts.Segments.Select(s => (s.RouteVersionId, s.SegmentSetId)).Concat(facts.PublishedSets.Select(s => (s.RouteVersionId, s.SegmentSetId))).Distinct().ToArray();
        if (scopes.Length == 0) scopes = [(filters.RouteVersionId ?? Guid.Empty, filters.SegmentSetId ?? Guid.Empty)];
        foreach (var (route, set) in scopes)
        foreach (var band in new[] { "SURFACE", "LEFT_EDGE", "RIGHT_EDGE" })
        {
            var segments = facts.Segments.Where(s => s.RouteVersionId == route && s.SegmentSetId == set).Select(s => s.SegmentId).Distinct().ToArray();
            var eligible = facts.Baselines.Where(b => b.RouteVersionId == route && b.SegmentSetId == set && b.Band == band && segments.Contains(b.SegmentId)).DistinctBy(b => b.SegmentId).ToArray();
            metrics.Add(new("baselineCoverageByBand", new(Band: band, RouteVersionId: route == Guid.Empty ? null : route, SegmentSetId: set == Guid.Empty ? null : set),
                segments.Length == 0 ? null : (decimal)eligible.Length * 100 / segments.Length, "percent", eligible.LongLength, segments.LongLength, false, "AVAILABLE",
                segments.Length == 0 ? ["EMPTY_DENOMINATOR"] : [], eligible.Select(b => new ReportingSourceRefDto("BaselineSelectionItem", b.Id, b.Id.ToString("N")))
                    .Concat(facts.Sources.Where(s => s.Type == "RoadSegmentSet" && s.Id == set || s.Type == "DatasetAssessment" && eligible.Any(b => b.AssessmentId == s.Id) ||
                        s.Type == "SurveyDataVersion" && eligible.Any(b => b.DatasetId == s.Id))).ToArray()));
            items.AddRange(eligible.Select(b => new ReportingItemDto(b.Id, "baselineCoverageByBand", "BaselineSelectionItem", b.Id.ToString("N"),
                RouteVersionId: route, SegmentSetId: set, SegmentId: b.SegmentId, Band: band, DatasetId: b.DatasetId, AssessmentId: b.AssessmentId)));
        }
        var files = facts.Files.DistinctBy(f => f.FileId).OrderBy(f => f.FileId).ToArray();
        var bytes = files.Aggregate(0L, (sum, file) => checked(sum + file.SizeBytes));
        var sourceWarnings = facts.Warnings.Where(w => w.StartsWith("SOURCE_", StringComparison.Ordinal) || w.StartsWith("SCOPE_", StringComparison.Ordinal)).ToArray();
        metrics.Add(new("verifiedSourceBytes", new(), bytes, "bytes", null, null, false, sourceWarnings.Length == 0 ? "AVAILABLE" : "PARTIAL", sourceWarnings,
            files.Select(f => new ReportingSourceRefDto("StoredFile", f.FileId, f.Version)).Concat(facts.Sources.Where(s => s.Type == "SurveyDataVersion")).ToArray()));
        items.AddRange(files.Select(f => new ReportingItemDto(f.FileId, "verifiedSourceBytes", "StoredFile", f.Version, SizeBytes: f.SizeBytes)));
        var validationFiltered = facts.Warnings.Contains("VALIDATION_GEOMETRY_FILTER_UNSUPPORTED");
        metrics.Add(new("validationMetrics", new(), null, "per-run", null, null, false, validationFiltered ? "UNAVAILABLE" : "AVAILABLE",
            validationFiltered ? ["VALIDATION_GEOMETRY_FILTER_UNSUPPORTED"] : ["PER_RUN_VALUES_IN_DRILLDOWN"], facts.Validations.Select(v => new ReportingSourceRefDto("ValidationRun", v.Id, v.Version)).ToArray()));
        items.AddRange(facts.Validations);
        return new(new(Schema, project, readAt, "pilot-reporting.v1", filters, metrics.ToArray(), facts.Warnings, facts.Isolation),
            items.DistinctBy(i => (i.Metric, i.Id)).ToArray(), files, facts.Timeline, TimelineAvailability);
    }
}
