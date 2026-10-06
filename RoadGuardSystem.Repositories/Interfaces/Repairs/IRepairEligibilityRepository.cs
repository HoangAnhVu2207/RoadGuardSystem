using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairEligibilityQuery(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId, Guid ItemId);
public sealed record RepairEligibilityFacts(Guid ItemId, Guid BindingId, Guid? PolicyRevisionId, string? PolicyContentHash,
    string PolicyState, RepairMeasurementRule[] Rules, Guid? AssessmentId, Guid[] MeasurementIds,
    DateTimeOffset? OriginalVerifiedStart, DateTimeOffset? ExecutionExpiresAt, RepairEligibilitySourceSnapshot Sources,
    string[] MissingReasons, string Version);
public sealed record RepairEligibilityReadResult(int Status, string? Code = null, RepairEligibilityFacts? Value = null);
public interface IRepairEligibilityRepository
{
    Task<RepairEligibilityReadResult> ReadAsync(RepairEligibilityQuery query, CancellationToken token);
}
