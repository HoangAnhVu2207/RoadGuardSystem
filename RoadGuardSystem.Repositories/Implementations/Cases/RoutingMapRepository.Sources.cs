using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Spatial;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed partial class RoutingMapRepository
{
    private Task<string> RegistryVersionAsync(CancellationToken token) => new RoutingSourceVersions(db, files).RegistryVersionAsync(token);
    private Task<string> CaseSourceVersionAsync(Guid caseId, CancellationToken token) => new RoutingSourceVersions(db, files).CaseSourceVersionAsync(caseId, token);

    private async Task<RoutingEvidencePin> EvidenceAsync(Guid caseId, Guid reportId, Guid? evidenceId, CancellationToken token)
    {
        if (!await db.Set<HuyCaseReportLink>().AsNoTracking().AnyAsync(l => l.CaseId == caseId && l.ReportId == reportId && l.EndedAt == null, token))
            Fail(404, "not_found");
        var report = await db.Reports.AsNoTracking().AsSplitQuery().Include(r => r.OriginalEvidence)
            .Include(r => r.Supplements).ThenInclude(s => s.Evidence).SingleAsync(r => r.Id == reportId, token);
        var evidence = evidenceId is null ? null : report.OriginalEvidence.Concat(report.Supplements.SelectMany(s => s.Evidence))
            .SingleOrDefault(e => e.Id == evidenceId);
        if (evidenceId is not null && evidence is null) Fail(404, "not_found");
        if (evidence is not null)
        {
            var file = await files.GetFileAsync(evidence.FileId, token);
            if (file is null || file.OwnerId != report.ReporterUserId || file.ProjectId is not null || file.Purpose != "REPORT_PHOTO")
                Fail(404, "not_found");
            if (!await db.FileScopes.AsNoTracking().AnyAsync(s => s.FileId == evidence.FileId && s.TargetId == null &&
                s.ProjectId == null && s.OwnerUserId == report.ReporterUserId && s.Purpose == "REPORT_PHOTO", token))
                Fail(404, "not_found");
            if (file!.State != "VERIFIED") Fail(409, "source_not_ready");
            if (file.FileVersion != evidence.FileVersion) Fail(412, "routing_source_stale");
        }
        var reportVersion = await db.Reports.Where(r => r.Id == reportId).Select(r => EF.Property<byte[]>(r, "RowVersion")).SingleAsync(token);
        var caseVersion = await db.IncidentCases.Where(c => c.Id == caseId).Select(c => EF.Property<byte[]>(c, "RowVersion")).SingleAsync(token);
        var capture = evidence?.CaptureMetadata;
        return new(caseId, Convert.ToBase64String(caseVersion), reportId, Convert.ToBase64String(reportVersion), evidence?.Id,
            evidence?.SupplementId, evidence?.FileId, evidence?.FileVersion, await CaseSourceVersionAsync(caseId, token),
            capture?.Latitude, capture?.Longitude, capture?.AccuracyMeters,
            capture?.LocationSource.ToString().ToUpperInvariant() ?? "UNKNOWN", capture?.CapturedAt,
            evidence?.SupplementId is Guid supplement ? report.Supplements.Single(s => s.Id == supplement).ReceivedAt : report.ReceivedAt);
    }

    public Task<RoutingCandidatesView> CandidatesAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId,
        Guid? evidenceId, int limit, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess,
        CancellationToken token) => InReadAsync<RoutingCandidatesView>(async () =>
    {
        await GuardAsync(actor, role, caseId, projectAccess, token);
        CheckLimit(limit);
        var source = await EvidenceAsync(caseId, reportId, evidenceId, token);
        var project = role == UserRoleCode.ProjectManager
            ? await db.IncidentCases.Where(c => c.Id == caseId).Select(c => c.ProjectId).SingleAsync(token) : null;
        var registry = await RegistryVersionAsync(token);
        var evaluation = Evaluation(source, registry, project);
        var filter = Hash(new { caseId, reportId, evidenceId, limit, project });
        var after = DecodeCursor(cursor, actor, "candidates", filter, evaluation);
        if (source.Latitude is null || source.Longitude is null)
            return new(source, evaluation, Algorithm, DateTimeOffset.UtcNow, [], null, "INCOMPLETE",
                [evidenceId is null ? "EVIDENCE_NOT_SELECTED" : "GPS_MISSING"], project is null ? "ORGANIZATION_ALL_VERSIONS" : "CURRENT_CASE_PROJECT_ALL_VERSIONS");
        var heads = await HeadsAsync(project, true, after, limit + 1, token);
        var page = heads.Take(limit).ToArray();
        var ids = page.Where(x => x.Points <= MaxVertices).Select(x => x.Id).ToArray();
        var shapes = await db.RoadSectionVersions.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Geometry }).ToDictionaryAsync(x => x.Id, x => x.Geometry, token);
        var candidates = new List<RoutingCandidate>();
        foreach (var head in page)
        {
            var route = await PropertiesAsync(head, token);
            var shape = shapes.GetValueOrDefault(head.Id);
            var reasons = new List<string> { "AUTHORITATIVE_CORRIDOR_UNAVAILABLE", "EVIDENCE_SCOPE_NOT_CONFIRMED_FOR_RESPONSIBILITY",
                "VERIFIED_EVENT_TIME_UNAVAILABLE" };
            var invalid = GeometryReason(head, shape);
            double? distance = null;
            if (invalid is not null) reasons.Add(invalid);
            else if (Wgs84MetricProjection.Project((double)source.Longitude.Value, (double)source.Latitude.Value, shape!.SRID) is { } point)
            {
                var measured = shape.Distance(new Point(point) { SRID = shape.SRID });
                if (double.IsFinite(measured) && measured >= 0) distance = measured;
                else reasons.Add("DISTANCE_UNAVAILABLE");
            }
            else reasons.Add("SOURCE_OUTSIDE_SUPPORTED_CRS_AREA");
            if (source.AccuracyMeters is null) reasons.Add("SOURCE_ACCURACY_MISSING");
            if (route.CoveragePins.Count == 0) reasons.Add("RESPONSIBILITY_SOURCE_MISSING");
            foreach (var pin in route.CoveragePins)
            {
                if (pin.Provenance != "REAL_SOURCE") reasons.Add("TEST_ONLY_MAPPING");
                var mapping = await db.Set<RoadCoverageMapping>().AsNoTracking().SingleAsync(m => m.Id == pin.Id, token);
                if (!await RoadCoverageResolver.CurrentSources(db, mapping, token)) reasons.Add("RESPONSIBILITY_SOURCE_STALE");
                if (source.ReceivedAt < pin.ApplicableFromUtc || source.ReceivedAt >= pin.ApplicableToUtc)
                    reasons.Add("MAPPING_NOT_APPLICABLE_AT_SERVER_RECEIPT");
            }
            candidates.Add(new(route, distance, distance is null ? null : "WGS84_UTM_GRID_MINIMUM_TO_CENTERLINE",
                "UNKNOWN", "UNKNOWN", "UNKNOWN", "SERVER_RECEIVED_AT_NOT_HISTORICAL_PROOF", source.ReceivedAt,
                reasons.Distinct(StringComparer.Ordinal).ToArray()));
        }
        return new(source, evaluation, Algorithm, DateTimeOffset.UtcNow, candidates,
            heads.Length > limit ? EncodeCursor(actor, "candidates", filter, evaluation, page[^1].Id) : null,
            "INCOMPLETE", ["SPATIAL_DISTANCE_IS_NOT_RESPONSIBILITY"], project is null ? "ORGANIZATION_ALL_VERSIONS" : "CURRENT_CASE_PROJECT_ALL_VERSIONS");
    }, token);

    private static string Evaluation(RoutingEvidencePin source, string registry, Guid? project)
        // Case version changes when a decision is saved; the source/link/report pins remain stable for replays and readback.
        => Hash(new { source = source with { CaseVersion = "" }, registry, project, Algorithm });
}
