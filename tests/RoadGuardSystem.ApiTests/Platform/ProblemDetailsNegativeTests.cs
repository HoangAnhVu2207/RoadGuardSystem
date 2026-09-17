using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.ApiTests.Controllers;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Platform;

[Trait("TaskId", "P1-01")]
public sealed class ProblemDetailsNegativeTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProblemDetailsNegativeTests()
    {
        _factory = new CustomWebApplicationFactory("Development");
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact(DisplayName = "Malformed JSON request returns 400 problem+json with validation_error code and no exception details leak")]
    public async Task MalformedJson_Returns400_ProblemDetails_WithValidationError_AndNoLeak()
    {
        // ARRANGE
        const string malformedJson = "{ \"name\": \"test\", invalid_json_syntax: ";
        using var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // ACT
        var response = await _client.PostAsync("/api/v1/probe/validate", content);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "malformed JSON syntax must result in HTTP 400 Bad Request");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json",
            because: "error responses must strictly follow RFC 7807/9110 ProblemDetails specification");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.ValidationError,
            because: "validation failures must have the exact literal code validation_error");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        Guid.TryParse(correlationElement.GetString(), out _).Should().BeTrue(
            because: "correlationId must be a valid UUID string");

        // Security invariants: no stack traces, no internal exception types, no machine paths
        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("system.text.json");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\", because: "local machine drive paths must not leak");
        rawBody.Should().NotContain("at RoadGuardSystem", because: "stack traces must not leak");
    }

    [Fact(DisplayName = "Invalid model payload returns 400 problem+json with validation_error and correlationId")]
    public async Task InvalidModelPayload_Returns400_ProblemDetails_WithValidationError()
    {
        // ARRANGE - name is too short (min 3) and score is out of range (1-100)
        var invalidPayload = new { name = "x", score = 999 };
        var json = JsonSerializer.Serialize(invalidPayload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // ACT
        var response = await _client.PostAsync("/api/v1/probe/validate", content);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.ValidationError);

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        Guid.TryParse(correlationElement.GetString(), out _).Should().BeTrue();

        root.TryGetProperty("errors", out var errorsElement).Should().BeTrue();
        errorsElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact(DisplayName = "Unhandled exception returns 500 problem+json with internal_error and generic message (no leak)")]
    public async Task UnhandledException_Returns500_ProblemDetails_WithInternalError_AndNoLeak()
    {
        // ACT
        var response = await _client.GetAsync("/api/v1/probe/throw");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError,
            because: "unhandled exceptions must produce HTTP 500 Internal Server Error");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.InternalError,
            because: "server internal errors must have the exact literal code internal_error");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        Guid.TryParse(correlationElement.GetString(), out _).Should().BeTrue();

        // Security check: ensure simulated secret, exception type, and stack trace do not leak
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage,
            because: "sensitive internal exception data must never be exposed to clients");
        rawBody.Should().NotContain("InvalidOperationException");
        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.Should().NotContain(":\\", because: "machine file paths must never leak");
        rawBody.Should().NotContain("at RoadGuardSystem", because: "stack traces must never leak");
    }

    [Fact(DisplayName = "Non-existent route returns 404 problem+json with not_found and correlationId")]
    public async Task NonExistentRoute_Returns404_ProblemDetails_WithNotFound()
    {
        // ACT
        var response = await _client.GetAsync("/api/v1/non-existent-endpoint-xyz");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.NotFound,
            because: "missing resource must have the exact literal code not_found");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        Guid.TryParse(correlationElement.GetString(), out _).Should().BeTrue();
    }

    [Fact(DisplayName = "Unsupported numeric API version with subpath /api/v99.0/probe/ok returns 400 problem+json with unsupported_api_version")]
    public async Task UnsupportedApiVersion_WithSubpath_Returns400_ProblemDetails_WithUnsupportedApiVersion()
    {
        // ACT - request nonexistent numeric API version 99.0 with subpath
        var response = await _client.GetAsync("/api/v99.0/probe/ok");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "unsupported numeric API version must be rejected cleanly with 400 Bad Request and not 404/500");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.UnsupportedApiVersion,
            because: "unsupported API version error must have code unsupported_api_version");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Fact(DisplayName = "Unsupported numeric API version without trailing slash /api/v99.0 returns 400 problem+json with unsupported_api_version")]
    public async Task UnsupportedApiVersion_ExactWithoutTrailingSlash_Returns400_ProblemDetails_WithUnsupportedApiVersion()
    {
        // ACT - request nonexistent numeric API version 99.0 without trailing slash
        var response = await _client.GetAsync("/api/v99.0");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "exact unsupported numeric version /api/v99.0 must be rejected with 400 Bad Request");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.UnsupportedApiVersion);

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Fact(DisplayName = "Unsupported numeric API version with trailing slash /api/v99.0/ returns identical 400 problem+json contract")]
    public async Task UnsupportedApiVersion_ExactWithTrailingSlash_Returns400_ProblemDetails_WithUnsupportedApiVersion()
    {
        // ACT - request nonexistent numeric API version 99.0 with trailing slash
        var response = await _client.GetAsync("/api/v99.0/");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "exact unsupported numeric version with trailing slash /api/v99.0/ must return identical 400 Bad Request contract");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.UnsupportedApiVersion);

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Fact(DisplayName = "Non-numeric malformed version prefix such as /api/vabc/probe/ok bypasses version middleware and returns 404 not_found")]
    public async Task NonNumericMalformedVersion_SuchAsVabc_BypassesVersionMiddleware_Returns404NotFound()
    {
        // ACT - non-numeric prefix does not match numeric version namespace
        var response = await _client.GetAsync("/api/vabc/probe/ok");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "non-numeric malformed version prefixes must bypass version middleware and return 404 Not Found");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.NotFound,
            because: "bypassed non-numeric routes must return code not_found");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Fact(DisplayName = "Empty version prefix such as /api/v/probe/ok bypasses version middleware and returns 404 not_found")]
    public async Task EmptyVersionPrefix_SuchAsV_BypassesVersionMiddleware_Returns404NotFound()
    {
        // ACT - empty prefix '/v/' does not match numeric version namespace
        var response = await _client.GetAsync("/api/v/probe/ok");
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "empty version prefix /api/v/ must bypass version middleware and return 404 Not Found");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.NotFound);

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Theory(DisplayName = "Boundary non-numeric routes such as /api/v1beta and /api/v1. bypass version middleware and return 404 not_found")]
    [InlineData("/api/v1beta/probe/ok")]
    [InlineData("/api/v1./probe/ok")]
    [InlineData("/api/v1beta")]
    [InlineData("/api/v1.")]
    public async Task BoundaryMalformedVersions_BypassVersionMiddleware_Return404NotFound(string boundaryPath)
    {
        // ACT
        var response = await _client.GetAsync(boundaryPath);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: $"boundary path {boundaryPath} does not match numeric version pattern and must return 404 Not Found");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.NotFound);

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }

    [Theory(DisplayName = "Unrelated paths such as /api/videos and /api/videos/123 bypass version middleware and return 404 not_found")]
    [InlineData("/api/videos")]
    [InlineData("/api/videos/123")]
    public async Task UnrelatedPaths_SuchAsVideos_BypassVersionMiddleware_Return404NotFound(string unrelatedPath)
    {
        // ACT - unrelated paths starting with /api/v... that are not numeric version routes
        var response = await _client.GetAsync(unrelatedPath);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: $"unrelated path {unrelatedPath} must not be intercepted by version middleware and must return 404 Not Found");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.NotFound,
            because: "unrelated non-existent paths must return code not_found and never unsupported_api_version");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId);

        rawBody.ToLowerInvariant().Should().NotContain("stacktrace");
        rawBody.ToLowerInvariant().Should().NotContain("exception");
        rawBody.Should().NotContain(":\\");
        rawBody.Should().NotContain("at RoadGuardSystem");
        rawBody.Should().NotContain(ProbeController.ExceptionSecretMessage);
    }


    [Fact(DisplayName = "Invalid correlation header is not reflected raw; server issues new valid UUID")]
    public async Task InvalidCorrelationHeader_IsNotReflectedRaw_ServerIssuesNewValidUuid()
    {
        // ARRANGE
        const string invalidCorrelationId = "INVALID_CORRELATION_ID_NOT_A_UUID_<script>alert('x')</script>";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/probe/ok");
        request.Headers.Add("X-Correlation-ID", invalidCorrelationId);

        // ACT
        var response = await _client.SendAsync(request);

        // ASSERT
        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        var returnedId = responseHeaders!.Single();

        returnedId.Should().NotBe(invalidCorrelationId,
            because: "malformed client correlation IDs must not be reflected raw back to clients");

        Guid.TryParse(returnedId, out _).Should().BeTrue(
            because: "server must replace malformed correlation IDs with a valid UUID");
    }

    [Fact(DisplayName = "Multiple correlation headers are not reflected raw; server issues new valid UUID")]
    public async Task MultipleCorrelationHeaders_AreNotReflectedRaw_ServerIssuesNewValidUuid()
    {
        // ARRANGE
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/probe/ok");
        request.Headers.Add("X-Correlation-ID", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() });

        // ACT
        var response = await _client.SendAsync(request);

        // ASSERT
        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        var returnedId = responseHeaders!.Single();

        returnedId.Should().NotContain(",",
            because: "multiple correlation headers must be collapsed or replaced with a single UUID");

        Guid.TryParse(returnedId, out _).Should().BeTrue(
            because: "server must emit a single valid UUID");
    }

    [Fact(DisplayName = "POST to GET-only endpoint returns 405 problem+json with method_not_allowed and matching correlationId")]
    public async Task PostToGetOnlyEndpoint_Returns405_ProblemDetails_WithMethodNotAllowed()
    {
        // ARRANGE - POST JSON to GET-only /api/v1/probe/ok
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // ACT
        var response = await _client.PostAsync("/api/v1/probe/ok", content);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.MethodNotAllowed,
            because: "HTTP 405 errors must have the exact literal code method_not_allowed");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId,
            because: "X-Correlation-ID header must match the body correlationId");
    }

    [Fact(DisplayName = "POST text/plain to JSON-only endpoint returns 415 problem+json with unsupported_media_type and matching correlationId")]
    public async Task PostTextPlainToJsonEndpoint_Returns415_ProblemDetails_WithUnsupportedMediaType()
    {
        // ARRANGE - POST text/plain to JSON-expecting /api/v1/probe/validate
        using var content = new StringContent("invalid text plain body", Encoding.UTF8, "text/plain");

        // ACT
        var response = await _client.PostAsync("/api/v1/probe/validate", content);
        var rawBody = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var jsonDoc = JsonDocument.Parse(rawBody);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("code", out var codeElement).Should().BeTrue();
        codeElement.GetString().Should().Be(ApiErrorCodes.UnsupportedMediaType,
            because: "HTTP 415 errors must have the exact literal code unsupported_media_type");

        root.TryGetProperty("correlationId", out var correlationElement).Should().BeTrue();
        var bodyCorrelationId = correlationElement.GetString();
        Guid.TryParse(bodyCorrelationId, out _).Should().BeTrue();

        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        responseHeaders!.Single().Should().Be(bodyCorrelationId,
            because: "X-Correlation-ID header must match the body correlationId");
    }
}
