using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairProducerService(IRepairProducerRepository repository) : IRepairProducerService
{
    public Task<RepairWorkflowResult> CreatePackageAsync(Guid actor, UserRoleCode role, Guid project,
        RepairPackageCreateInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, UserRoleCode.ProjectManager, project, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.DefectId == Guid.Empty || input.DefectVersion != version![1..^1] ||
            !Text(input.Reason) || input.Obligations is null || input.Obligations.Length is < 1 or > 100 ||
            input.Obligations.Any(row => row is null || !Text(row.Reason) ||
                row.Kind is not ("FORMAL_REPAIR" or "TEMPORARY_SAFETY") || !Scope(row.Scope)))
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.CreatePackageAsync(new(actor, role, project, RepairContractMapping.ToData(input), key!, version[1..^1]), token));
    }
    public Task<RepairWorkflowResult> ProposeItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        RepairItemProposeInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, UserRoleCode.ProjectManager, project, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (package == Guid.Empty || input is null || input.ObligationId == Guid.Empty ||
            input.Mode is not ("NORMAL" or "FAST_TRACK") || !Text(input.Reason) || !Text(input.RepairPlan) || !Text(input.ChecklistVersion))
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.ProposeItemAsync(new(actor, role, project, package, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> ApproveItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairDecisionInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, UserRoleCode.Supervisor, project, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (package == Guid.Empty || item == Guid.Empty || input is null || !Text(input.Reason)) return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.ApproveItemAsync(new(actor, role, project, package, item, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> AssignItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        RepairItemAssignInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, UserRoleCode.ProjectManager, project, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (package == Guid.Empty || item == Guid.Empty || input is null || !Text(input.Reason) || input.Task is null ||
            input.Task.DefectId == Guid.Empty || input.Task.RouteVersionId == Guid.Empty || input.Task.AssignedToUserId == Guid.Empty ||
            input.Task.Purpose != "POST_REPAIR" || input.Task.RequiredMeasurementType is < 1 or > 4 ||
            input.Task.DueAt == default || input.Task.SurveyId == Guid.Empty || input.PolicyRevisionId == Guid.Empty)
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.AssignItemAsync(new(actor, role, project, package, item, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    private static RepairWorkflowResult? Admission(Guid actor, UserRoleCode actual, UserRoleCode required, Guid project,
        string? key, string? version)
    {
        if (actual != required) return new(403, "access_forbidden");
        if (actor == Guid.Empty || project == Guid.Empty) return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        return key.Length > 150 || key.Any(value => value is < '!' or > '~') || !Version(version)
            ? new(400, "validation_error") : null;
    }
    private static bool Version(string value)
    {
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"') return false;
        Span<byte> bytes = stackalloc byte[8]; var token = value[1..^1];
        return Convert.TryFromBase64String(token, bytes, out var count) && count == 8 && Convert.ToBase64String(bytes) == token;
    }
    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 2000;
    private static bool Scope(RepairActualScopeInput? value)
    {
        if (value is null || value.PhysicalRoadId == Guid.Empty || value.RouteVersionId == Guid.Empty ||
            value.SegmentSetId == Guid.Empty || value.LayoutRevisionId == Guid.Empty || value.FromMeters < 0 ||
            value.ToMeters <= value.FromMeters || value.OffsetToMeters <= value.OffsetFromMeters) return false;
        const decimal maximum = 999999999999999.999m;
        return new[] { value.FromMeters, value.ToMeters, value.OffsetFromMeters, value.OffsetToMeters }
            .All(bound => bound >= -maximum && bound <= maximum && decimal.Round(bound, 3) == bound);
    }
    private static Task<RepairWorkflowResult> Error(int status, string code) => Task.FromResult(new RepairWorkflowResult(status, code));
}
