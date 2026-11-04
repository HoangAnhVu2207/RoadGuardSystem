using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class CaseReportDetailsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(CaseReportDetailsController)) return;
        if (context.MethodInfo.Name == nameof(CaseReportDetailsController.List))
        {
            operation.Description = "Supervisor or current-project PM reads currently linked Reports, ordered by receivedAt DESC then reportId DESC. Follow nextCursor using the same actor, Case and limit (1-100). Every page includes each Report's original evidence and all supplements. Reporter name, phone and phone confirmation come from the current account and can change after submission; a missing phone is null. Missing evidence position/time stays null or UNKNOWN.";
        }
        else if (context.MethodInfo.Name == nameof(CaseReportDetailsController.Download))
        {
            operation.Description = "Authenticated private image stream. Checks current Case authority, active Report-Case link, evidence and verified private file before opening storage. No signed URL is issued. An already opened stream cannot be recalled retroactively after rights change.";
            operation.Responses["200"].Content.Clear();
            foreach (var mediaType in new[] { "image/jpeg", "image/png" })
                operation.Responses["200"].Content[mediaType] = new OpenApiMediaType
                { Schema = new OpenApiSchema { Type = "string", Format = "binary" } };
        }
    }
}
