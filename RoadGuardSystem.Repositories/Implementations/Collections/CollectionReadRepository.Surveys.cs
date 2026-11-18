using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Collections;

namespace RoadGuardSystem.Repositories.Implementations.Collections;

public sealed partial class CollectionReadRepository
{
    public async Task<SurveyCollectionSummary[]> SurveySummariesAsync(CollectionResource resource, Guid project,
        Guid[] ids, CancellationToken token)
    {
        if (resource == CollectionResource.SurveyPlans)
        {
            var plans = await db.SurveyPlans.AsNoTracking().Where(row => row.ProjectId == project && ids.Contains(row.Id)).ToArrayAsync(token);
            var scopes = await db.SurveyPlanScopes.AsNoTracking().Where(row => ids.Contains(row.SurveyPlanId)).OrderBy(row => row.Id).ToArrayAsync(token);
            return plans.Select(plan =>
            {
                var scope = scopes.Where(row => row.SurveyPlanId == plan.Id).Select(row => new SurveyScopeSummary(
                    row.RouteSectionVersionId, row.SegmentSetId, row.TargetBand, CountSegments(row.SegmentIdsJson))).ToArray();
                if (scope.Length == 0) scope = LegacyScopes(plan.OutputRequirements);
                return new SurveyCollectionSummary(plan.Id, project, Status(plan.Status.ToString()), Convert.ToBase64String(plan.RowVersion),
                    plan.PlannedStartAt, plan.PlannedEndAt, Guid.Empty, null, "NOT_APPLICABLE", null, null, 0, null, scope,
                    scope.Length == 0 ? "MISSING" : "STORED");
            }).ToArray();
        }
        var tasks = await db.SurveyRequests.AsNoTracking().Where(row => row.ProjectId == project && ids.Contains(row.Id)).ToArrayAsync(token);
        var assignments = await db.SurveyAssignments.AsNoTracking().Where(row => ids.Contains(row.SurveyRequestId))
            .OrderByDescending(row => row.EndedAt == null).ThenByDescending(row => row.AssignedAt).ThenByDescending(row => row.Id).ToArrayAsync(token);
        var actorIds = assignments.Select(row => row.OperatorUserId).Distinct().ToArray();
        var names = await db.Users.AsNoTracking().Where(row => actorIds.Contains(row.Id)).Select(row => new { row.Id, row.DisplayName }).ToDictionaryAsync(row => row.Id, token);
        var taskScopes = await db.SurveyRequestScopes.AsNoTracking().Where(row => ids.Contains(row.SurveyRequestId)).OrderBy(row => row.Id).ToArrayAsync(token);
        var counts = await (from data in db.SurveyDataVersions.AsNoTracking()
                            join survey in db.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
                            where survey.ProjectId == project && survey.SurveyRequestId.HasValue && ids.Contains(survey.SurveyRequestId.Value)
                            group data by survey.SurveyRequestId!.Value into grouped
                            select new { Id = grouped.Key, Count = grouped.Count() }).ToDictionaryAsync(row => row.Id, token);
        var children = await (from child in db.SurveyRequests.AsNoTracking()
                              join request in db.SupplementarySurveyRequests.AsNoTracking() on child.SupplementRequestId equals (Guid?)request.Id
                              where child.ProjectId == project && child.ParentTaskId.HasValue && ids.Contains(child.ParentTaskId.Value) && request.SurveyRequestId == child.ParentTaskId
                              select new { Parent = child.ParentTaskId!.Value, child.Id }).ToArrayAsync(token);
        var result = new List<SurveyCollectionSummary>();
        foreach (var task in tasks)
        {
            var assignment = assignments.FirstOrDefault(row => row.SurveyRequestId == task.Id);
            if (assignment is null) continue; // Same visibility as detail's assignment join.
            var scope = taskScopes.Where(row => row.SurveyRequestId == task.Id).Select(row => new SurveyScopeSummary(
                row.RouteSectionVersionId, row.SegmentSetId, row.TargetBand, CountSegments(row.SegmentIdsJson))).ToArray();
            if (scope.Length == 0) scope = LegacyScopes(task.OutputRequirements);
            var direct = children.Where(row => row.Parent == task.Id).ToArray();
            result.Add(new(task.Id, project, Status(task.Status.ToString()), Convert.ToBase64String(task.RowVersion),
                null, null, assignment.OperatorUserId, names.GetValueOrDefault(assignment.OperatorUserId)?.DisplayName,
                names.ContainsKey(assignment.OperatorUserId) ? "CURRENT_ACCOUNT" : "MISSING_ACCOUNT", task.SurveyPlanId, task.DueAt,
                counts.GetValueOrDefault(task.Id)?.Count ?? 0, direct.Length == 1 ? direct[0].Id : null, scope, scope.Length == 0 ? "MISSING" : "STORED"));
        }
        return result.ToArray();
    }
    private static int CountSegments(string json)
    { try { return JsonSerializer.Deserialize<Guid[]>(json)?.Distinct().Count() ?? 0; } catch (JsonException) { return 0; } }
    private static SurveyScopeSummary[] LegacyScopes(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var properties = document.RootElement.EnumerateObject().ToDictionary(row => row.Name, row => row.Value, StringComparer.OrdinalIgnoreCase);
            if (!properties.TryGetValue("scope", out var scope) || scope.ValueKind != JsonValueKind.Array) return [];
            return scope.EnumerateArray().Select(row =>
            {
                var values = row.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);
                return new SurveyScopeSummary(values["routeVersionId"].GetGuid(), values["segmentSetId"].GetGuid(),
                    values["targetBand"].GetString()!, CountSegments(values["segmentIds"].GetRawText()));
            }).ToArray();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException) { return []; }
    }
    private static string Status(string value) => value switch
    { "NewAssigned" => "NEW_ASSIGNED", "InProgress" => "IN_PROGRESS", "SupplementRequired" => "SUPPLEMENT_REQUIRED", "TaskCreated" => "TASK_CREATED", _ => value.ToUpperInvariant() };
}
