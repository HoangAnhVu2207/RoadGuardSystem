using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class ReporterRoutingOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(CaseRoutingController)) return;
        operation.Description = context.MethodInfo.Name switch
        {
            nameof(CaseRoutingController.Map) => "Active Supervisor organization map. WGS84 display geometry; bounded registry pages may be empty. Follow nextCursor with identical filters. Authorize before conditional GET. Unlocated routes have unknown bbox relation.",
            nameof(CaseRoutingController.Candidates) => "Exact currently linked Report evidence. Sup reads organization candidates; PM is limited to the current Case project. Canonical UTM grid distance does not prove corridor inclusion or responsibility. No business radius or automatic routing.",
            nameof(CaseRoutingController.History) => "Supervisor-only append-only intake decisions, ordered by SQL identifier ascending. Follow the opaque cursor with the same Case and limit. Source changes require review; triage supersedes disposition without erasing history.",
            _ => "Supervisor decides intake for an unassigned Case. Current evidence and authoritative sources are rechecked. Outside scope requires a complete source-backed whole-Case attestation. No technical conclusion, publication or automatic work is created."
        };
        foreach (var name in new[] { "If-Match", "Idempotency-Key" })
            if (context.MethodInfo.Name == nameof(CaseRoutingController.Decide))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = name == "If-Match" ? "Quoted Case ETag from GET Case. Stale version returns 412 concurrency_conflict."
                        : "Persist the exact key, body and If-Match before sending. Retry the unchanged request after timeout."
                });
        foreach (var parameter in operation.Parameters.Where(p => p.In == ParameterLocation.Query && p.Name == "limit"))
        { parameter.Schema.Minimum = 1; parameter.Schema.Maximum = 100; parameter.Schema.Default = new OpenApiInteger(20); }
        if (context.MethodInfo.Name == nameof(CaseRoutingController.Decide))
            operation.Responses["200"].Headers["ETag"] = new OpenApiHeader
            {
                Schema = new OpenApiSchema { Type = "string" },
                Description = "Quoted Case version after this operation; GET Case to reconcile current state after replay."
            };
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "auth_unauthorized: a current session is required." });
        operation.Responses.TryAdd("503", new OpenApiResponse { Description = "Dependency unavailable; never interpreted as an empty routing result." });
        if (context.MethodInfo.Name == nameof(CaseRoutingController.Map))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-None-Match",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description = "Quoted page snapshot ETag for the identical filters/cursor. Current authority is checked before 304."
            });
            operation.Responses.TryAdd("304", new OpenApiResponse { Description = "Authorized page snapshot is unchanged." });
            foreach (var status in new[] { "200", "304" })
                operation.Responses[status].Headers["ETag"] = new OpenApiHeader
                {
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Quoted snapshotVersion for this page and filters."
                };
        }
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CaseRoutingDecisionDto))
            Values("outcome", "NEEDS_LOCATION_EVIDENCE", "OUTSIDE_COMPANY_SCOPE");
        if (context.Type == typeof(RoutingDispositionView)) Values("state", "EFFECTIVE", "REVIEW_REQUIRED");
        if (context.Type == typeof(RoutingDecisionView)) Values("state", "EFFECTIVE", "REVIEW_REQUIRED", "SUPERSEDED");
        if (context.Type == typeof(RoutingWholeCaseAttestation))
        {
            Values("temporalBasis", "SERVER_RECEIVED_AT");
            schema.Properties["reports"].MinItems = 1;
            schema.Properties["reports"].MaxItems = 100;
        }
        if (context.Type == typeof(RoutingReportAttestation))
        {
            schema.Properties["sourceReferences"].MinItems = 1;
            schema.Properties["sourceReferences"].MaxItems = 20;
        }
        if (context.Type == typeof(RoutingSourceReference)) Values("kind", "ROUTE_VERSION", "COVERAGE_MAPPING");
        void Values(string property, params string[] values)
        {
            if (schema.Properties.TryGetValue(property, out var field))
                field.Enum = values.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();
        }
    }
}
