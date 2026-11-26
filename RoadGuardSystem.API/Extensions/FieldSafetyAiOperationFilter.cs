using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.DTOs.Repairs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class FieldSafetyAiOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.MethodInfo.DeclaringType;
        var action = context.MethodInfo.Name;
        if (controller == typeof(RoadCoverageController) && action == "HandoverDocuments")
        {
            operation.Description = "Supervisor only; Bearer or Web session, no CSRF for GET. " +
                "Returns all project documents ordered by SQL uniqueidentifier ID, including missing fileId. " +
                "An empty items array means no documents, not missing authority. canAttachFile is false for a file already attached or any coverage mapping. " +
                "This inventory does not prove VERIFIED or REAL_SOURCE eligibility. " +
                "Use document id to select handoverDocumentId. The item version is document rowversion; " +
                "after upload VERIFIED, GET road-coverage for its aggregate ETag to POST attach. Cache-Control: no-store.";
        }
        if (controller == typeof(RepairPackagesController) && action is "TemporarySafetySources" or "AddTemporarySafety")
        {
            operation.Description = "Eligible unused, unresolved TemporarySafety obligations within the current NORMAL formal item scope. " +
                "Current responsibility determines the project in the URL; source package, Defect and geometry keep their original project. " +
                "GET requires current PM or Supervisor authority; POST requires current PM authority. Bearer or Web session. " +
                "ETag/packageVersion identify the package, itemVersion is for safety-measure creation, each obligation has its own version. " +
                "An empty collection is valid. Appending does not change approval, assignment or item state. " +
                "After formal transfer, a new creation action establishes safety responsibility without copying transfer acceptance.";
            if (action == "AddTemporarySafety")
            {
                operation.Description += " Web writes require X-CSRF-TOKEN from GET /api/v1/auth/web/csrf and credentials. " +
                    "201 creates one obligation; 200 replays the stored result after rechecking current authority. " +
                    "Scope must use the original pinned geometry and be contained in the formal scope.";
                Header("If-Match", "Quoted package ETag from the source collection GET. Stale package returns 409 concurrency_conflict.");
                Header("Idempotency-Key", "Persist the exact key/body/If-Match before sending; retry that same request after timeout.");
            }
            foreach (var code in new[] { "400", "401", "403", "404", "409", "428" })
                operation.Responses.TryAdd(code, new OpenApiResponse
                {
                    Description = code switch
                    {
                        "400" => "validation_error",
                        "401" => "unauthorized",
                        "403" => "access_forbidden or csrf_failed",
                        "404" => "not_found",
                        "409" => "concurrency_conflict, idempotency_key_reused, active_scope_conflict, safety_obligation_source_conflict or temporary_safety_not_allowed",
                        _ => "precondition_required"
                    }
                });
        }
        if (controller == typeof(AiMockRunsController))
        {
            operation.Description = action == "Candidate"
                ? "PM/Supervisor with current source authority; Bearer or Web session, no CSRF for GET. Read disposition.decisionId, then candidate-decisions/{decisionId} for defectId. Refresh creates nothing. MOCK/SYNTHETIC only."
                : "MOCK only. PM creates; PM/Supervisor read within current project scope. Bearer or Web session; Web POST requires CSRF. Errors add reasonCode/step/field/recovery. Run originCorrelationId links enqueue/worker/result; nullable failure preserves safe diagnostics.";
        }
        void Header(string name, string description)
        {
            if (!operation.Parameters.Any(x => x.In == ParameterLocation.Header && x.Name == name))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = description,
                    Schema = new OpenApiSchema { Type = "string" }
                });
        }
    }
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(AiMockRunView)) NullableReference("failure");
        if (context.Type == typeof(TemporarySafetyObligationSourcesView))
        {
            NullableReference("binding");
            if (schema.Properties.TryGetValue("items", out var items)) items.Nullable = false;
        }
        void NullableReference(string name)
        {
            if (schema.Properties.TryGetValue(name, out var property))
                schema.Properties[name] = new OpenApiSchema { Nullable = true, AllOf = [property] };
        }
    }

}
