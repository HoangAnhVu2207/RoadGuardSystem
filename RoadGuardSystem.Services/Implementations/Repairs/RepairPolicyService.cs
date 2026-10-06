using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairPolicyService(IRepairPolicyRepository repository) : IRepairPolicyService
{
    public Task<RepairPolicyServiceResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, string action,
        Guid? resource, RepairPolicyDefinitionInput? definition, string? reason, string? key, string? version,
        CancellationToken cancellationToken)
    {
        if (role != UserRoleCode.ProjectManager) return Error(403, "access_forbidden");
        if (actor == Guid.Empty || project == Guid.Empty || action is not ("create" or "update" or "publish" or "revoke" or "draft-get" or "revision-get") ||
            action != "create" && (resource is null || resource == Guid.Empty)) return Error(400, "validation_error");
        var read = action.EndsWith("-get", StringComparison.Ordinal);
        if (!read && string.IsNullOrWhiteSpace(key)) return Error(428, "precondition_required");
        if (!read && (key!.Length > 150 || key.Any(c => c is < '!' or > '~'))) return Error(400, "validation_error");
        string? expected = null;
        if (!read && action != "create")
        {
            if (string.IsNullOrWhiteSpace(version)) return Error(428, "precondition_required");
            if (version.Length < 3 || version[0] != '"' || version[^1] != '"') return Error(400, "validation_error");
            expected = version[1..^1];
            if (action != "revoke")
            {
                Span<byte> bytes = stackalloc byte[8];
                if (!Convert.TryFromBase64String(expected, bytes, out var count) || count != 8 ||
                    Convert.ToBase64String(bytes) != expected) return Error(400, "validation_error");
            }
            else if (!expected.StartsWith("h4-policy-v1-", StringComparison.Ordinal) || expected.Length != 77 ||
                expected[13..].Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
                return Error(400, "validation_error");
        }
        if (action is "create" or "update")
        {
            if (definition is null || string.IsNullOrWhiteSpace(definition.DefectTypeCode) || definition.DefectTypeCode.Length > 200 ||
                string.IsNullOrWhiteSpace(definition.ChecklistVersion) || definition.ChecklistVersion.Length > 200 ||
                string.IsNullOrWhiteSpace(definition.Reason) || definition.Reason.Length > 2000 ||
                definition.Measurements is null || definition.Measurements.Length > 200 ||
                definition.StopConditions is null || definition.StopConditions.Length > 100 ||
                definition.Measurements.Any(row => row is null || string.IsNullOrWhiteSpace(row.Code) || row.Code.Length > 200 ||
                    string.IsNullOrWhiteSpace(row.Unit) || row.Unit.Length > 80) ||
                definition.StopConditions.Any(row => string.IsNullOrWhiteSpace(row) || row.Length > 200)) return Error(400, "validation_error");
        }
        if (action is "publish" or "revoke" && (string.IsNullOrWhiteSpace(reason) || reason.Length > 2000))
            return Error(400, "validation_error");
        var input = definition is null ? null : new RepairPolicyDefinition(definition.DefectTypeCode,
            definition.ChecklistVersion, definition.Measurements.ToArray(), definition.StopConditions.ToArray(), definition.Reason);
        return MapAsync(repository.ExecuteAsync(new(actor, role, project, action, resource, input, reason, key, expected), cancellationToken));
    }
    private static async Task<RepairPolicyServiceResult> MapAsync(Task<RepairPolicyResult> pending)
    {
        var result = await pending; var row = result.Value;
        return new(result.Status, result.Code, row is null ? null : new(row.Id, row.ProjectId, row.CurrentChangeId,
            row.PublishedRevisionId, row.Revision, row.DefectTypeCode, row.ChecklistVersion, row.Measurements.ToArray(),
            row.StopConditions.ToArray(), row.State, row.ActorId, row.At, row.Reason, row.Version), result.Replayed);
    }
    private static Task<RepairPolicyServiceResult> Error(int status, string code) => Task.FromResult(new RepairPolicyServiceResult(status, code));
}
