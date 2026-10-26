using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class NotificationSupervisorOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(NotificationSupervisorController)) return;
        var method = context.ApiDescription.HttpMethod;
        operation.Description = "Current project PM manages one notification-only FastTrack Supervisor assignment. Supervisor authority is global; no ProjectMember is required for the recipient. No approval duty is granted. Current authority/resource checks precede envelope validation and historical replay.";
        if (method != "GET")
        {
            operation.Parameters.Add(new() { Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true, Schema = new() { Type = "string", MinLength = 1, MaxLength = 200 } });
            operation.Parameters.Add(new() { Name = "If-Match", In = ParameterLocation.Header, Required = method == "DELETE", Description = "Quoted base64 eight-byte head rowversion; required for replacement/revocation.", Schema = new() { Type = "string" } });
            if (method == "PUT")
            {
                operation.Parameters.Add(new() { Name = "If-None-Match", In = ParameterLocation.Header, Description = "Exactly * for initial create; mutually exclusive with If-Match. One conditional header is required.", Schema = new() { Type = "string", Enum = [new Microsoft.OpenApi.Any.OpenApiString("*")] } });
                operation.RequestBody = new() { Required = true, Content = { ["application/json"] = new() { Schema = new() { Type = "object", AdditionalPropertiesAllowed = false, Required = new HashSet<string> { "supervisorId" }, Properties = { ["supervisorId"] = new() { Type = "string", Format = "uuid" } } } } } };
            }
        }
        foreach (var (status, description) in new[] {
            ("400", "validation_error: malformed/duplicate/unknown body fields, invalid key/ETag or both conditionals."),
            ("401", "Existing invalid or revoked authentication/session behavior."),
            ("403", "access_forbidden or existing csrf_failed for cookie writes."),
            ("404", "not_found: concealed missing/mismatched resource, absent head or invalid recipient."),
            ("409", "invalid_state_transition, notification_assignment_revoked, or idempotency_key_reused."),
            ("412", "precondition_failed: head version mismatch or initial create against existing head."),
            ("428", "precondition_required: missing conditional header.") })
        {
            if (method == "GET" && status is "400" or "409" or "412" or "428") continue;
            operation.Responses[status] = new() { Description = description, Content = { ["application/problem+json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository) } } };
        }
        foreach (var response in operation.Responses.Values)
            response.Headers["Cache-Control"] = new() { Description = "private, no-store", Schema = new() { Type = "string" } };
        foreach (var response in operation.Responses.Where(x => x.Key.StartsWith('2')).Select(x => x.Value))
            response.Headers["ETag"] = new() { Description = "Quoted base64 SQL head rowversion, including historical command replay.", Schema = new() { Type = "string" } };
        if (operation.Responses.TryGetValue("201", out var created))
            created.Headers["Location"] = new() { Description = "Same notification-supervisor resource path.", Schema = new() { Type = "string" } };
    }
}
