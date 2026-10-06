using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Inspections;

namespace RoadGuardSystem.Services.Implementations.Inspections;
public sealed class FieldInspectionWorkflowService(IFieldInspectionWorkflowRepository repository, IProjectScopeGuard guard) : IFieldInspectionWorkflowService
{
    public async Task<FieldWorkflowResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, Guid? task,
        string action, object? input, string? key, string? expectedVersion, CancellationToken cancellationToken)
    {
        if (actor == Guid.Empty || project == Guid.Empty || task == Guid.Empty) return new(400, "validation_error");
        var read = action is "list" or "get" or "start-origin" or "submission" or "history" or "geometry" or "evidence";
        var pm = action is "create" or "assign" or "review" or "cancel" or "reassign" or "reuse" or "impact";
        var crew = action is "accept" or "reject" or "start" or "submit";
        if (read ? role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew) :
            pm ? role != UserRoleCode.ProjectManager : crew ? role != UserRoleCode.RepairCrew : true)
            return new(403, "access_forbidden");
        if (!read)
        {
            if (string.IsNullOrWhiteSpace(key)) return new(428, "precondition_required");
            if (key.Length > 150 || key.Any(x => x is < '!' or > '~')) return new(400, "validation_error");
            if (action != "create" && string.IsNullOrWhiteSpace(expectedVersion)) return new(428, "precondition_required");
            if (input is null) return new(400, "validation_error");
        }
        if (action is not ("create" or "list") && task is null) return new(400, "validation_error");
        try
        {
            var offlineClaim = input is FieldStartInput { OfflineProof: not null };
            var context = new FieldAdmissionContext(actor, role, actor, "DIRECT", !offlineClaim);
            return await repository.ExecuteAsync(new(project, task, action, input, key, expectedVersion?.Trim().Trim('"'), context),
                async token => await guard.AuthorizeAsync(actor, role, project, token) is not null, cancellationToken);
        }
        catch (ArgumentException) { return new(400, "validation_error"); }
        catch (JsonException) { return new(400, "validation_error"); }
    }
}
