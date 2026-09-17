using System.Net;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Platform;

[Trait("TaskId", "P1-01")]
public sealed class ApiPlatformPositiveTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiPlatformPositiveTests()
    {
        _factory = new CustomWebApplicationFactory("Development");
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact(DisplayName = "WebApplicationFactory starts up successfully without external dependencies")]
    public void WebApplicationFactory_StartsSuccessfully()
    {
        var act = () => _factory.CreateClient();
        act.Should().NotThrow(because: "API factory must build and start without database or external dependencies");
    }

    [Fact(DisplayName = "GET /health responds 200 OK without requiring database or Docker")]
    public async Task HealthEndpoint_Returns200Ok_WithoutDatabase()
    {
        // ACT
        var response = await _client.GetAsync("/health");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "unversioned health check endpoint must report liveness without external dependencies");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "OpenAPI v1 specification endpoint responds 200 OK in Development")]
    public async Task OpenApiV1_Specification_Returns200Ok_InDevelopment()
    {
        // ACT
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "versioned OpenAPI document for v1 must be served in Development");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check OpenAPI root fields
        root.TryGetProperty("openapi", out _).Should().BeTrue();
        root.TryGetProperty("info", out var info).Should().BeTrue();
        info.TryGetProperty("version", out var versionElement).Should().BeTrue();
        versionElement.GetString().Should().Contain("1", because: "document is for v1");

        // Check paths exist and contain v1 routes
        root.TryGetProperty("paths", out var paths).Should().BeTrue();
        var pathsJson = paths.GetRawText();
        pathsJson.Should().Contain("/api/v1/",
            because: "v1 OpenAPI document must describe versioned /api/v1/ routes");
    }

    [Fact(DisplayName = "Request without correlation header is assigned a new valid UUID and routes successfully to v1")]
    public async Task RequestWithoutCorrelationHeader_ReceivesNewValidUuid()
    {
        // ACT
        var response = await _client.GetAsync("/api/v1/probe/ok");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "GET /api/v1/probe/ok must route successfully and return HTTP 200 OK for supported v1");

        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue(
            because: "server must attach X-Correlation-ID header to every response");

        var correlationId = values!.Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue(
            because: "server-generated correlation ID must be a valid UUID");
    }

    [Fact(DisplayName = "Valid client UUID in correlation header is preserved in response and matches ProblemDetails")]
    public async Task ValidClientCorrelationHeader_IsPreserved_AndMatchesProblemDetails()
    {
        // ARRANGE
        var clientUuid = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/non-existent-endpoint");
        request.Headers.Add("X-Correlation-ID", clientUuid);

        // ACT
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // ASSERT
        response.Headers.TryGetValues("X-Correlation-ID", out var responseHeaders).Should().BeTrue();
        var returnedHeaderId = responseHeaders!.Single();

        returnedHeaderId.Should().Be(clientUuid,
            because: "a valid client UUID in X-Correlation-ID must be preserved in response headers");

        using var jsonDoc = JsonDocument.Parse(body);
        var correlationInBody = jsonDoc.RootElement.GetProperty("correlationId").GetString();

        correlationInBody.Should().Be(clientUuid,
            because: "ProblemDetails correlationId extension must match the preserved client correlation ID");

        returnedHeaderId.Should().Be(correlationInBody,
            because: "response header and ProblemDetails correlationId must be strictly identical");
    }
}
