using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class SurveyPlanCompletionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(SurveyV2Controller)) return;
        if (context.MethodInfo.Name == nameof(SurveyV2Controller.CreatePlan))
            DescribeConflict("survey_active_plan_conflict includes authorized existingPlanId/existingPlanStatus for the exact project + roadSection + surveyType active key. duplicate_request means a reused idempotency key with a different fingerprint. survey_source_scope_invalid means invalid source scope.");
        if (context.MethodInfo.Name == nameof(SurveyV2Controller.CreateTask))
            DescribeConflict("survey_task_plan_mismatch: type/scope incompatible with plan; survey_plan_state_conflict: plan cannot create a task; survey_source_scope_invalid: invalid source; duplicate_request: key/fingerprint conflict. A plan in another project remains 404.");
        if (context.MethodInfo.Name != nameof(SurveyV2Controller.CompletePlan)) return;
        operation.Description = "Current project PM explicitly completes an IN_PROGRESS BAND_V1 plan using persisted root/supplement lineage, submitted datasets and their current pm-evidence-review.v1 assessments. Exact pinned route/set/segment/band PASS coverage is required. Unhandled branches block completion. Does not complete tasks or change baseline. Completion basis, audit, plan version and receipt commit atomically. Replay rechecks current PM authority; retain the original body, If-Match and Idempotency-Key. GET plan returns completion basis after reload. No legacy backfill.";
        foreach (var parameter in operation.Parameters.Where(x => x.In == ParameterLocation.Header))
        {
            parameter.Required = true;
            parameter.Description = parameter.Name == "If-Match" ? "Quoted ETag from GET /api/v1/survey-plans/{id}. Keep original header on retry." : "New operation key; retain unchanged request on retry.";
        }
        if (operation.RequestBody is not null) operation.RequestBody.Required = true;
        foreach (var status in new[] { "401", "403", "404", "422", "428" })
            operation.Responses[status] = new OpenApiResponse
            {
                Description = status switch
                {
                    "401" => "unauthorized: missing/invalid session or Bearer",
                    "403" => "access_forbidden or csrf_failed for cookie writes",
                    "404" => "survey_plan_not_found",
                    "428" => "validation_error: missing If-Match or Idempotency-Key",
                    _ => "survey_validation_failed: invalid reason/source list"
                }
            };
        DescribeConflict("survey_plan_completion_blocked with reasonCode: plan_not_in_progress, plan_lineage_unavailable, plan_scope_unavailable, plan_source_task_invalid, plan_dataset_lineage_invalid, plan_assessment_not_current, plan_source_scope_mismatch, plan_scope_not_satisfied, plan_unhandled_task_branch. duplicate_request: same scoped key with different request.");
        operation.Responses["412"].Description = "auth_concurrency_conflict: stale plan ETag; reload before a new decision.";
        operation.Responses["200"].Headers["ETag"] = new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" }, Description = "New plan rowversion; task and dataset versions are independent." };

        foreach (var response in operation.Responses)
        {
            response.Value.Headers["X-Correlation-ID"] = new OpenApiHeader
            { Schema = new OpenApiSchema { Type = "string", Format = "uuid" }, Description = "Server request correlation ID." };
            if (response.Key != "200") response.Value.Content["application/problem+json"] = new OpenApiMediaType
            { Schema = context.SchemaGenerator.GenerateSchema(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository) };
        }

        void DescribeConflict(string description)
        {
            if (!operation.Responses.TryGetValue("409", out var response))
                operation.Responses["409"] = response = new OpenApiResponse();
            response.Description = description;
            response.Content["application/problem+json"] = new OpenApiMediaType
            { Schema = context.SchemaGenerator.GenerateSchema(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository) };
        }
    }
}
