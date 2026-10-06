using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairExecutionService(IRepairExecutionRepository repository) : IRepairExecutionService
{
    public Task<RepairWorkflowResult> AssessAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairMeasurementAssessmentInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.OriginId == Guid.Empty || input.FieldFirstStartId == Guid.Empty || input.DeviceId == Guid.Empty ||
            input.Measurements?.Length > 200 || input.Evidence?.Length > 100)
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.AssessAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> StartExecutionAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairExecutionStartInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.OriginId == Guid.Empty || input.FieldFirstStartId == Guid.Empty || input.AssessmentId == Guid.Empty ||
            input.DeviceId == Guid.Empty || input.ClaimedAt == default || input.MonotonicMilliseconds < 0 || input.BootId?.Length > 200)
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.StartExecutionAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> FinishExecutionAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairExecutionFinishInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.OriginId == Guid.Empty || input.ExecutionStartId == Guid.Empty || input.DeviceId == Guid.Empty ||
            input.ClaimedAt == default || input.MonotonicMilliseconds < 0 || input.BootId?.Length > 200)
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.FinishExecutionAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> SubmitAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid task, RepairAttemptSubmitInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.FieldSubmissionId == Guid.Empty || input.ExecutionFinishId == Guid.Empty ||
            input.ExpectedContentHash is not { Length: 64 } || input.ExpectedContentHash.Any(value => !char.IsAsciiHexDigit(value)))
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.SubmitAttemptAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> SupplementAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid task, RepairAttemptSupplementInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.FieldSubmissionId == Guid.Empty || input.ExpectedContentHash is not { Length: 64 } ||
            input.ExpectedContentHash.Any(value => !char.IsAsciiHexDigit(value))) return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.SupplementAttemptAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> ReviewAttemptAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid task, RepairAttemptReviewInput input, string? key, string? version, CancellationToken token)
    {
        var denial = Admission(actor, role, project, package, item, task, key, version, UserRoleCode.ProjectManager);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || input.FieldSubmissionId == Guid.Empty ||
            input.Decision is not ("SUPPLEMENT" or "ACCEPT") ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000) return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.ReviewAttemptAsync(new(actor, role, project, package, item, task, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    public Task<RepairWorkflowResult> ConfirmFinalAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, RepairDecisionInput input, string? key, string? version, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor))
            return Error(403, "access_forbidden");
        var denial = Admission(actor, role, project, package, item, null, key, version, role);
        if (denial is not null) return Task.FromResult(denial);
        if (input is null || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000)
            return Error(400, "validation_error");
        return RepairContractMapping.ToWireAsync(repository.ConfirmFinalAsync(new(actor, role, project, package, item, RepairContractMapping.ToData(input), key!, version![1..^1]), token));
    }
    private static RepairWorkflowResult? Admission(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        Guid? task, string? key, string? version, UserRoleCode required = UserRoleCode.RepairCrew)
    {
        if (role != required) return new(403, "access_forbidden");
        if (actor == Guid.Empty || project == Guid.Empty || package == Guid.Empty || item == Guid.Empty || task == Guid.Empty)
            return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        if (key.Length > 150 || key.Any(value => value is < '!' or > '~') || version.Length < 3 || version[0] != '"' || version[^1] != '"')
            return new(400, "validation_error");
        Span<byte> bytes = stackalloc byte[8]; var encoded = version[1..^1];
        return Convert.TryFromBase64String(encoded, bytes, out var count) && count == 8 && Convert.ToBase64String(bytes) == encoded
            ? null : new(400, "validation_error");
    }
    private static Task<RepairWorkflowResult> Error(int status, string code) => Task.FromResult(new RepairWorkflowResult(status, code));
}
