using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class GeometrySubmissionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(GeometryWorkflowController)) return;

        switch (context.MethodInfo.Name)
        {
            case nameof(GeometryWorkflowController.Submit):
                operation.Description = "Current-project PM submits one ready draft per project. The submitted content becomes immutable. Replay returns the original receipt after current authorization.";
                AddHeader(operation, "If-Match", "Quoted ETag from the saved draft.");
                AddHeader(operation, "Idempotency-Key", "One key per submission. Retry the same draft and ETag with the same key.");
                AddHeaderToResponse(operation.Responses["201"], "ETag", "Draft ETag after submission. Use it for confirm.");
                AddHeaderToResponse(operation.Responses["201"], "Location", "Project submission lookup URL.");
                AddError(operation, context, "409", "road_geometry_submission_exists, duplicate_request, project_closed or road_geometry_invalid_state");
                AddError(operation, context, "412", "concurrency_conflict");
                AddError(operation, context, "422", "Draft readiness, CRS or topology validation failed.");
                AddError(operation, context, "428", "validation_error: missing preconditions.");
                break;
            case nameof(GeometryWorkflowController.Submission):
                operation.Description = "Supervisor or current-project PM finds the submitted dossier by project ID. Returns SUBMITTED or CONFIRMED and the current draft ETag.";
                AddHeaderToResponse(operation.Responses["200"], "ETag", "Current draft ETag.");
                break;
            case nameof(GeometryWorkflowController.Confirm):
                operation.Description += " Supervisor confirms the project's submitted draft using its current ETag. A DRAFT without a submission returns road_geometry_submission_required.";
                operation.Responses["409"].Description += " Also returns road_geometry_submission_required for an unsubmitted draft.";
                return;
            default:
                return;
        }

        AddError(operation, context, "401", "unauthorized");
        AddError(operation, context, "403", "access_forbidden or csrf_failed for a Web write.");
        AddError(operation, context, "404", "road_geometry_submission_not_found, road_geometry_draft_not_found or project_not_found");
    }

    private static void AddHeader(OpenApiOperation operation, string name, string description)
        => operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema { Type = "string" },
            Description = description
        });

    private static void AddHeaderToResponse(OpenApiResponse response, string name, string description)
        => response.Headers[name] = new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" }, Description = description };

    private static void AddError(OpenApiOperation operation, OperationFilterContext context, string status, string description)
        => operation.Responses[status] = new OpenApiResponse
        {
            Description = description,
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)
                }
            }
        };
}
