using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Services.Implementations.Repairs;
public sealed class RepairEligibilityService(IRepairEligibilityRepository repository) : RoadGuardSystem.Services.Repairs.IRepairEligibilityService
{
    public async Task<RepairEligibilityServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item, CancellationToken token)
    {
        if (new[] { actor, project, package, item }.Any(id => id == Guid.Empty)) return new(400, "validation_error");
        var result = await repository.ReadAsync(new(actor, role, project, package, item), token); var facts = result.Value;
        return new(result.Status, result.Code, facts is null ? null : new(false, "OWNER_SOURCE_ACTIVATION_PENDING", facts.ItemId,
            facts.BindingId, facts.PolicyRevisionId, facts.PolicyContentHash, facts.PolicyState, facts.Rules.ToArray(), facts.AssessmentId,
            facts.MeasurementIds.ToArray(), facts.OriginalVerifiedStart, facts.ExecutionExpiresAt, facts.Sources.SourceMapping,
            facts.Sources.Warranties.ToArray(), facts.Sources.Handovers.ToArray(), facts.MissingReasons.ToArray(), facts.Version));
    }
}
