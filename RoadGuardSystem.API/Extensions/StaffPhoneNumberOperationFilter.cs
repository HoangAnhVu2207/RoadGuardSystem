using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using RoadGuardSystem.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class StaffPhoneNumberOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(UsersController) ||
            context.MethodInfo.Name != nameof(UsersController.UpdatePhoneNumber)) return;

        operation.Description = "Supervisor sets or changes the phone number of an active PM, Drone Operator, or Repair Crew account. Get the current ETag from GET /api/v1/users/{userId}. Changing the number clears phone confirmation; this route does not verify a number. Browser cookie requests require a valid CSRF token.";
        operation.Parameters ??= new List<OpenApiParameter>();
        Header("If-Match", "Current quoted base64 user rowversion from GET /users/{userId}.");
        Header("Idempotency-Key", "Nonblank request key, at most 200 characters. Reuse only with the same target, phone number, and version.");
        operation.RequestBody!.Required = true;
        operation.Responses["204"] = new OpenApiResponse { Description = "Phone number saved, or the same request replayed." };
        foreach (var (status, description) in new[]
        {
            ("400", "validation_error: invalid phone number, key, ETag, or JSON body."),
            ("401", "Missing or invalid credential or session."),
            ("403", "access_forbidden: caller is not a current Supervisor; csrf_failed for invalid browser CSRF."),
            ("404", "identity_user_not_found: target is not an active PM, Operator, or Crew account."),
            ("409", "idempotency_key_reused: the key was used for a different request."),
            ("412", "auth_concurrency_conflict: the user version changed."),
            ("428", "validation_error: If-Match is required.")
        })
            operation.Responses[status] = new OpenApiResponse
            {
                Description = description,
                Content = { ["application/problem+json"] = new OpenApiMediaType
                    { Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository) } }
            };
        foreach (var response in operation.Responses.Values)
            response.Headers["X-Correlation-ID"] = new OpenApiHeader
                { Description = "Request correlation UUID.", Schema = new OpenApiSchema { Type = "string", Format = "uuid" } };

        void Header(string name, string description)
        {
            var header = operation.Parameters.FirstOrDefault(x => x.In == ParameterLocation.Header && x.Name == name);
            if (header is null)
            {
                header = new OpenApiParameter { Name = name, In = ParameterLocation.Header };
                operation.Parameters.Add(header);
            }
            header.Required = true;
            header.Description = description;
            header.Schema = new OpenApiSchema { Type = "string", MinLength = 1,
                MaxLength = name == "Idempotency-Key" ? 200 : null };
        }
    }
}
