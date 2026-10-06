using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Inspections;

namespace RoadGuardSystem.Services.Inspections;
public interface IFieldInspectionWorkflowService
{
    Task<FieldWorkflowResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, Guid? task,
        string action, object? input, string? key, string? expectedVersion, CancellationToken cancellationToken);
}
