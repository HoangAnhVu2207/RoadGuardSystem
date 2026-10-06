using System.Data.SqlTypes;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Reporting;

public sealed class ReportingService(IReportingRepository repository, IIdentityRepository identity, IProjectScopeGuard guard, TimeProvider clock,
    IEnumerable<RoadGuardSystem.Services.Integration.ICaseDefectReadReader>? caseDefectReaders = null,
    IEnumerable<ICurrentRepairFactsReader>? currentRepairReaders = null) : IReportingService
{
    public async Task<ReportingResult<ReportingCaptureDto>> CaptureAsync(Guid actor, Guid project, ReportingFiltersDto filters, CancellationToken token)
    {
        if (!TryNormalize(filters, out var normalized)) return new("validation_error");
        return await repository.ReadConsistentlyAsync(async ct =>
        {
            if (!await Authorized(actor, project, ct)) return new ReportingResult<ReportingCaptureDto>("access_forbidden");
            var readAt = clock.GetUtcNow();
            var facts = await repository.CaptureAsync(project, normalized, ct);
            if (facts.Facts is null) return new(facts.Code);
            var capture = ReportingDefinitions.Create(project, readAt, normalized, facts.Facts);
            var reader = caseDefectReaders?.SingleOrDefault();
            if (reader is not null)
            {
                var actorState = await identity.GetUserSecurityStateAsync(actor, ct);
                if (actorState is null) return new("access_forbidden");
                try
                {
                    var snapshot = await reader.CaptureAsync(actor, actorState.RoleCode, project, normalized, ct);
                    if (snapshot is not null)
                    {
                        try { capture = CaseDefectCaptureConsumer.Apply(capture, snapshot); }
                        catch (InvalidOperationException) { return new("producer_invalid"); }
                    }
                }
                catch (UnauthorizedAccessException) { return new("access_forbidden"); }
            }
            var repairReader=currentRepairReaders?.SingleOrDefault();
            if(repairReader is not null)
            {
                try { capture=CurrentRepairCaptureConsumer.Apply(capture,await repairReader.CaptureAsync(actor,project,normalized,ct)); }
                catch(UnauthorizedAccessException)
                {
                    // Preserve legacy report access without exposing new protected repair facts.
                    if(!await Authorized(actor,project,ct)) return new("access_forbidden");
                    capture=CurrentRepairCaptureConsumer.Apply(capture,new(project,[],["REPAIR_PROJECT_AUTHORITY_NOT_VERIFIED"]));
                }
                catch(InvalidOperationException) { return new("producer_invalid"); }
            }
            return new("success", capture);
        }, token);
    }
    public async Task<ReportingResult<ProjectSummaryV1>> SummaryAsync(Guid actor, Guid project, ReportingFiltersDto filters, CancellationToken token)
    {
        var capture = await CaptureAsync(actor, project, filters, token); return new(capture.Code, capture.Value?.Summary);
    }
    public async Task<ReportingResult<ReportingItemsPageDto>> ItemsAsync(Guid actor, Guid project, ReportingFiltersDto filters, string metric, string? cursor, int pageSize, CancellationToken token)
    {
        if (!ReportingDefinitions.Codes.Contains(metric) || pageSize is < 1 or > 200 || !TryNormalize(filters, out var normalized)) return new("validation_error");
        var hash = ReportingCursor.FilterHash(project, normalized, metric);
        if (!ReportingCursor.TryDecode(cursor, hash, out var key)) return new("cursor_invalid");
        var capture = await CaptureAsync(actor, project, normalized, token);
        if (capture.Value is not { } value) return new(capture.Code);
        var rows = value.Items.Where(i => i.Metric == metric && (key is null || new SqlGuid(i.Id).CompareTo(new SqlGuid(key.Id)) > 0))
            .OrderBy(i => new SqlGuid(i.Id)).Take(pageSize + 1).ToArray();
        var page = rows.Take(pageSize).ToArray();
        var metricRows = value.Summary.Metrics.Where(m => m.Code == metric).ToArray();
        var availability = metricRows.Any(m => m.Availability == "UNAVAILABLE") ? "UNAVAILABLE" : metricRows.Any(m => m.Availability == "PARTIAL") ? "PARTIAL" : "AVAILABLE";
        return new("success", new(ReportingDefinitions.Schema, metric, value.Summary.ReadAt, page,
            rows.Length > pageSize ? ReportingCursor.Encode(hash, DateTimeOffset.UnixEpoch, page[^1].Id) : null,
            new(availability, metricRows.SelectMany(m => m.ReasonCodes).Distinct().ToArray())));
    }
    public async Task<ReportingResult<ReportingTimelinePageDto>> TimelineAsync(Guid actor, Guid project, string aggregateType, Guid aggregateId, ReportingFiltersDto filters, string? cursor, int pageSize, CancellationToken token)
    {
        if (aggregateType is not ("Project" or "SurveyRequest" or "SurveyDataVersion" or "DatasetAssessment" or "BaselineSelection") || aggregateId == Guid.Empty ||
            pageSize is < 1 or > 200 || !TryNormalize(filters, out var normalized) || normalized.RouteVersionId.HasValue || normalized.SegmentSetId.HasValue || normalized.SegmentIds!.Length > 0)
            return new("validation_error");
        var hash = ReportingCursor.FilterHash(project, normalized, "timeline", aggregateType, aggregateId);
        if (!ReportingCursor.TryDecode(cursor, hash, out var key)) return new("cursor_invalid");
        return await repository.ReadConsistentlyAsync(async ct =>
        {
            if (!await Authorized(actor, project, ct)) return new ReportingResult<ReportingTimelinePageDto>("access_forbidden");
            if (!await repository.AggregateBelongsAsync(project, aggregateType, aggregateId, ct)) return new ReportingResult<ReportingTimelinePageDto>("not_found");
            var readAt = clock.GetUtcNow();
            ReportingTimelineItemDto[] rows;
            if (aggregateType == "Project")
            {
                var facts = await repository.CaptureAsync(project, normalized, ct);
                rows = (facts.Facts?.Timeline ?? []).Where(i => key is null || i.OccurredAt > key.Time || i.OccurredAt == key.Time && new SqlGuid(i.EventId).CompareTo(new SqlGuid(key.Id)) > 0)
                    .OrderBy(i => i.OccurredAt).ThenBy(i => new SqlGuid(i.EventId)).Take(pageSize + 1).ToArray();
            }
            else rows = await repository.TimelineAsync(aggregateType, aggregateId, normalized, key?.Time, key?.Id, pageSize + 1, ct);
            var page = rows.Take(pageSize).ToArray();
            return new ReportingResult<ReportingTimelinePageDto>("success", new(ReportingDefinitions.Schema, readAt, page,
                rows.Length > pageSize ? ReportingCursor.Encode(hash, page[^1].OccurredAt, page[^1].EventId) : null, ReportingDefinitions.TimelineAvailability));
        }, token);
    }
    private async Task<bool> Authorized(Guid actor, Guid project, CancellationToken token)
    {
        if (actor == Guid.Empty || project == Guid.Empty) return false;
        var user = await identity.GetUserSecurityStateAsync(actor, token);
        return user is { Status: UserStatus.Active, MustChangePassword: false, RoleCode: UserRoleCode.ProjectManager or UserRoleCode.Supervisor } &&
            await identity.IsRoleActiveAsync(user.RoleCode, token) && await guard.AuthorizeAsync(actor, user.RoleCode, project, token) is not null;
    }
    public static bool TryNormalize(ReportingFiltersDto filters, out ReportingFiltersDto normalized)
    {
        normalized = filters with { From = filters.From?.ToUniversalTime(), To = filters.To?.ToUniversalTime(), SegmentIds = (filters.SegmentIds ?? []).Distinct().Order().ToArray() };
        return filters.From.HasValue == filters.To.HasValue && (!filters.From.HasValue || filters.From.Value.Offset == TimeSpan.Zero && filters.To!.Value.Offset == TimeSpan.Zero && filters.From < filters.To) &&
            filters.RouteVersionId != Guid.Empty && filters.SegmentSetId != Guid.Empty && normalized.SegmentIds.Length <= 500 && !normalized.SegmentIds.Contains(Guid.Empty);
    }
}
