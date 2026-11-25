using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.DTOs.Repairs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class WebBReadOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RepairSafetyResolveInput))
        {
            schema.Properties["outcome"].Enum = [new Microsoft.OpenApi.Any.OpenApiString("REMOVED"), new Microsoft.OpenApi.Any.OpenApiString("REPLACED")];
            schema.Properties["conditionSatisfied"].Description = "Explicit PM attestation of the recorded removal/replacement condition; not inferred from formal completion.";
            schema.Properties["evidenceIds"].Description = "FIELD evidence-link IDs from the current Safe check, not file IDs.";
            schema.Properties["obligationVersion"].Description = "Unquoted obligation.version from GET this measure; distinct from measure If-Match.";
        }
        if (context.Type == typeof(RoadGuardSystem.DTOs.Defects.CandidateMatchItemDto))
        {
            schema.Properties["distanceMeters"].Description = "Nullable distance in metres. distanceMethod distinguishes ellipsoid point distance from minimum projected grid distance to the actual shape.";
            schema.Properties["accuracyMeters"].Description = "Stored accuracy of the confirmed source evidence; not target uncertainty or projection accuracy. Missing stays null.";
            schema.Properties["distanceStatus"].Description = "MEASURED or the actual source/geometry/CRS unavailability reason; no invented distance or snapping.";
            schema.Properties["overlapStatus"].Description = "SOURCE_UNCERTAINTY_UNAVAILABLE or TARGET_UNCERTAINTY_UNAVAILABLE until compatible sourced uncertainty exists on both sides.";
            schema.Properties["accuracyOverlap"].Description = "Omitted while unknown. No default radius, confidence or false value substitutes for missing uncertainty.";
        }
        var names = context.Type == typeof(RoadGuardSystem.DTOs.Defects.ReportMatchingLocationView) ? new[] { "confirmation" }
            : context.Type == typeof(RepairItemView) ? ["sourceContext"]
            : context.Type == typeof(RepairItemSourceView) ? ["task"]
            : context.Type == typeof(RepairSafetyView) ? ["obligation", "resolution"]
            : context.Type == typeof(RoadGuardSystem.DTOs.Projects.ObligationResponsibilityDto) ? ["safetyContext"]
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
            operation.Description = "Current project PM confirms evidence location and/or pinned segments for advisory matching. Original evidence is unchanged. GET returns versions and confirmation state. POST requires Idempotency-Key and cookie CSRF; stale versions return 409. Replay rechecks authority.";
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
            operation.Description = "Current item responsibility permits only this bound source. Bearer or Web cookie; GET needs no CSRF. Private content rechecks lineage and authority after storage opens. No source-project membership or write permission is granted.";
            Errors(operation, false);
            if (action == "Content") Binary(operation);
        }
        if (controller == typeof(FieldInspectionWorkflowController) && action == "History")
            operation.Description = "Current task authority applies. PM/Supervisor receive submission revisions, exact reviews and reuse provenance in details; Crew retain assignments/events only. Metadata does not grant file access.";
        if (controller == typeof(RepairsController) && action == "History")
        {
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RepairHistoryView));
            operation.Description = "Current item responsibility permits attempt, intake, review and binding history. Review.current matches the effective submission. Supplements do not inherit reviews. History ETag is not an item command version.";
        }
        if (controller == typeof(RepairsController) && action == "Item")
        {
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RepairItemView));
            operation.Description = "Current item responsibility permits this item and its sourceContext: origin, current responsibility, defect scope and task pins. currentBinding retains origin links after transfer. Commands use item ETag.";
        }
        if (controller == typeof(RepairSafetyController) && action == "ListCrew")
            operation.Description = "Crew see assigned unresolved measures. PM/Supervisor discover this item’s measures under current safety responsibility. Read each measure for checks, warnings, ACK and history. This does not grant writes or change the separate unresolved selection.";
        if (controller == typeof(RepairSafetyController) && action == "Resolve")
        {
            operation.Description = "Current safety PM only: confirm recorded removal/replacement condition against the current Safe check and verified evidence. Requires measure If-Match, obligationVersion and Idempotency-Key; cookie also needs CSRF. Resolution is immutable and independent of formal completion.";
            Errors(operation, true);
            foreach (var name in new[] { "If-Match", "Idempotency-Key" })
                if (!operation.Parameters.Any(p => p.In == ParameterLocation.Header && p.Name == name))
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = name,
                        In = ParameterLocation.Header,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = name == "If-Match" ? "Current ETag from GET this safety measure." : "Persist original key and body for uncertain outcome retry."
                    });
        }
        if (controller == typeof(CasesController) && action == "Conclude")
        {
            operation.Responses.Remove("201");
            operation.Responses["200"] = JsonResponse(operation, context, typeof(RoadGuardSystem.DTOs.Cases.InternalCaseDto));
            operation.Description = "PM conclusion returns 200 InternalCaseDto with Case ETag; publication remains 201. Current authority, evidence, If-Match, Idempotency-Key and cookie CSRF apply.";
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
