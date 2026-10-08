using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

/// <summary>Explicit, source-audited documentation only. Never participates in request handling.</summary>
public sealed class ManualContractOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, JsonElement> Contracts = Load();

    private static Dictionary<string, JsonElement> Load()
    {
        using var stream = typeof(ManualContractOperationFilter).Assembly.GetManifestResourceStream("RoadGuardSystem.API.ManualSwaggerContracts.json")
            ?? throw new InvalidOperationException("Missing audited Swagger contracts.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.EnumerateArray().ToDictionary(
            x => x.GetProperty("method").GetString() + " " + x.GetProperty("route").GetString(), x => x.Clone(), StringComparer.Ordinal);
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var key = context.ApiDescription.HttpMethod + " /" + context.ApiDescription.RelativePath?.Split('?')[0];
        if (!Contracts.TryGetValue(key, out var contract)) return;
        operation.Extensions["x-roadguard-api-id"] = new OpenApiString(contract.GetProperty("id").GetString());
        operation.Extensions["x-contract-evidence"] = new OpenApiString("CURRENT_VERIFIED_SOURCE; execution and business prerequisites are separate.");
        operation.Description = Text(contract, "uiDescription");
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional UUID for request tracing. The server generates one when omitted or invalid.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
        foreach (var header in contract.GetProperty("headers").EnumerateArray())
        {
            var name = header.GetProperty("name").GetString()!;
            var parameter = operation.Parameters.FirstOrDefault(x => x.In == ParameterLocation.Header && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (parameter is null) { parameter = new OpenApiParameter { Name = name, In = ParameterLocation.Header }; operation.Parameters.Add(parameter); }
            parameter.Required = header.GetProperty("required").GetBoolean();
            parameter.Description = Text(header, "uiDescription");
            parameter.Schema = new OpenApiSchema { Type = "string", MinLength = Number(header, "minLength"), MaxLength = Number(header, "maxLength"), Pattern = TextOrNull(header, "pattern") };
            if (TextOrNull(header, "example") is { } example) parameter.Example = new OpenApiString(example);
            parameter.Extensions["x-requirement"] = new OpenApiString(Text(header, "requirement"));
        }
        foreach (var rule in ManualContractSchemaFilter.Metadata.GetProperty("queryParameters").EnumerateArray())
            if (rule.GetProperty("ids").EnumerateArray().Any(x => x.GetString() == contract.GetProperty("id").GetString()))
            {
                var parameter = operation.Parameters.Single(x => x.Name == rule.GetProperty("name").GetString() && x.In == ParameterLocation.Query);
                ManualContractSchemaFilter.Constraints(parameter.Schema, rule);
                if (rule.TryGetProperty("description", out var description)) parameter.Description = description.GetString();
            }
        if (operation.RequestBody is not null)
        {
            operation.RequestBody.Required = contract.GetProperty("bodyRequired").GetBoolean();
            operation.RequestBody.Description = "Required JSON body.";
        }
        // Replace inferred IActionResult success placeholders with the action's actual response mapping.
        foreach (var status in operation.Responses.Keys.Where(x => x.Length == 3 && x[0] == '2').ToArray()) operation.Responses.Remove(status);
        foreach (var success in contract.GetProperty("successResponses").EnumerateArray())
        {
            var response = new OpenApiResponse { Description = "Success." };
            var typeName = TextOrNull(success, "type");
            var schema = success.TryGetProperty("schema", out var shape) ? ManualContractSchemaFilter.Shape(shape, context) : ResponseSchema(typeName, context);
            foreach (var media in success.GetProperty("mediaTypes").EnumerateArray())
                response.Content[media.GetString()!] = new OpenApiMediaType { Schema = schema };
            foreach (var header in success.GetProperty("headers").EnumerateArray())
                response.Headers[header.GetProperty("name").GetString()!] = new OpenApiHeader { Description = Text(header, "uiDescription"), Schema = new OpenApiSchema { Type = "string", Format = TextOrNull(header, "format"), Pattern = TextOrNull(header, "pattern") } };
            operation.Responses[success.GetProperty("status").ToString()] = response;
        }
        foreach (var error in contract.GetProperty("errors").EnumerateArray())
        {
            var codes = Text(error, "codes");
            operation.Responses[error.GetProperty("status").ToString()] = new OpenApiResponse
            {
                Description = codes.Length > 0 ? "Error codes: " + codes.Replace("\n", ", ", StringComparison.Ordinal) + "." : "Request rejected.",
                Content = { ["application/problem+json"] = new OpenApiMediaType { Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository) } }
            };
        }
        AddPlatformResponse(operation, context, "500", "Unexpected exception; generic internal_error ProblemDetails. No internal implementation details are returned.");
        if (operation.RequestBody is not null)
        {
            AddPlatformResponse(operation, context, "400", "Malformed or DTO-invalid JSON: validation_error.");
            AddPlatformResponse(operation, context, "415", "Unsupported request media type: unsupported_media_type.");
        }
        if (operation.Security.Any(x => x.Keys.Any(k => k.Reference?.Id is "Bearer" or "AiServiceBearer" or "WebSession")))
        {
            AddPlatformResponse(operation, context, "401", "Authentication challenge: missing, expired or invalid credential/session.");
            AddPlatformResponse(operation, context, "403", "Authorization policy or current authority denies this request.");
        }
        if (operation.Parameters.Any(x => x.Name == "X-CSRF-TOKEN"))
        {
            AddPlatformResponse(operation, context, "403", "Cookie unsafe request with missing/invalid antiforgery pair: csrf_failed.");
            operation.Responses["403"].Description += " Cookie unsafe requests also return csrf_failed when the antiforgery pair is invalid.";
        }
        foreach (var response in operation.Responses.Values)
            response.Headers["X-Correlation-ID"] = new OpenApiHeader { Description = "Request correlation UUID, emitted by middleware on all responses.", Schema = new OpenApiSchema { Type = "string", Format = "uuid" } };
    }

    private static void AddPlatformResponse(OpenApiOperation operation, OperationFilterContext context, string status, string description)
    {
        if (!operation.Responses.ContainsKey(status))
            operation.Responses[status] = new OpenApiResponse
            {
                Description = description,
                Content = { ["application/problem+json"] = new OpenApiMediaType { Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository) } }
            };
    }

    private static OpenApiSchema ResponseSchema(string? name, OperationFilterContext context)
    {
        if (name is null) return new OpenApiSchema { Type = "object", Description = "See the documented source response shape." };
        if (name.StartsWith("binary", StringComparison.Ordinal)) return new OpenApiSchema { Type = "string", Format = "binary" };
        if (name.StartsWith("anonymous object", StringComparison.Ordinal)) return new OpenApiSchema { Type = "object", Description = name };
        var array = name.EndsWith("[]", StringComparison.Ordinal);
        var elementName = array ? name[..^2] : name;
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(elementName)).FirstOrDefault(x => x is not null)
            ?? throw new InvalidOperationException("Audited Swagger response type is unavailable: " + name);
        return context.SchemaGenerator.GenerateSchema(array ? type.MakeArrayType() : type, context.SchemaRepository);
    }

    private static int? Number(JsonElement value, string key) => value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.Number ? item.GetInt32() : null;
    private static string? TextOrNull(JsonElement value, string key) => value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    private static string Text(JsonElement value, string key) => value.TryGetProperty(key, out var item) ? Render(item) : "";
    private static string Render(JsonElement item) => item.ValueKind switch
    {
        JsonValueKind.Array => string.Join("\n", item.EnumerateArray().Select(Render)),
        JsonValueKind.Object => string.Join("; ", item.EnumerateObject().Select(x => x.Name + ": " + Render(x.Value))),
        JsonValueKind.String => item.GetString() ?? "",
        JsonValueKind.Null => "",
        _ => item.ToString()
    };
}
