using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.DTOs.Repairs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class WebBReadOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var names = context.Type == typeof(RoadGuardSystem.DTOs.Defects.ReportMatchingLocationView) ? new[] { "confirmation" }
            : context.Type == typeof(RepairItemView) ? ["sourceContext"]
            : context.Type == typeof(RepairItemSourceView) ? ["task"]
            : context.Type == typeof(RepairSafetyView) ? ["obligation"]
            : context.Type == typeof(RoadGuardSystem.DTOs.Inspections.FieldHistoryView) ? ["details"] : [];
        foreach (var name in names)
            if (schema.Properties.TryGetValue(name, out var property))
                schema.Properties[name] = new OpenApiSchema { Nullable = true, AllOf = [property] };
    }
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.MethodInfo.DeclaringType;
        var action = context.MethodInfo.Name;
        if (controller == typeof(ReportMatchingLocationController))
        {
            operation.Description = "Current project PM only. Explicit matching-only confirmation; original evidence remains immutable. " +
                "GET discovers sourceVersion, geometryVersion, original evidence locations and current/stale confirmation. " +
                "POST selects evidenceId (with stored coordinates), segmentIds, or both, with reason. All segments must belong to the Case's pinned geometry. " +
                "No automatic representative GPS, snapping, FIELD verification or execution grant. Source/geometry change returns 409 candidate_stale. " +
                "200 includes GPS_MISSING, GPS_UNCONFIRMED, GPS_CONFIRMED, SCOPE_CONFIRMED or MATCHING_LOCATION_STALE. " +
                "Bearer or Web cookie; GET needs no CSRF. POST requires X-CSRF-TOKEN for cookie and Idempotency-Key for both transports; replay rechecks current authority.";
            if (action == "Confirm")
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Idempotency-Key",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Persist and retry the same key/body. Different body with the same actor/project/operation key returns 409 idempotency_key_reused. Versions are in the body, no If-Match."
                });
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-CSRF-TOKEN",
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Required only with Web session, from GET /api/v1/auth/web/csrf in the same session. Bearer does not require CSRF; security lists these as alternatives."
                });
            }
            Errors(operation, action == "Confirm");
        }
        if (controller == typeof(RepairSourceReadsController) || controller == typeof(RepairSourceEvidenceController))
        {
            operation.Description = "PM/Supervisor with current exact item obligation responsibility; projectId is the responsible project. " +
                "Retains original source project and validates exact item/task/source links. No origin-project membership or mutation rights are granted. " +
                "Bearer or Web cookie, GET without CSRF. Each request rechecks current authority; private content rechecks again after opening storage. " +
                "History/submission metadata is distinct from private file access. No signed URL; an open stream cannot be recalled after later rights changes.";
            Errors(operation, false);
            if (action == "Content") Binary(operation);
        }
        if (controller == typeof(FieldInspectionWorkflowController) && action == "History")
            operation.Description = "Existing task authority and assignment guards apply. assignments/events retain their wire. " +
                "PM/Supervisor additionally receive details.submissions (revision/root/parent/hash), details.reviews (exact submission) and details.evidenceReuse (original provenance). " +
                "Crew response has no details field. Metadata never grants source download access.";
        if (controller == typeof(RepairsController) && action == "History")
        {
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RepairHistoryView));
            operation.Description = "Exact current obligation responsibility read. Immutable attempts/decisions/reviewRequests plus intakeSubmissions, attemptReviews and taskBindings. " +
                "Review.current requires the exact current attempt/link/effective submission; supplements do not inherit old reviews. History ETag is not a command item rowversion.";
        }
        if (controller == typeof(RepairsController) && action == "Item")
        {
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RepairItemView));
            operation.Description = "Exact current obligation responsibility read. sourceContext retains originProjectId, responsibilityProjectId, source defect/version, physical scope and valid current task pins. " +
                "currentBinding remains tied to the immutable origin project after transfer. Item ETag is for item commands; source/history tokens are not interchangeable.";
        }
        if (controller == typeof(RepairSafetyController) && action == "ListCrew")
            operation.Description = "Crew: current assigned unresolved measures only, unchanged. PM/Supervisor: measureIds for this item and currently responsible safety obligations, including historical resolved measures. " +
                "Use GET safety-measures/{measureId} to read checks/warnings/ACK and obligation scope/origin/current responsibility. Formal and safety responsibility are independent. " +
                "This is history discovery, separate from the unresolved/unused temporary-safety-obligations selection. No new write grant. Bearer or Web cookie; GET needs no CSRF.";
        if (controller == typeof(CasesController) && action == "Conclude")
        {
            operation.Responses.Remove("201");
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RoadGuardSystem.DTOs.Cases.InternalCaseDto));
            operation.Description = "PM conclusion returns 200 InternalCaseDto with Case ETag, including replay. This is distinct from publication (201). " +
                "Existing authority, If-Match, Idempotency-Key, evidence and CSRF requirements remain unchanged.";
        }
    }
    private static OpenApiResponse JsonResponse(OpenApiOperation operation, OperationFilterContext context, Type type)
    {
        var response = new OpenApiResponse
        {
            Description = "Current scoped representation",
            Headers = { ["ETag"] = new OpenApiHeader { Description = "Quoted version of this representation. Item/Case rowversion and history hash are distinct; use only the command's own resource version.", Schema = new OpenApiSchema { Type = "string" } } },
            Content = { ["application/json"] = new OpenApiMediaType { Schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository) } }
        };
        if (operation.Responses.TryGetValue("200", out var existing))
            foreach (var header in existing.Headers) response.Headers.TryAdd(header.Key, header.Value);
        return response;
    }
    private static void Binary(OpenApiOperation operation)
    {
        var response = new OpenApiResponse { Description = "Authenticated private bytes; current scoped authority required" };
        foreach (var media in new[] { "image/jpeg", "image/png", "video/mp4", "application/octet-stream" })
            response.Content[media] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = "string", Format = "binary" } };
        operation.Responses["200"] = response;
        operation.Responses.TryAdd("503", new OpenApiResponse { Description = "storage_unavailable" });
    }
    private static void Errors(OpenApiOperation operation, bool write)
    {
        foreach (var code in new[] { "400", "401", "403", "404", "409" })
            operation.Responses.TryAdd(code, new OpenApiResponse
            {
                Description = code switch
                { "400" => "validation_error", "401" => "unauthorized/session invalid", "403" => "access_forbidden/CSRF failed", "404" => "not_found in current scope", _ => "source_not_ready/candidate_stale/matching_scope_conflict/matching_evidence_location_unavailable/idempotency_key_reused" }
            });
        if (write) operation.Responses.TryAdd("428", new OpenApiResponse { Description = "precondition_required" });
    }
}
