using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using RoadGuardSystem.API.Authentication;
using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var path = "/" + context.ApiDescription.RelativePath?.Split('?')[0];
        var webLogin = path == "/api/v1/auth/web/login";
        var webRenew = path == "/api/v1/auth/web/renew";
        var webLogout = path == "/api/v1/auth/web/logout";
        if (webLogin || webRenew)
        {
            operation.Security = [Requirement(webRenew ? ["WebRenewal", "CsrfToken", "AntiforgeryCookie"] : ["CsrfToken", "AntiforgeryCookie"])];
            AddCsrf(operation, true);
            operation.Description = webRenew ? "Renew the browser session using its renewal cookie and CSRF token. No body or Authorization header." : "Sign in to a browser session. Supply the CSRF token from the bootstrap endpoint.";
            return;
        }
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        var authorizations = metadata.OfType<IAuthorizeData>().ToArray();
        if (authorizations.Any(x => x.Policy == "AiCallback"))
        {
            operation.Security = [Requirement("AiServiceBearer")];
            operation.Description = "Submit a processing result. Authentication: AI service bearer token with AI_SERVICE role.";
            return;
        }
        if (authorizations.Any(x => x.AuthenticationSchemes == WebCookieConfiguration.Scheme))
        {
            operation.Security = [Requirement(webLogout ? ["WebSession", "CsrfToken", "AntiforgeryCookie"] : ["WebSession"])];
            if (webLogout) AddCsrf(operation, true);
            return;
        }
        operation.Security = [Requirement("Bearer")];
        var concretePath = Regex.Replace(path, "\\{[^}]+\\}", Guid.Empty.ToString());
        if (metadata.OfType<WebCookieEligibleAttribute>().Any() ||
            WebCookieConfiguration.IsCookieEligibleRequest(context.ApiDescription.HttpMethod ?? "", new PathString(concretePath)))
        {
            var unsafeMethod = !HttpMethods.IsGet(context.ApiDescription.HttpMethod ?? "") &&
                !HttpMethods.IsHead(context.ApiDescription.HttpMethod ?? "") && !HttpMethods.IsOptions(context.ApiDescription.HttpMethod ?? "");
            operation.Security.Add(Requirement(unsafeMethod ? ["WebSession", "CsrfToken", "AntiforgeryCookie"] : ["WebSession"]));
            if (unsafeMethod) AddCsrf(operation, false);
        }
    }

    private static OpenApiSecurityRequirement Requirement(params string[] names)
    {
        var result = new OpenApiSecurityRequirement();
        foreach (var name in names)
            result[new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = name } }] = Array.Empty<string>();
        return result;
    }

    private static void AddCsrf(OpenApiOperation operation, bool required)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter { Name = "X-CSRF-TOKEN", In = ParameterLocation.Header,
            Required = required, Description = "CSRF token from the bootstrap endpoint for this browser session. Required for cookie authentication on unsafe requests.",
            Schema = new OpenApiSchema { Type = "string" }, Extensions = { ["x-requirement"] = new OpenApiString(required ? "required" : "conditional: cookie authentication only") } });
    }
}
