using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed class RepairEligibilityRepository(RoadGuardDbContext db, TimeProvider clock) : IRepairEligibilityRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public Task<RepairEligibilityReadResult> ReadAsync(RepairEligibilityQuery query, CancellationToken token)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await Anh02ReceiptAuthority.LockAsync(db, query.ActorId, query.ProjectId, token);
            var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (query.Role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew) ||
                !await new AnhHuyFactsRepository(db).IsCurrentActorAsync(query.ActorId, query.Role, token) ||
                !await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == query.ProjectId && row.UserId == query.ActorId &&
                    row.RoleCode == query.Role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day &&
                    (row.ValidTo == null || row.ValidTo >= day), token)) return new RepairEligibilityReadResult(403, "access_forbidden");
            var item = await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == query.ItemId &&
                row.ProjectId == query.ProjectId && EF.Property<Guid?>(row, "PackageId") == query.PackageId, token);
            if (item is null) return new RepairEligibilityReadResult(404, "not_found");
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.CurrentBindingId &&
                row.ProjectId == query.ProjectId && row.ItemId == item.Id, token);
            if (binding is null) return new RepairEligibilityReadResult(409, "repair_binding_not_current");
            if (query.Role == UserRoleCode.RepairCrew && (binding.CrewId != query.ActorId ||
                !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(row => row.Id == binding.AssignmentId &&
                    row.FieldInspectionTaskId == binding.TaskId && row.AssignedToUserId == query.ActorId &&
                    row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, token)))
                return new RepairEligibilityReadResult(403, "access_forbidden");
            var roadId = await (from route in db.RoadSectionVersions.AsNoTracking()
                                join road in db.RoadSections.AsNoTracking()
                on route.RoadSectionId equals road.Id
                                where route.Id == binding.RouteVersionId && road.ProjectId == query.ProjectId
                                select (Guid?)road.Id).SingleOrDefaultAsync(token);
            if (roadId is null) return new RepairEligibilityReadResult(409, "repair_scope_source_unavailable");
            var sources = RepairEligibilitySourceSnapshot.Capture(query.ProjectId, roadId.Value,
                await db.Warranties.AsNoTracking().Where(row => row.ProjectId == query.ProjectId &&
                    (row.RoadSectionId == null || row.RoadSectionId == roadId)).OrderBy(row => row.Id).ToArrayAsync(token),
                await db.HandoverDocuments.AsNoTracking().Where(row => row.ProjectId == query.ProjectId).OrderBy(row => row.Id).ToArrayAsync(token));
            var policy = await db.Set<RepairPolicyRevision>().AsNoTracking().Include(row => row.Measurements).Include(row => row.Revocations)
                .SingleOrDefaultAsync(row => row.Id == binding.PolicyRevisionId && row.ProjectId == query.ProjectId, token);
            var assessment = await db.Set<RepairMeasurementAssessment>().AsNoTracking().Include(row => row.Measurements)
                .SingleOrDefaultAsync(row => row.Id == item.CurrentAssessmentId && row.BindingId == binding.Id && row.ProjectId == query.ProjectId, token);
            var authorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == binding.AuthorizationId && row.ProjectId == query.ProjectId && row.TaskId == binding.TaskId &&
                row.AssignmentId == binding.AssignmentId && row.CrewId == binding.CrewId, token);
            var missing = new List<string> { "ROAD_HANDOVER_MAPPING_UNKNOWN", "COVERAGE_MAPPING_UNKNOWN" };
            if (item.Mode != RepairMode.FastTrack) missing.Add("NORMAL_FLOW_REQUIRES_SUPERVISOR_APPROVAL");
            var policyState = policy is null ? "UNAVAILABLE" : policy.IsRevoked ? "REVOKED" : policy.Measurements.Count == 0 ? "NOT_CONFIGURED" : "CONFIGURED";
            if (policyState != "CONFIGURED") missing.Add("POLICY_" + policyState);
            if (assessment is null) missing.Add("ASSESSMENT_REQUIRED");
            else missing.AddRange(JsonSerializer.Deserialize<string[]>(assessment.MissingReasonsJson, Json) ?? []);
            if (authorization?.VerifiedStartedAt is null) missing.Add("FIRST_START_NOT_VERIFIED");
            else if (clock.GetUtcNow() >= authorization.ExpiresAt) missing.Add("EXECUTION_WINDOW_EXPIRED");
            var facts = new RepairEligibilityFacts(item.Id, binding.Id, binding.PolicyRevisionId, binding.PolicyContentHash,
                policyState, policy?.Measurements.OrderBy(row => row.Code, StringComparer.Ordinal).ToArray() ?? [], assessment?.Id,
                assessment?.Measurements.Select(row => row.MeasurementId).Order().ToArray() ?? [], authorization?.VerifiedStartedAt,
                authorization?.ExpiresAt, sources, missing.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), "");
            var version = "h4-eligibility-v1-" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(facts, Json))).ToLowerInvariant();
            await tx.CommitAsync(token); return new RepairEligibilityReadResult(200, Value: facts with { Version = version });
        });
}
