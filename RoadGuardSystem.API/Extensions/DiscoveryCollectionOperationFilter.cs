using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class DiscoveryCollectionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(DiscoveryCollectionsController)) return;
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "limit",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 100, Default = new OpenApiInteger(25) }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "cursor",
            In = ParameterLocation.Query,
            Description = "Opaque continuation token bound to actor, route, project, package and mode. Current authority is reapplied; no snapshot guarantee.",
            Schema = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 4096 }
        });
        if (context.MethodInfo.Name == nameof(DiscoveryCollectionsController.RepairItems))
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "mode",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema { Type = "string", Enum = [new OpenApiString("NORMAL"), new OpenApiString("FAST_TRACK")] }
            });
        operation.Description = "Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.";
        operation.Responses["200"].Headers["Cache-Control"] = new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" }, Description = "private, no-store" };
    }
}
