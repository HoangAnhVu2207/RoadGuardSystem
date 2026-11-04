using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Authorization;

namespace RoadGuardSystem.Services.Implementations.Cases;

public sealed class CaseReportDetailsService(ICaseReportDetailsRepository repository, IProjectScopeGuard scope) : ICaseReportDetailsService
{
    public async Task<CaseReportDetailResult> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, int limit,
        string? cursor, CancellationToken token)
    {
        if (role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager)) return new(403, "access_forbidden");
        if (limit is < 1 or > 100) return new(400, "validation_error");
        DateTimeOffset? before = null;
        Guid? afterId = null;
        if (cursor is not null)
        {
            try
            {
                if (cursor.Length > 512) return new(400, "validation_error");
                var decoded = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(cursor));
                if (decoded is null || decoded.CaseId != caseId || decoded.Actor != actor || decoded.Limit != limit ||
                    decoded.ReceivedAt == default || decoded.ReportId == Guid.Empty)
                    return new(400, "validation_error");
                before = decoded.ReceivedAt;
                afterId = decoded.ReportId;
            }
            catch (Exception exception) when (exception is FormatException or JsonException)
            {
                return new(400, "validation_error");
            }
        }
        try
        {
            var rows = await repository.ReadAsync(actor, role, caseId, before, afterId, limit + 1,
                async (project, ct) => await scope.AuthorizeAsync(actor, role, project, ct) is not null, token);
            var items = rows.Items.Take(limit).Select(row => new CaseReportDetailDto(caseId, row.Id, row.ReporterUserId,
                row.ReporterDisplayName, row.ReporterPhoneNumber, row.ReporterPhoneNumberConfirmed,
                row.Description, row.ReceivedAt,
                row.OriginalEvidence.Select(e => Evidence(caseId, row.Id, e)).ToArray(),
                row.Supplements.Select(s => new CaseReportSupplementDto(s.Id, s.Description, s.ReceivedAt,
                    s.Evidence.Select(e => Evidence(caseId, row.Id, e)).ToArray())).ToArray())).ToArray();
            var last = items.LastOrDefault();
            var next = rows.HasMore && last is not null
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                    new Cursor(actor, caseId, limit, last.ReceivedAt, last.ReportId)))) : null;
            return new(200, Page: new(items, next));
        }
        catch (CaseWorkflowException exception) { return new(exception.Status, exception.Code); }
    }

    public async Task<CaseReportDetailResult> DownloadAsync(Guid actor, UserRoleCode role, Guid caseId,
        Guid reportId, Guid evidenceId, CancellationToken token)
    {
        if (role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager)) return new(403, "access_forbidden");
        try
        {
            return new(200, Content: await repository.DownloadAsync(actor, role, caseId, reportId, evidenceId,
            async (project, ct) => await scope.AuthorizeAsync(actor, role, project, ct) is not null, token));
        }
        catch (CaseWorkflowException exception) { return new(exception.Status, exception.Code); }
        catch (FileStorageException) { return new(503, "dependency_unavailable"); }
    }

    private static CaseReportEvidenceDto Evidence(Guid caseId, Guid reportId, CaseEvidenceRow e) =>
        new(e.Id, e.FileId, e.FileVersion, e.SupplementId, e.CapturedAt, e.LocationSource,
            e.Latitude is decimal latitude ? new ReportEvidenceLocationDto(latitude, e.Longitude, e.AccuracyMeters) : null,
            $"/api/v1/cases/{caseId:D}/reports/{reportId:D}/evidence/{e.Id:D}/content");

    private sealed record Cursor(Guid Actor, Guid CaseId, int Limit, DateTimeOffset ReceivedAt, Guid ReportId);
}
