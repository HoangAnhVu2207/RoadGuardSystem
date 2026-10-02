using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RoadGuardSystem.API.Middlewares;

/// <summary>
/// Correlation ID middleware. Inspects incoming request for X-Correlation-ID header.
/// If present and valid UUID, preserves and echoes it; otherwise generates a new standard UUID.
/// Propagates correlation ID to response header, HttpContext items, and structured logger scope.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[CorrelationIdItemKey] = correlationId;
        context.TraceIdentifier = correlationId;

        // Set response header eagerly so error handlers and early aborts include it
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Ensure header is written even if downstream flushes response stream
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Structured logging scope without leaking body, headers, or secrets
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdItemKey] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var values))
        {
            // Only accept a single header value that is a valid UUID/GUID
            if (values.Count == 1 && Guid.TryParse(values[0], out var validGuid))
            {
                return validGuid.ToString();
            }
        }

        return Guid.NewGuid().ToString();
    }
}
