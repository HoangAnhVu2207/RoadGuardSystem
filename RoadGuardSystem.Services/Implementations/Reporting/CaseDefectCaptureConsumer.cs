using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public static class CaseDefectCaptureConsumer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string Hash(CaseDefectSnapshotV1 snapshot) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(snapshot with { Hash = "" }, Json))).ToLowerInvariant();

    public static ReportingCaptureDto Apply(ReportingCaptureDto capture, CaseDefectSnapshotV1 snapshot)
    {
        var project = capture.Summary.ProjectId;
        var filters = capture.Summary.Filters;
        if (snapshot.SchemaVersion != "anh-huy.case-defect.v1" || snapshot.SnapshotId == Guid.Empty || snapshot.ProjectId != project
            || snapshot.CapturedAt.Offset != TimeSpan.Zero || snapshot.Hash != Hash(snapshot)
            || snapshot.Cases is null || snapshot.Defects is null || snapshot.AuthorizedEvidence is null || snapshot.MissingReasons is null
            || snapshot.Cases.Any(c => c is null || c.ProjectId != project || c.CaseId == Guid.Empty || string.IsNullOrWhiteSpace(c.Version)
                || string.IsNullOrWhiteSpace(c.Status) || c.CurrentReports is null || c.Conclusions is null || c.Publications is null
                || c.CurrentReports.Any(r => r is null || r.ReportId == Guid.Empty || string.IsNullOrWhiteSpace(r.Version) || r.ReceivedAt.Offset != TimeSpan.Zero)
                || c.CurrentReports.Select(r => r.ReportId).Distinct().Count() != c.CurrentReports.Length
                || c.Conclusions.Any(r => r is null || r.Id == Guid.Empty || string.IsNullOrWhiteSpace(r.Type) || string.IsNullOrWhiteSpace(r.Version))
                || c.Publications.Any(p => p is null || p.PublicationId == Guid.Empty || string.IsNullOrWhiteSpace(p.Version)
                    || p.RecipientReportIds is null || p.EvidenceIds is null
                    || p.RecipientReportIds.Any(id => id == Guid.Empty) || p.EvidenceIds.Any(id => id == Guid.Empty)))
            || snapshot.Defects.Any(d => d is null || d.ProjectId != project || d.DefectId == Guid.Empty || string.IsNullOrWhiteSpace(d.Version)
                || d.SourceId == Guid.Empty || string.IsNullOrWhiteSpace(d.SourceVersion) || d.SourceKind is not ("REPORT" or "AI_DETECTION")
                || filters.RouteVersionId.HasValue && d.RouteVersionId != filters.RouteVersionId
                || filters.SegmentSetId.HasValue && d.SegmentSetId != filters.SegmentSetId
                || filters.SegmentIds is { Length: > 0 } && !filters.SegmentIds.Contains(d.SegmentId ?? Guid.Empty))
            || snapshot.Cases.Select(c => c.CaseId).Distinct().Count() != snapshot.Cases.Length
            || snapshot.Defects.Select(d => d.DefectId).Distinct().Count() != snapshot.Defects.Length
            || snapshot.AuthorizedEvidence.Any(e => e is null || e.FileId == Guid.Empty || e.EvidenceId == Guid.Empty || e.SizeBytes <= 0 || e.Sha256?.Length != 64
                || !e.Sha256.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(e.MediaType)
                || e.SourceReportId == Guid.Empty || string.IsNullOrWhiteSpace(e.FileVersion) || !snapshot.Cases.Any(c => c.CaseId == e.CaseId))
            || snapshot.AuthorizedEvidence.Select(e => (e.CaseId, e.EvidenceId)).Distinct().Count() != snapshot.AuthorizedEvidence.Length)
            throw new InvalidOperationException("Case/Defect producer snapshot is invalid or outside requested scope.");
        // Incomplete producer facts are frozen for diagnosis but cannot replace
        // existing partial/unavailable metrics or make unknown counts zero.
        if (snapshot.MissingReasons.Length != 0) return capture with { CaseDefectFacts = snapshot };
        var metrics = capture.Summary.Metrics.Where(m => m.Code is not ("reportsReceived" or "casesByStatus" or "defectsByStatus")).ToList();
        var items = capture.Items.Where(i => i.Metric is not ("reportsReceived" or "casesByStatus" or "defectsByStatus")).ToList();
        var reports = snapshot.Cases.SelectMany(c => c.CurrentReports).DistinctBy(r => r.ReportId)
            .Where(r => (!filters.From.HasValue || r.ReceivedAt >= filters.From) && (!filters.To.HasValue || r.ReceivedAt < filters.To)).ToArray();
        metrics.Add(new("reportsReceived", new(), reports.LongLength, "count", null, null, true, "AVAILABLE", [],
            reports.Select(r => new ReportingSourceRefDto("Report", r.ReportId, r.Version)).ToArray()));
        items.AddRange(reports.Select(r => new ReportingItemDto(r.ReportId, "reportsReceived", "Report", r.Version)));
        foreach (var (code, type, rows) in new[] {
            ("casesByStatus", "IncidentCase", snapshot.Cases.Select(c => (Id:c.CaseId,c.Version,c.Status)).ToArray()),
            ("defectsByStatus", "Defect", snapshot.Defects.Select(d => (Id:d.DefectId,d.Version,d.Status)).ToArray()) })
        {
            foreach (var group in rows.GroupBy(r => r.Status).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                metrics.Add(new(code, new(Status: group.Key), group.LongCount(), "count", null, null, false, "AVAILABLE", [],
                    group.Select(r => new ReportingSourceRefDto(type, r.Id, r.Version)).ToArray()));
                items.AddRange(group.Select(r => new ReportingItemDto(r.Id, code, type, r.Version, r.Status)));
            }
            if (rows.Length == 0) metrics.Add(new(code, new(), 0, "count", null, null, false, "AVAILABLE", [], []));
        }
        // Keep private evidence out of generic Files/ZIP original-file admission;
        // a future current-source access contract is required for those bytes.
        return capture with { Summary = capture.Summary with { Metrics = metrics.ToArray() }, Items = items.ToArray(), CaseDefectFacts = snapshot };
    }
}
