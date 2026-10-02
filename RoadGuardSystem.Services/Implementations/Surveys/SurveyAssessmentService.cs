using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public sealed class SurveyAssessmentService(ISurveyAssessmentRepository repository, ISurveyV2Repository surveys, IProjectScopeGuard guard) : ISurveyAssessmentService
{
    public async Task<AssessmentOutcome> CreateAsync(Guid actor, UserRoleCode role, Guid dataset, CreateAssessmentDto request, string key, string version, Guid? correlation, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager || actor == Guid.Empty) return new("access_forbidden");
        var access = await surveys.GetDatasetAccessAsync(dataset, token);
        if (access is null) return new("not_found");
        if (await guard.AuthorizeAsync(actor, role, access.ProjectId, token) is null) return new("access_forbidden");
        if (request.MethodVersion != "pm-evidence-review.v1") return new("assessment_method_not_approved");
        if (!ValidItems(request.Items)) return new("assessment_validation_failed");
        var items = request.Items.Select(i => new AssessmentItem(i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand,
            i.PositionStatus, i.QualityStatus, i.CoverageStatus, i.Reason, i.Evidence.Select(e => new AssessmentEvidence(e.FileId, e.FromMilliseconds, e.ToMilliseconds)).ToArray())).ToArray();
        return await repository.CreateAsync(actor, access.ProjectId, dataset, version.Trim('"'), items, key, Hash(new { dataset, request, version }), correlation, token);
    }
    public static bool ValidItems(IReadOnlyList<AssessmentItemDto>? items)
    {
        if (items is not { Count: > 0 } || items.Any(i => i is null)) return false;
        if (items.Select(i => (i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand)).Distinct().Count() != items.Count) return false;
        return items.All(i => i.RouteVersionId != Guid.Empty && i.SegmentSetId != Guid.Empty && i.SegmentId != Guid.Empty &&
            i.TargetBand is "SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE" && ValidStatus(i.PositionStatus) && ValidStatus(i.QualityStatus) && ValidStatus(i.CoverageStatus) &&
            !string.IsNullOrWhiteSpace(i.Reason) && i.Evidence is not null &&
            (!(i.PositionStatus == "PASS" || i.QualityStatus == "PASS" || i.CoverageStatus == "PASS") || i.Evidence.Count > 0) &&
            i.Evidence.All(e => e is not null && e.FileId != Guid.Empty &&
                (e.FromMilliseconds is null && e.ToMilliseconds is null || e.FromMilliseconds >= 0 && e.ToMilliseconds > e.FromMilliseconds)));
    }
    public async Task<AssessmentOutcome> ReadAsync(Guid actor, UserRoleCode role, Guid dataset, Guid assessment, CancellationToken token)
    {
        var access = await surveys.GetDatasetAccessAsync(dataset, token);
        if (access is null) return new("not_found");
        if (!await CanRead(actor, role, access.ProjectId, access.OperatorId, token)) return new("access_forbidden");
        var result = await repository.ReadAsync(dataset, assessment, token);
        return result is null ? new("not_found") : new("success", result);
    }
    public async Task<AssessmentOutcome> SelectAsync(Guid actor, UserRoleCode role, Guid project, CreateBaselineDto request, string key, Guid? correlation, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager || await guard.AuthorizeAsync(actor, role, project, token) is null) return new("access_forbidden");
        if (request.Items is not { Count: > 0 } || string.IsNullOrWhiteSpace(request.Reason) || request.Items.Any(i => i is null ||
            i.DatasetId == Guid.Empty || i.AssessmentId == Guid.Empty || i.SegmentId == Guid.Empty || i.RouteVersionId == Guid.Empty || i.SegmentSetId == Guid.Empty ||
            i.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")) ||
            request.Items.Select(i => (i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand)).Distinct().Count() != request.Items.Count) return new("baseline_not_eligible");
        return await repository.SelectAsync(actor, project, request.Items.Select(i => new BaselineItem(i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand,
            i.DatasetId, i.AssessmentId, i.ExpectedBaselineSelectionId)).ToArray(), request.Reason, key, Hash(new { project, request }), correlation, token);
    }
    public async Task<(string Code, object? Value)> BaselineReadAsync(Guid actor, UserRoleCode role, Guid project, Guid? id, Guid? segmentSetId, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || await guard.AuthorizeAsync(actor, role, project, token) is null) return ("access_forbidden", null);
        if (id is { } history) { var result = await repository.HistoryAsync(project, history, token); return result is null ? ("not_found", null) : ("success", result); }
        return ("success", new { items = await repository.CurrentAsync(project, segmentSetId, token) });
    }
    private async Task<bool> CanRead(Guid actor, UserRoleCode role, Guid project, Guid assigned, CancellationToken token) =>
        role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator &&
        (role != UserRoleCode.DroneOperator || actor == assigned) && await guard.AuthorizeAsync(actor, role, project, token) is not null;
    private static bool ValidStatus(string status) => status is "PASS" or "FAIL" or "UNKNOWN";
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
}
