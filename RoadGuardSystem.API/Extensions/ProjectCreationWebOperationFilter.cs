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

        operation.Description = "Supervisor only. Obtain CSRF from GET /api/v1/auth/web/csrf for Web writes. " +
            "Create a staging upload, wait for VERIFIED, then send its fileId in handoverFileIds and ETag in If-Match. " +
            "Retry the same request with the same Idempotency-Key.";
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "If-Match",
            In = ParameterLocation.Header,
            Description = "Quoted ETag from GET /api/v1/uploads/{uploadId} after VERIFIED. Required for new projects.",
            Schema = new OpenApiSchema { Type = "string" }
        });
        var missing = new OpenApiResponse
        {
            Description = "handover_required: one staged fileId and its quoted upload ETag are required."
        };
        missing.Headers["X-Correlation-ID"] = new OpenApiHeader
        {
            Description = "Request correlation UUID.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        operation.Responses["428"] = missing;
        if (operation.RequestBody?.Content.TryGetValue("application/json", out var json) == true)
            json.Example = new OpenApiObject
            {
                ["code"] = new OpenApiString("RG-2026-001"),
                ["name"] = new OpenApiString("Road maintenance project"),
                ["primaryPmId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["handoverDate"] = new OpenApiString("2026-11-01"),
                ["warrantyEndDate"] = new OpenApiString("2027-11-01"),
                ["handoverFileIds"] = new OpenApiArray { new OpenApiString("22222222-2222-2222-2222-222222222222") }
            };
    }
}
