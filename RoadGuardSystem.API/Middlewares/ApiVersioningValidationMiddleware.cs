using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;

namespace RoadGuardSystem.API.Middlewares;

/// <summary>
/// Middleware that validates requested API version numbers in URL segment routes (/api/v{version}/...).
/// If an unsupported version is requested, rejects with HTTP 400 Bad Request and
/// a standard ProblemDetails envelope (code: unsupported_api_version, correlationId).
/// </summary>
public class ApiVersioningValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersioningValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApiVersionDescriptionProvider provider)
    {
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path) && path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
        {
            var slashIndex = path.IndexOf('/', 6);
            var versionSegment = slashIndex > 6 ? path.Substring(6, slashIndex - 6) : path.Substring(6);

            if (ApiVersionParser.Default.TryParse(versionSegment, out var requestedVersion))
            {
                var isSupported = provider.ApiVersionDescriptions.Any(d => d.ApiVersion == requestedVersion);
                if (!isSupported)
                {
                    var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string
                        ?? context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].ToString();

                    if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out _))
                    {
                        correlationId = Guid.NewGuid().ToString();
                        context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
                        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;
                    }

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Unsupported API Version",
                        Detail = $"The requested API version '{versionSegment}' is not supported.",
                        Instance = context.Request.Path,
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                    };

                    problemDetails.Extensions["code"] = ApiErrorCodes.UnsupportedApiVersion;
                    problemDetails.Extensions["correlationId"] = correlationId;

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/problem+json";
                    var json = System.Text.Json.JsonSerializer.Serialize(problemDetails);
                    await context.Response.WriteAsync(json);
                    return;
                }
            }
        }

        await _next(context);
    }
}
