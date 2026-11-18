using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class WebPmCompletionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.MethodInfo.DeclaringType;
        var action = context.MethodInfo.Name;
        if (controller == typeof(WebCoordinationController) ||
            controller == typeof(H6NotificationOperationsController) && action == "MarkManyRead")
        {
            var write = context.ApiDescription.HttpMethod is "PUT" or "POST";
            operation.Description = "Authenticated Bearer or Web session. Project reads require current PM membership or Supervisor authority. " +
                "Web writes require X-CSRF-TOKEN from GET /api/v1/auth/web/csrf and browser credentials. " +
                "Locality/organization writes require Supervisor; planning writes require current project PM. " +
                "Repair policy discovery remains PM-only. Null means metadata was not stored; names are current account names.";
            if (write)
            {
                Header("Idempotency-Key", "Persist the key and exact body before sending; replay the same request after timeout.");
                if (context.ApiDescription.HttpMethod == "PUT") Header("If-Match", "Quoted current resource rowversion from its GET response. A stale version returns 409 concurrency_conflict.");
            }
            foreach (var status in new[] { "400", "401", "403", "404", "409", "428" })
                operation.Responses.TryAdd(status, new OpenApiResponse
                {
                    Description = status switch
                    { "400" => "validation_error", "401" => "Authentication missing or no longer valid", "403" => "access_forbidden or csrf_failed", "404" => "not_found", "409" => "concurrency_conflict or duplicate_request", _ => "precondition_required" }
                });
            if (action == "MarkManyRead") operation.Description += " Batch limit 1–100. First call captures a database rowversion snapshot. " +
                "Repeat subsequent batches with the returned snapshotVersion and the same through cutoff, using a new key per batch. " +
                "Newly inserted notifications, including backdated events, are outside this snapshot. Entire batch commits atomically; no partial success.";
        }
        if (controller == typeof(UploadsController) && action == "Download")
        {
            operation.Description = "Authenticated private content. Web PM/Supervisor may play a VERIFIED SURVEY_VIDEO linked to a dataset on the stored task. " +
                "Operator Bearer requires current task assignment and project membership. Each request rechecks authority. " +
                "Single bytes Range supports start-end, start-, and -suffix. Multiple or invalid ranges return 416. " +
                "If-Range accepts only the exact strong current ETag; other validators return the whole 200 representation. " +
                "Storage ranges are streamed directly; codec/container support is the player's responsibility.";
            foreach (var status in new[] { "200", "206", "416" })
            {
                var response = new OpenApiResponse { Description = status == "206" ? "Selected byte interval" : status == "416" ? "Unsupported or unsatisfiable byte interval" : "Full representation" };
                foreach (var header in new[] { "Accept-Ranges", "Content-Range", "Content-Length", "ETag" })
                    response.Headers[header] = new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } };
                if (status != "416") response.Content["video/mp4"] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = "string", Format = "binary" } };
                operation.Responses[status] = response;
            }
            Header("Range", "Optional single byte interval.", false);
            Header("If-Range", "Optional exact strong ETag; dates and weak validators fall back to 200.", false);
        }
        if (controller == typeof(DiscoveryCollectionsController) && action == "MockRuns")
        {
            foreach (var name in new[] { "datasetId", "surveyTaskId" })
                if (!operation.Parameters.Any(parameter => parameter.Name == name)) operation.Parameters.Add(new OpenApiParameter
                { Name = name, In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string", Format = "uuid" }, Description = "Filter by the persisted processing/dataset/task relation in this project. Bound to cursor." });
        }
        if (controller == typeof(ExportsController) && action == "Create")
            operation.Description += " DOSSIER supports repairItemIds (max 100) or repairPackageIds (max 20), with at most 100 resolved items. " +
                "These scopes cannot combine with segmentIds/defectIds/time filters. Snapshot is immutable; missing sections are explicit. " +
                "ZIP may include authorized original JPEG/PNG evidence; PDF does not embed original files. Source authority is checked at admission, processing and download.";

        void Header(string name, string description, bool required = true)
        {
            if (!operation.Parameters.Any(parameter => parameter.In == ParameterLocation.Header && parameter.Name == name))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Header,
                    Required = required,
                    Description = description,
                    Schema = new OpenApiSchema { Type = "string" }
                });
        }
    }
}
