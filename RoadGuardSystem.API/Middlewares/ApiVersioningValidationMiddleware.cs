using System.Text.RegularExpressions;
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
/// Only numeric version segments matching ^/api/v\d+(\.\d+)?(/.*)?$ enter version validation.
/// </summary>
public partial class ApiVersioningValidationMiddleware
{
    private readonly RequestDelegate _next;

    [GeneratedRegex(@"^/api/v(?<version>\d+(?:\.\d+)?)(?:/.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VersionRouteRegex();

    public ApiVersioningValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApiVersionDescriptionProvider provider)
    {
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path))
        {
            var match = VersionRouteRegex().Match(path);
            if (match.Success)
            {
                var versionSegment = match.Groups["version"].Value;
                var isSupported = ApiVersionParser.Default.TryParse(versionSegment, out var requestedVersion)
                    && provider.ApiVersionDescriptions.Any(d => d.ApiVersion == requestedVersion);

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
