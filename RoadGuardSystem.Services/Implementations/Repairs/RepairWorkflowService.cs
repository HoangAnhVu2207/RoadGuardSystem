using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairWorkflowService(IRepairWorkflowRepository repository) : IRepairWorkflowService
{
    public Task<RepairWorkflowResult> RequestReviewAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, RepairReviewRequestInput input, string? key, string? expectedVersion,
        CancellationToken cancellationToken)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew))
            return Task.FromResult(new RepairWorkflowResult(403, "access_forbidden"));
        if (actorId == Guid.Empty || projectId == Guid.Empty || packageId == Guid.Empty || itemId == Guid.Empty || input is null)
            return Task.FromResult(new RepairWorkflowResult(400, "validation_error"));
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expectedVersion))
            return Task.FromResult(new RepairWorkflowResult(428, "precondition_required"));
        if (key.Length > 150 || key.Any(value => value is < '!' or > '~') || !ValidVersion(expectedVersion) ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000)
            return Task.FromResult(new RepairWorkflowResult(400, "validation_error"));
        return RepairContractMapping.ToWireAsync(repository.RequestReviewAsync(new(actorId, role, projectId, packageId, itemId, RepairContractMapping.ToData(input),
            key, expectedVersion[1..^1]), cancellationToken));
    }
    public Task<RepairWorkflowResult> CorrectAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, RepairCorrectionInput input, string? key, string? expectedVersion,
        CancellationToken cancellationToken)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor))
            return Task.FromResult(new RepairWorkflowResult(403, "access_forbidden"));
        if (actorId == Guid.Empty || projectId == Guid.Empty || packageId == Guid.Empty || itemId == Guid.Empty || input is null)
            return Task.FromResult(new RepairWorkflowResult(400, "validation_error"));
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expectedVersion))
            return Task.FromResult(new RepairWorkflowResult(428, "precondition_required"));
        if (key.Length > 150 || key.Any(value => value is < '!' or > '~') || !ValidVersion(expectedVersion) ||
            input.SupersedesDecisionId == Guid.Empty || input.Result is not ("UNREPAIRED" or "REPORTED_AWAITING_REVIEW" or "CONFIRMED") ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000 || input.Basis is null ||
            string.IsNullOrWhiteSpace(input.Basis.Text) || input.Basis.Text.Length > 2000 ||
            input.Basis.EvidenceReferenceIds is null || input.Basis.EvidenceReferenceIds.Length > 100 ||
            input.Basis.EvidenceReferenceIds.Any(id => id == Guid.Empty) ||
            input.Basis.EvidenceReferenceIds.Distinct().Count() != input.Basis.EvidenceReferenceIds.Length)
            return Task.FromResult(new RepairWorkflowResult(400, "validation_error"));
        return RepairContractMapping.ToWireAsync(repository.CorrectAsync(new(actorId, role, projectId, packageId, itemId, RepairContractMapping.ToData(input), key,
            expectedVersion[1..^1]), cancellationToken));
    }
    private static bool ValidVersion(string value)
    {
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"') return false;
        Span<byte> bytes = stackalloc byte[8];
        var token = value[1..^1];
        return Convert.TryFromBase64String(token, bytes, out var count) && count == 8 &&
            Convert.ToBase64String(bytes) == token;
    }
    public Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken)
        => RepairContractMapping.ToWireAsync(repository.ReadItemAsync(actorId, role, projectId, packageId, itemId, history, cancellationToken));
}
