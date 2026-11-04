using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class ProjectCreationWebOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ProjectsController) ||
            context.MethodInfo.Name != nameof(ProjectsController.Create)) return;

        operation.Description = "Supervisor only. Bearer or Web cookie. For cookie requests, get requestToken " +
            "from GET /api/v1/auth/web/csrf and send X-CSRF-TOKEN with credentials. Supply Idempotency-Key " +
            "or body operationId. handoverFileIds: [] means no attachment; handoverDate and warrantyEndDate " +
            "are required. Same-key changed-body retry returns 409.";
        if (operation.RequestBody?.Content.TryGetValue("application/json", out var json) == true)
            json.Example = new OpenApiObject
            {
                ["code"] = new OpenApiString("RG-2026-001"),
                ["name"] = new OpenApiString("Road maintenance project"),
                ["primaryPmId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["handoverDate"] = new OpenApiString("2026-11-01"),
                ["warrantyEndDate"] = new OpenApiString("2027-11-01"),
                ["handoverFileIds"] = new OpenApiArray()
            };
    }
}
