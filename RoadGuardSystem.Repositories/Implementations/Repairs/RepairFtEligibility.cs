using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

internal static class RepairFtEligibility
{
    internal static async Task<string[]> Missing(RoadGuardDbContext db, TimeProvider clock, RepairItem item,
        RepairFieldTaskBinding binding, RepairMeasurementAssessment? assessment, RepairPolicyRevision? policy,
        RepairExecutionAuthorization? authorization, RoadCoverageResolution coverage, CancellationToken token)
    {
        var missing = new List<string>();
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(binding.CrewId, UserRoleCode.RepairCrew, token) ||
            !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == item.ProjectId && row.UserId == binding.CrewId &&
                row.RoleCode == UserRoleCode.RepairCrew && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day &&
                (row.ValidTo == null || row.ValidTo >= day), token) ||
            !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(row => row.Id == binding.AssignmentId &&
                row.FieldInspectionTaskId == binding.TaskId && row.AssignedToUserId == binding.CrewId &&
                row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, token) ||
            !await db.FieldInspectionTasks.AsNoTracking().AnyAsync(row => row.Id == binding.TaskId && row.ProjectId == item.ProjectId &&
                row.RepairItemId == item.Id && row.TaskMode == "CONDITIONAL_FT" && row.Status == FieldInspectionTaskStatus.InProgress, token))
            missing.Add("CURRENT_EXECUTION_AUTHORITY_DENIED");
        if (!coverage.PermitsExecution)
        {
            missing.Add(coverage.State == "TEST_ONLY_MAPPING" ? "TEST_ONLY_SOURCE_NOT_EXECUTABLE" : "ROAD_HANDOVER_MAPPING_UNKNOWN");
            missing.Add(coverage.State == "UNKNOWN_OWNER_MAPPING" ? "COVERAGE_MAPPING_UNKNOWN" : coverage.State);
        }
        if (item.Mode != RepairMode.FastTrack) missing.Add("NORMAL_FLOW_REQUIRES_SUPERVISOR_APPROVAL");
        if (policy is null || policy.IsRevoked || policy.Measurements.Count == 0 || policy.Id != binding.PolicyRevisionId ||
            policy.ProjectId != item.ProjectId || policy.ChecklistVersion != binding.ChecklistVersion)
            missing.Add("POLICY_UNAVAILABLE");
        if (policy is not null && binding.PolicyContentHash != RoadCoverageResolver.Hash(new
        {
            policy.Id,
            policy.ProjectId,
            policy.Revision,
            policy.DefectTypeCode,
            policy.ChecklistVersion,
            policy.PublishedAt,
            policy.PublishedBy,
            measurements = policy.Measurements.OrderBy(row => row.Code, StringComparer.Ordinal).ToArray(),
            stopConditions = policy.StopConditions.Order(StringComparer.Ordinal).ToArray()
        })) missing.Add("PINNED_POLICY_SOURCE_MISMATCH");
        if (assessment is null) missing.Add("ASSESSMENT_REQUIRED");
        else
        {
            missing.AddRange(JsonSerializer.Deserialize<string[]>(assessment.MissingReasonsJson, RoadCoverageResolver.Json) ?? []);
            if (assessment.ItemId != item.Id || assessment.BindingId != binding.Id || assessment.TaskId != binding.TaskId ||
                assessment.AssignmentId != binding.AssignmentId || assessment.OriginalActorId != binding.CrewId ||
                assessment.ProjectId != item.ProjectId || assessment.Stage != "PRE_EXECUTION" || assessment.LocationState != "VERIFIED_CHECKLIST")
                missing.Add("ASSESSMENT_SOURCE_OR_LOCATION_UNVERIFIED");
            var ids = assessment.Measurements.Select(row => row.MeasurementId).ToArray();
            var values = await db.GroundTruthMeasurements.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArrayAsync(token);
            if (values.Length != ids.Length || values.Any(row => row.FieldInspectionSessionId != assessment.SessionId ||
                row.DefectId != item.DefectId || row.RoadSectionVersionId != binding.RouteVersionId)) missing.Add("MEASUREMENT_SOURCE_MISMATCH");
            if (policy is not null)
            {
                var facts = new List<RepairMeasurementFact>();
                foreach (var rule in policy.Measurements)
                {
                    var matched = values.Where(row => string.Equals(row.MeasurementType.ToString(), rule.Code, StringComparison.Ordinal)).ToArray();
                    if (matched.Length != 1) { missing.Add("POLICY_MEASUREMENT_SOURCE_NOT_UNIQUE"); continue; }
                    var fact = matched[0];
                    facts.Add(new(rule.Code, fact.ValueState == "KNOWN" ? fact.Value : null, fact.Unit));
                }
                using var payload = JsonDocument.Parse(assessment.PayloadJson);
                var observations = payload.RootElement.TryGetProperty("stopConditions", out var observed)
                    ? JsonSerializer.Deserialize<Dictionary<string, bool?>>(observed, RoadCoverageResolver.Json) : null;
                if (policy.StopConditions.Any(code => observations is null || !observations.TryGetValue(code, out var present) || present is null))
                    missing.Add("STOP_CONDITION_OBSERVATION_UNKNOWN");
                var result = policy.Evaluate(facts, observations?.Where(row => row.Value == true).Select(row => row.Key).ToArray() ?? []);
                if (!result.Eligible) missing.Add("POLICY_" + result.Reason.ToString().ToUpperInvariant());
            }
        }
        if (authorization?.VerifiedStartedAt is null) missing.Add("FIRST_START_NOT_VERIFIED");
        else if (clock.GetUtcNow() >= authorization.ExpiresAt) missing.Add("EXECUTION_WINDOW_EXPIRED");
        if (authorization is not null && assessment is not null)
        {
            var evaluated = authorization.Evaluate(clock.GetUtcNow(), new(item.ProjectId, item.DefectId, binding.TaskId,
                binding.AssignmentId, binding.CrewId, binding.LocationVersion, binding.PolicyRevisionId ?? Guid.Empty,
                true, coverage.PermitsExecution, coverage.PermitsExecution ? RepairFactState.Confirmed : RepairFactState.Unknown,
                policy is not null && !policy.IsRevoked, missing.Count == 0, assessment.LocationState == "VERIFIED_CHECKLIST",
                assessment.Readiness != "READY", false));
            if (!evaluated.Eligible) missing.Add("EXECUTION_" + evaluated.Reason.ToString().ToUpperInvariant());
        }
        return missing.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }
}
