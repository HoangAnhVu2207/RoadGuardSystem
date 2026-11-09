using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class UploadCancelOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(UploadsController) ||
            context.MethodInfo.Name != nameof(UploadsController.Cancel)) return;

        operation.Description = "Cancel an owned PENDING or UPLOADING session using Bearer. RepairCrew can cancel BEFORE, AFTER, and MEASUREMENT photos. DroneOperator can cancel SURVEY_VIDEO and TELEMETRY. Current project membership and task upload authority are required, including on repeat. No body, If-Match, or Idempotency-Key.";
        operation.Responses["409"].Description = "upload_not_cancellable. The upload is VERIFYING, VERIFIED, or failed for another reason.";
    }
}
