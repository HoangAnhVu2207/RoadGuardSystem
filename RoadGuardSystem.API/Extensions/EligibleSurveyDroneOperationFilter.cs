using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class EligibleSurveyDroneOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(SurveyV2Controller) ||
            context.MethodInfo.Name != nameof(SurveyV2Controller.EligibleDrones)) return;
        operation.Description = "Assigned DroneOperator with current project membership lists Active drones from the shared registry. Selection records the drone used and does not reserve it. Stable ID order; follow nextCursor. New dataset submissions recheck Active status; committed replay retains its result.";
        var limit = operation.Parameters.Single(p => p.Name == "limit");
        limit.Schema.Minimum = 1;
        limit.Schema.Maximum = 100;
        operation.Responses["200"].Headers["Cache-Control"] = new OpenApiHeader
        { Schema = new OpenApiSchema { Type = "string" }, Description = "private, no-store" };
    }
}
