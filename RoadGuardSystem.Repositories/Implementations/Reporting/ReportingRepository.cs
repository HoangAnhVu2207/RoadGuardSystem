using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Reporting;

public sealed partial class ReportingRepository(RoadGuardDbContext db) : IReportingRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<T> ReadConsistentlyAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is { } existing)
        {
            if (await ReadIsolationAsync(token) is not (IsolationLevel.Serializable or IsolationLevel.Snapshot))
                throw new InvalidOperationException("Reporting capture requires SERIALIZABLE or SNAPSHOT admission isolation.");
            return await read(token);
        }
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await read(token);
            await transaction.CommitAsync(token);
            return result;
        });
    }

    public async Task<ReportingReadResult> CaptureAsync(Guid project, ReportingFiltersFact filters, CancellationToken token)
    {
        filters = filters with { SegmentIds = filters.SegmentIds ?? [] };
        var filterSegments = filters.SegmentIds!;
        if (!await db.Projects.AsNoTracking().AnyAsync(x => x.Id == project, token)) return new("not_found");
        var routes = await (from r in db.RoadSectionVersions.AsNoTracking()
                            join s in db.RoadSections.AsNoTracking() on r.RoadSectionId equals s.Id
                            where s.ProjectId == project
                            select new { r.Id, r.IsCurrent }).ToListAsync(token);
        var routeIds = routes.Select(x => x.Id).ToArray();
        var sets = await db.RoadSegmentSets.AsNoTracking().Where(x => routeIds.Contains(x.RoadSectionVersionId)).ToListAsync(token);
        var setIds = sets.Select(x => x.Id).ToArray();
        var segments = await db.RoadSegments.AsNoTracking().Where(x => setIds.Contains(x.SegmentSetId)).ToListAsync(token);
        if (filters.RouteVersionId is { } route && !routeIds.Contains(route) || filters.SegmentSetId is { } set &&
            !sets.Any(x => x.Id == set && (!filters.RouteVersionId.HasValue || x.RoadSectionVersionId == filters.RouteVersionId)) ||
            filters.SegmentIds!.Any(id => !segments.Any(s => s.Id == id && (!filters.RouteVersionId.HasValue || s.RoadSectionVersionId == filters.RouteVersionId) &&
                (!filters.SegmentSetId.HasValue || s.SegmentSetId == filters.SegmentSetId)))) return new("not_found");
        var currentSets = sets.Where(s => s.Status == "PUBLISHED" && routes.Any(r => r.Id == s.RoadSectionVersionId && r.IsCurrent) &&
            (!filters.RouteVersionId.HasValue || s.RoadSectionVersionId == filters.RouteVersionId) && (!filters.SegmentSetId.HasValue || s.Id == filters.SegmentSetId)).ToArray();
        var currentSetIds = currentSets.Select(x => x.Id).ToArray();
        var selectedSegments = segments.Where(s => currentSetIds.Contains(s.SegmentSetId) && (filterSegments.Length == 0 || filterSegments.Contains(s.Id)))
            .Select(s => new ReportingSegmentFact(s.RoadSectionVersionId, s.SegmentSetId, s.Id)).ToArray();
        var warnings = new HashSet<string>();
        var spatialIntake = filters.RouteVersionId.HasValue || filters.SegmentSetId.HasValue || filterSegments.Length > 0;
        ReportingIntakeFacts? intake = null;
        if (spatialIntake) warnings.Add("REPORTER_SPATIAL_SCOPE_UNAVAILABLE");
        else
        {
            var caseRows = await db.IncidentCases.AsNoTracking().Where(c => c.ProjectId == project)
                .Select(c => new { c.Id, c.Status, Version = EF.Property<byte[]>(c, "RowVersion") }).ToArrayAsync(token);
            var reportQuery = from r in db.Reports.AsNoTracking()
                              join l in db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().AsNoTracking() on r.Id equals l.ReportId
                              join c in db.IncidentCases.AsNoTracking() on l.CaseId equals c.Id
                              where l.EndedAt == null && c.ProjectId == project
                              select new { r.Id, r.ReceivedAt, Version = EF.Property<byte[]>(r, "RowVersion"), CaseId = c.Id, CaseVersion = EF.Property<byte[]>(c, "RowVersion") };
            if (filters.From is { } reportFrom) reportQuery = reportQuery.Where(r => r.ReceivedAt >= reportFrom);
            if (filters.To is { } reportTo) reportQuery = reportQuery.Where(r => r.ReceivedAt < reportTo);
            var reportRows = await reportQuery.ToArrayAsync(token);
            intake = new(reportRows.DistinctBy(r => r.Id).OrderBy(r => r.Id).Select(r => new ReportingReportFact(r.Id, Convert.ToBase64String(r.Version), r.CaseId, Convert.ToBase64String(r.CaseVersion))).ToArray(),
                caseRows.OrderBy(c => c.Id).Select(c => new ReportingCaseFact(c.Id, Convert.ToBase64String(c.Version), c.Status.ToString().ToUpperInvariant())).ToArray());
        }
        var requests = await db.SurveyRequests.AsNoTracking().Where(x => x.ProjectId == project).ToListAsync(token);
        var requestIds = requests.Select(x => x.Id).ToArray();
        var scopes = await db.SurveyRequestScopes.AsNoTracking().Where(x => requestIds.Contains(x.SurveyRequestId)).ToListAsync(token);
        var spatial = filters.RouteVersionId.HasValue || filters.SegmentSetId.HasValue || filterSegments.Length > 0;
        if (spatial && requests.Any(t => t.ScopeFormatVersion != "BAND_V1" && !scopes.Any(s => s.SurveyRequestId == t.Id)))
            warnings.Add("LEGACY_SPATIAL_SCOPE_UNAVAILABLE");
        var tasks = requests.Where(t => !spatial || scopes.Any(s => s.SurveyRequestId == t.Id && MatchesScope(s.RouteSectionVersionId, s.SegmentSetId, s.SegmentIdsJson, filters, warnings)))
            .Select(t => new ReportingTaskFact(t.Id, TaskStatus(t.Status), t.ParentTaskId, Convert.ToBase64String(t.RowVersion), t.ScopeFormatVersion != "BAND_V1")).ToArray();
        var datasetRows = await (from d in db.SurveyDataVersions.AsNoTracking()
                                 join s in db.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                                 join t in db.SurveyRequests.AsNoTracking() on s.SurveyRequestId equals t.Id
                                 where s.ProjectId == project && t.ProjectId == project && t.ScopeFormatVersion == "BAND_V1" && d.ScopeManifest != null
                                 select d).ToListAsync(token);
        var datasets = datasetRows.Where(d => MatchesManifest(d.ScopeManifest!, filters, warnings)).ToArray();
        var datasetIds = datasets.Select(d => d.Id).ToArray();
        var sourceIds = new HashSet<Guid>();
        var manifests = new List<SourceFile>();
        foreach (var d in datasets)
        {
            try
            {
                foreach (var f in JsonSerializer.Deserialize<SourceFile[]>(d.SourceManifest, Json) ?? [])
                { if (f is null || f.FileId == Guid.Empty) warnings.Add("SOURCE_MANIFEST_UNREADABLE"); else { sourceIds.Add(f.FileId); manifests.Add(f); } }
            }
            catch (JsonException) { warnings.Add("SOURCE_MANIFEST_UNREADABLE"); }
        }
        var ids = sourceIds.ToArray();
        var fileRows = await (from f in db.Files.AsNoTracking()
                              join u in db.UploadSessions.AsNoTracking() on f.Id equals u.FileId
                              where ids.Contains(f.Id) && u.Status == UploadSessionStatus.Verified && (u.Purpose == "SURVEY_VIDEO" || u.Purpose == "TELEMETRY") &&
                                  u.ExpectedChecksumSha256 == f.Checksum && u.ExpectedSizeBytes == f.SizeBytes && u.MediaType == f.MimeType && u.OwnerUserId == f.UploadedByUserId &&
                                  db.FileScopes.Any(s => s.FileId == f.Id && s.ProjectId == project && s.Purpose == u.Purpose && s.OwnerUserId == u.OwnerUserId)
                              select new { f.Id, f.Checksum, f.SizeBytes, f.MimeType, u.Purpose, u.RowVersion }).ToArrayAsync(token);
        var files = fileRows.Where(f => manifests.Where(m => m.FileId == f.Id).All(m => m.ChecksumSha256 == f.Checksum && m.SizeBytes == f.SizeBytes && m.MediaType == f.MimeType && m.Purpose == f.Purpose))
            .Select(f => new ReportingFileFact(f.Id, Convert.ToBase64String(f.RowVersion), f.Checksum, f.SizeBytes, f.MimeType)).DistinctBy(f => f.FileId).ToArray();
        if (fileRows.Length != files.Length) warnings.Add("SOURCE_MANIFEST_METADATA_MISMATCH");
        if (files.Length != ids.Length) warnings.Add("SOURCE_FILES_NOT_VERIFIED");
        var baselineRows = await (from p in db.Set<BaselineCurrentPointer>().AsNoTracking()
                                  join b in db.Set<BaselineSelectionItem>().AsNoTracking() on p.SelectionId equals b.Id
                                  join a in db.Set<DatasetAssessment>().AsNoTracking() on b.AssessmentId equals a.Id
                                  join i in db.Set<DatasetAssessmentItem>().AsNoTracking() on a.Id equals i.AssessmentId
                                  where p.ProjectId == project && datasetIds.Contains(b.DatasetId) && a.DatasetId == b.DatasetId && a.MethodVersion == "pm-evidence-review.v1" &&
                                      p.RouteVersionId == b.RouteVersionId && p.SegmentSetId == b.SegmentSetId && p.SegmentId == b.SegmentId && p.TargetBand == b.TargetBand &&
                                      i.RouteVersionId == b.RouteVersionId && i.SegmentSetId == b.SegmentSetId && i.SegmentId == b.SegmentId && i.TargetBand == b.TargetBand &&
                                      i.PositionStatus == "PASS" && i.QualityStatus == "PASS" && i.CoverageStatus == "PASS"
                                  select new { b.Id, b.RouteVersionId, b.SegmentSetId, b.SegmentId, b.TargetBand, b.DatasetId, b.AssessmentId }).ToListAsync(token);
        var baselines = baselineRows.Where(b => selectedSegments.Any(s => s.RouteVersionId == b.RouteVersionId && s.SegmentSetId == b.SegmentSetId && s.SegmentId == b.SegmentId))
            .Select(b => new ReportingBaselineFact(b.Id, b.RouteVersionId, b.SegmentSetId, b.SegmentId, b.TargetBand, b.DatasetId, b.AssessmentId)).Distinct().ToArray();
        if (baselineRows.Count != baselines.Length) warnings.Add("STALE_OR_OUT_OF_SCOPE_BASELINES_EXCLUDED");
        var validations = spatial ? [] : await db.ValidationRuns.AsNoTracking().Where(v => v.ProjectId == project)
            .Select(v => new { v.Id, v.RowVersion, v.Status, v.Unit, v.Bias, v.Mae, v.Rmse, v.UsedCount, v.ExcludedCount, v.ModelVersionId, v.DatasetSplitId, v.MeasurementType }).ToArrayAsync(token);
        var validationItems = validations.Select(v => new ReportingItemFact(v.Id, "validationMetrics", "ValidationRun", Convert.ToBase64String(v.RowVersion),
            Status: v.Status.ToString(), Unit: v.Unit, Bias: v.Bias, Mae: v.Mae, Rmse: v.Rmse, Used: v.UsedCount, Excluded: v.ExcludedCount, ModelVersionId: v.ModelVersionId, SplitId: v.DatasetSplitId, MeasurementType: v.MeasurementType)).ToArray();
        if (spatial) warnings.Add("VALIDATION_GEOMETRY_FILTER_UNSUPPORTED");
        var assessmentRows = await db.Set<DatasetAssessment>().AsNoTracking().Where(a => datasetIds.Contains(a.DatasetId)).Select(a => new { a.Id, a.RowVersion }).ToArrayAsync(token);
        var assessmentIds = assessmentRows.Select(a => a.Id).ToArray();
        var batchQuery = db.Set<BaselineSelection>().AsNoTracking().Where(b => b.ProjectId == project);
        if (spatial) batchQuery = batchQuery.Where(b => db.Set<BaselineSelectionItem>().Any(i => i.BaselineSelectionId == b.Id &&
            (!filters.RouteVersionId.HasValue || i.RouteVersionId == filters.RouteVersionId) && (!filters.SegmentSetId.HasValue || i.SegmentSetId == filters.SegmentSetId) &&
            (filterSegments.Length == 0 || filterSegments.Contains(i.SegmentId))));
        var batchIds = await batchQuery.Select(b => b.Id).ToArrayAsync(token);
        var taskIds = tasks.Select(t => t.Id).ToArray();
        var audits = db.AuditLogs.AsNoTracking().Where(a => taskIds.Contains(a.EntityId) && a.EntityType == "SurveyRequest" ||
            datasetIds.Contains(a.EntityId) && a.EntityType == "SurveyDataVersion" || (assessmentIds.Contains(a.EntityId) || batchIds.Contains(a.EntityId)) && a.EntityType == "SurveyAssessment");
        var timeline = await MapTimeline(audits, filters, token);
        var sources = datasets.Select(d => new ReportingSourceRefFact("SurveyDataVersion", d.Id, Convert.ToBase64String(d.RowVersion)))
            .Concat(currentSets.Select(s => new ReportingSourceRefFact("RoadSegmentSet", s.Id, Convert.ToBase64String(s.RowVersion))))
            .Concat(assessmentRows.Select(a => new ReportingSourceRefFact("DatasetAssessment", a.Id, Convert.ToBase64String(a.RowVersion)))).ToArray();
        return new("success", new(tasks, selectedSegments, baselines, files, validationItems, timeline, sources, warnings.ToArray(),
            currentSets.Select(s => new ReportingPublishedSetFact(s.RoadSectionVersionId, s.Id)).ToArray(),
            await ReadIsolationAsync(token) == IsolationLevel.Snapshot ? "SNAPSHOT" : "SERIALIZABLE", intake));
    }

    public async Task<bool> AggregateBelongsAsync(Guid project, string type, Guid id, CancellationToken token) => type switch
    {
        "Project" => id == project && await db.Projects.AnyAsync(x => x.Id == project, token),
        "SurveyRequest" => await db.SurveyRequests.AnyAsync(x => x.Id == id && x.ProjectId == project, token),
        "SurveyDataVersion" => await (from d in db.SurveyDataVersions join s in db.Surveys on d.SurveyId equals s.Id where d.Id == id && s.ProjectId == project select d.Id).AnyAsync(token),
        "DatasetAssessment" => await (from a in db.Set<DatasetAssessment>() join d in db.SurveyDataVersions on a.DatasetId equals d.Id join s in db.Surveys on d.SurveyId equals s.Id where a.Id == id && s.ProjectId == project select a.Id).AnyAsync(token),
        "BaselineSelection" => await db.Set<BaselineSelection>().AnyAsync(x => x.Id == id && x.ProjectId == project, token),
        _ => false
    };
    public async Task<ReportingTimelineItemFact[]> TimelineAsync(string type, Guid id, ReportingFiltersFact filters, DateTimeOffset? afterTime, Guid? afterId, int take, CancellationToken token)
    {
        var storedType = type is "DatasetAssessment" or "BaselineSelection" ? "SurveyAssessment" : type;
        var query = Allowed(db.AuditLogs.AsNoTracking().Where(a => a.EntityType == storedType && a.EntityId == id));
        if (filters.From is { } from) query = query.Where(a => a.OccurredAtUtc >= from);
        if (filters.To is { } to) query = query.Where(a => a.OccurredAtUtc < to);
        if (afterTime is { } time && afterId is { } eventId) query = query.Where(a => a.OccurredAtUtc > time || a.OccurredAtUtc == time && a.Id.CompareTo(eventId) > 0);
        var rows = await query.OrderBy(a => a.OccurredAtUtc).ThenBy(a => a.Id).Take(take).ToArrayAsync(token);
        return rows.Select(ToTimeline).ToArray();
    }
    private static IQueryable<RoadGuardSystem.BusinessObjects.Auditing.AuditLog> Allowed(IQueryable<RoadGuardSystem.BusinessObjects.Auditing.AuditLog> q) =>
        q.Where(a => a.EntityType == "SurveyRequest" && (a.EventType == "survey_task_created" || a.EventType == "survey_task_accept" || a.EventType == "survey_task_cancel" ||
            a.EventType == "survey_task_decline" || a.EventType == "survey_task_reassign" || a.EventType == "survey_task_supplement") ||
            a.EntityType == "SurveyDataVersion" && a.EventType == "survey_dataset_submitted" ||
            a.EntityType == "SurveyAssessment" && (a.EventType == "dataset_assessment_created" || a.EventType == "baseline_selected"));
    private static async Task<ReportingTimelineItemFact[]> MapTimeline(IQueryable<RoadGuardSystem.BusinessObjects.Auditing.AuditLog> query, ReportingFiltersFact filters, CancellationToken token)
    {
        query = Allowed(query);
        if (filters.From is { } from) query = query.Where(a => a.OccurredAtUtc >= from);
        if (filters.To is { } to) query = query.Where(a => a.OccurredAtUtc < to);
        return (await query.OrderBy(a => a.OccurredAtUtc).ThenBy(a => a.Id).ToArrayAsync(token)).Select(ToTimeline).ToArray();
    }
    private static ReportingTimelineItemFact ToTimeline(RoadGuardSystem.BusinessObjects.Auditing.AuditLog a) =>
        new(a.Id, a.OccurredAtUtc, null, a.EventType, new(a.EventType == "baseline_selected" ? "BaselineSelection" : a.EventType == "dataset_assessment_created" ? "DatasetAssessment" : a.EntityType, a.EntityId, a.Id.ToString("N")),
            a.EventType switch { "dataset_assessment_created" => "Manual dataset assessment recorded.", "baseline_selected" => "Baseline selection recorded.", "survey_dataset_submitted" => "Dataset submitted.", _ => "Survey task workflow recorded." });
    private static bool MatchesScope(Guid route, Guid set, string idsJson, ReportingFiltersFact f, HashSet<string> warnings)
    {
        try
        {
            return (!f.RouteVersionId.HasValue || route == f.RouteVersionId) && (!f.SegmentSetId.HasValue || set == f.SegmentSetId) &&
            (f.SegmentIds!.Length == 0 || (JsonSerializer.Deserialize<Guid[]>(idsJson) ?? []).Intersect(f.SegmentIds).Any());
        }
        catch (JsonException) { warnings.Add("SCOPE_MANIFEST_UNREADABLE"); return false; }
    }
    private static bool MatchesManifest(string json, ReportingFiltersFact f, HashSet<string> warnings)
    {
        try
        {
            var scopes = JsonSerializer.Deserialize<Scope[]>(json, Json) ?? [];
            if (scopes.Length == 0 || scopes.Any(s => s is null || s.RouteVersionId == Guid.Empty || s.SegmentSetId == Guid.Empty || s.SegmentIds is not { Length: > 0 } ||
                s.SegmentIds.Contains(Guid.Empty) || s.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")))
            { warnings.Add("SCOPE_MANIFEST_UNREADABLE"); return false; }
            return scopes.Any(s => MatchesScope(s.RouteVersionId, s.SegmentSetId, JsonSerializer.Serialize(s.SegmentIds), f, warnings));
        }
        catch (JsonException) { warnings.Add("SCOPE_MANIFEST_UNREADABLE"); return false; }
    }
    private sealed record Scope(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand);
    private sealed record SourceFile(Guid FileId, string ChecksumSha256, long SizeBytes, string MediaType, string Purpose);
    private static string TaskStatus(SurveyRequestStatus status) => status switch
    { SurveyRequestStatus.NewAssigned => "NEW_ASSIGNED", SurveyRequestStatus.InProgress => "IN_PROGRESS", SurveyRequestStatus.SupplementRequired => "SUPPLEMENT_REQUIRED", _ => status.ToString().ToUpperInvariant() };
    private async Task<IsolationLevel> ReadIsolationAsync(CancellationToken token)
    {
        var transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        if (transaction is null) return IsolationLevel.Unspecified;
        if (transaction.IsolationLevel is IsolationLevel.Serializable or IsolationLevel.Snapshot) return transaction.IsolationLevel;
        // Admission may have raised the SQL session isolation after beginning an existing transaction.
        // SqlTransaction's cached property does not establish the server's effective setting in that case.
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id = @@SPID";
        return Convert.ToInt32(await command.ExecuteScalarAsync(token), System.Globalization.CultureInfo.InvariantCulture) switch
        { 4 => IsolationLevel.Serializable, 5 => IsolationLevel.Snapshot, _ => IsolationLevel.Unspecified };
    }
}
