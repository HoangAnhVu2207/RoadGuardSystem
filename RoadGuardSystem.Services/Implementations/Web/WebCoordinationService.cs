using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Web;
using Facts = RoadGuardSystem.BusinessObjects.PersistenceFacts.Web;
using RoadGuardSystem.Repositories.Web;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Web;

public sealed class WebReadException(int status, string code) : Exception(code)
{ public int Status { get; } = status; public string Code { get; } = code; }

public sealed class WebCoordinationService(IWebCoordinationRepository repository, IRepairWorkflowService repairs,
    RoadGuardSystem.Services.Cases.ICaseReportDetailsService reports, RoadGuardSystem.Services.Integration.IAiCandidateFactsReader ai,
    RoadGuardSystem.Services.Files.IUploadService files)
{
    public async Task<OrganizationProfile?> OrganizationAsync(Guid actor, UserRoleCode role, Guid user, CancellationToken token)
    {
        if (role != UserRoleCode.Supervisor || !await repository.AuthorizeAsync(actor, role, [], token))
            throw new WebReadException(403, "access_forbidden");
        var row = await repository.OrganizationAsync(user, token); return row is null ? null : (OrganizationProfile)row;
    }
    public async Task GuardAsync(Guid actor, UserRoleCode role, Guid[] projects, CancellationToken token)
    {
        if (projects.Length is < 1 or > 100 || projects.Any(id => id == Guid.Empty)) throw new WebReadException(400, "validation_error");
        if (!await repository.AuthorizeAsync(actor, role, projects, token)) throw new WebReadException(403, "access_forbidden");
    }
    public async Task<TaskAssignee[]> TaskAssigneesAsync(Guid actor, UserRoleCode role, Guid project, Guid[] tasks, CancellationToken token)
    {
        await GuardAsync(actor, role, [project], token);
        if (tasks.Length is < 1 or > 100 || tasks.Any(id => id == Guid.Empty)) throw new WebReadException(400, "validation_error");
        return (await repository.TaskAssigneesAsync(project, tasks.Distinct().ToArray(), token)).Select(row => (TaskAssignee)row).ToArray();
    }
    public async Task<ProjectOverview[]> OverviewsAsync(Guid actor, UserRoleCode role, Guid[] projects, CancellationToken token)
    {
        projects = projects.Distinct().ToArray();
        await GuardAsync(actor, role, projects, token);
        return (await repository.OverviewsAsync(projects, token)).Select(row => (ProjectOverview)row).ToArray();
    }
    public async Task<ProjectContact[]> ContactsAsync(Guid actor, UserRoleCode role, Guid project, Guid? after, int take, CancellationToken token)
    {
        await GuardAsync(actor, role, [project], token);
        return (await repository.ContactsAsync(project, after, take, token)).Select(row => (ProjectContact)row).ToArray();
    }
    public async Task<PolicyDiscovery[]> PoliciesAsync(Guid actor, UserRoleCode role, Guid project, string kind,
        Guid? after, int take, string? defectType, bool applicableOnly, CancellationToken token)
    {
        await GuardAsync(actor, role, [project], token);
        // Existing policy read/write authority is PM, not a project-global Supervisor grant.
        if (role != UserRoleCode.ProjectManager) throw new WebReadException(403, "access_forbidden");
        if (kind is not ("DRAFT" or "REVISION") || defectType?.Length > 80) throw new WebReadException(400, "validation_error");
        return (await repository.PoliciesAsync(project, kind, after, take, defectType, applicableOnly, token)).Select(row => (PolicyDiscovery)row).ToArray();
    }
    public async Task<PackagePlanning?> PackageAsync(Guid actor, UserRoleCode role, Guid project, Guid package, CancellationToken token)
    {
        await GuardAsync(actor, role, [project], token);
        var row = await repository.PackageAsync(project, package, token); return row is null ? null : (PackagePlanning)row;
    }
    public async Task<ItemPlanning?> ItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item, CancellationToken token)
    {
        await ItemGuard(actor, role, project, package, item, token);
        var row = await repository.ItemAsync(project, package, item, token); return row is null ? null : (ItemPlanning)row;
    }
    public async Task<DefectSourceReview?> DefectAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item, CancellationToken token)
    {
        await ItemGuard(actor, role, project, package, item, token);
        var stored = await repository.DefectAsync(project, package, item, token);
        var result = stored is null ? null : (DefectSourceReview)stored;
        if (result is null) return null;
        var evidence = new List<DefectSourceEvidence>();
        var missing = new List<string>(result.MissingSections);
        foreach (var source in result.Sources.Take(100))
        {
            if (source.Kind == "Report" && source.CaseId is Guid caseId)
            {
                string? cursor = null;
                var found = false;
                do
                {
                    var page = await reports.ReadAsync(actor, role, caseId, 100, cursor, token);
                    if (page.Page is null) { missing.Add("REPORT_SOURCE_UNAVAILABLE"); break; }
                    var report = page.Page.Items.SingleOrDefault(row => row.ReportId == source.Id);
                    if (report is not null)
                    {
                        found = true;
                        evidence.AddRange(report.OriginalEvidence.Concat(report.Supplements.SelectMany(row => row.Evidence))
                            .Select(row => new DefectSourceEvidence(row.EvidenceId, row.ContentPath, "REPORT", row.CapturedAt)));
                        break;
                    }
                    cursor = page.Page.NextCursor;
                } while (cursor is not null);
                if (!found) missing.Add("REPORT_SOURCE_UNAVAILABLE");
            }
            else if (source.Kind == "AiDetection")
            {
                var resolved = await ai.ResolveAsync(actor, role, project, source.Id, cancellationToken: token);
                if (resolved.Facts is not { } facts) { missing.Add("AI_SOURCE_UNAVAILABLE"); continue; }
                var file = await files.GetFileMetadataAsync(actor, role, facts.FrameFileId, cancellationToken: token);
                if (file.File is not { Status: "VERIFIED" } || file.File.Version != facts.FrameFileVersion)
                { missing.Add("AI_FRAME_UNAVAILABLE"); continue; }
                evidence.Add(new(facts.FrameFileId, $"/api/v1/files/{facts.FrameFileId:D}/content", "AI_FRAME", null));
            }
            else missing.Add("SOURCE_EVIDENCE_UNAVAILABLE");
        }
        if (result.Sources.Length == 0) missing.Add("LEGACY_SOURCE_NOT_STORED");
        if (result.Sources.Length > 100) missing.Add("SOURCE_READ_LIMIT");
        return result with { Evidence = evidence.Distinct().ToArray(), MissingSections = missing.Distinct().ToArray() };
    }
    private async Task ItemGuard(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item, CancellationToken token)
    {
        var result = await repairs.ReadItemAsync(actor, role, project, package, item, false, token);
        if (result.Status != 200) throw new WebReadException(result.Status, result.Code ?? "not_found");
    }
    public async Task<WebMetadataResult> WriteAsync(Guid actor, UserRoleCode role, WebMetadataRequest command, CancellationToken token)
    {
        var input = command with
        {
            Text = Normalize(command.Text),
            Description = Normalize(command.Description),
            Unit = Normalize(command.Unit),
            Materials = Normalize(command.Materials),
            Reason = command.Reason?.Trim() ?? ""
        };
        if (input.Reason.Length is < 1 or > 1000 || input.Key.Length is < 1 or > 150 || input.Key.Any(c => c is < '!' or > '~') ||
            input.Text?.Length > (input.Action == "locality" ? 500 : 255) || input.Description?.Length > 4000 ||
            input.Materials?.Length > 4000 || input.Unit?.Length > 32 || input.Quantity is <= 0 or > 999999999999m ||
            input.Quantity.HasValue != (input.Unit is not null) || input.Quantity.HasValue && decimal.Round(input.Quantity.Value, 6) != input.Quantity.Value)
            return new WebMetadataResult(400, "validation_error");
        return (WebMetadataResult)await repository.WriteAsync(actor, role, new Facts.WebMetadataCommand(input.Action, input.TargetId, input.ProjectId, input.PackageId, input.Text, input.Description, input.DueAt, input.Quantity, input.Unit, input.Materials, input.Reason, input.Key, input.ExpectedVersion), token);
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
