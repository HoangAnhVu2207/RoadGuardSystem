using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairLifecycleService(IRepairLifecycleRepository repository) : IRepairLifecycleService
{
    public Task<RepairWorkflowResult> CancelItemAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, RepairCancellationInput input, string? key, string? version, CancellationToken token)
    {
        var denied = Admission(actor, role, project, package, item, key, version);
        if (denied is not null) return Task.FromResult(denied);
        if (input is null || !Text(input.Reason) || !Handover(input.Handover)) return Error();
        return Wire(repository.CancelItemAsync(new(actor, role, project, package, item,
            new(input.Reason, Data(input.Handover)), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> ContinueNormallyAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, RepairNormalContinuationInput input, string? key, string? version, CancellationToken token)
    {
        var denied = Admission(actor, role, project, package, item, key, version);
        if (denied is not null) return Task.FromResult(denied);
        if (input is null || !Text(input.Reason) || !Text(input.RepairPlan) || !Text(input.ChecklistVersion) ||
            input.ChecklistVersion.Length > 200 || !Handover(input.Handover)) return Error();
        return Wire(repository.ContinueNormallyAsync(new(actor, role, project, package, item,
            new(input.RepairPlan, input.ChecklistVersion, input.Reason, Data(input.Handover)), key!, version![1..^1]), token));
    }
    private static RepairWorkflowResult? Admission(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, string? key, string? version)
    {
        if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
        if (new[] { actor, project, package, item }.Any(id => id == Guid.Empty)) return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        Span<byte> bytes = stackalloc byte[8];
        if (key.Length > 150 || key.Any(value => value is < '!' or > '~') || version.Length < 3 ||
            version[0] != '"' || version[^1] != '"' ||
            !Convert.TryFromBase64String(version[1..^1], bytes, out var count) || count != 8 ||
            Convert.ToBase64String(bytes) != version[1..^1]) return new(400, "validation_error");
        return null;
    }
    private static bool Text(string? text) => !string.IsNullOrWhiteSpace(text) && text.Length <= 2000;
    private static bool Handover(RepairLifecycleHandoverInput? input) => input is null ||
        input.FirstStartId != Guid.Empty && input.RecipientUserId != Guid.Empty && Text(input.PerformedPortion) && Text(input.SafetyState);
    private static RepairLifecycleHandoverData? Data(RepairLifecycleHandoverInput? input) => input is null ? null :
        new(input.FirstStartId, input.RecipientUserId, input.PerformedPortion, input.SafetyState);
    private static Task<RepairWorkflowResult> Error() => Task.FromResult(new RepairWorkflowResult(400, "validation_error"));
    private static async Task<RepairWorkflowResult> Wire(Task<RepairWorkflowResult> operation)
    {
        var result = await operation;
        if (result.Value is not RepairLifecycleFact fact) return result;
        return result with { Value = new RepairLifecycleView(fact.SourceItemId, fact.SuccessorItemId, fact.ObligationId,
            fact.CancellationEventId, fact.HandoverEventId, fact.ContinuationId, fact.SourceState, fact.SuccessorState,
            fact.SourceVersion, fact.SuccessorVersion) };
    }
}
