using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Startup;

/// <summary>
/// API startup tests using WebApplicationFactory&lt;Program&gt;.
///
/// Purpose (P1-00): confirm the application's DI pipeline and startup pipeline
/// can be built without a real database or external service. No business
/// endpoint behavior is tested here.
///
/// F8 scope decision (P1-00): Program.cs bootstraps AddSwaggerGen/UseSwagger because
/// Swashbuckle.AspNetCore was already present in the API project before P1-00, and a
/// minimal working startup was required to fix CS5001. This bootstrap is treated as part
/// of the P1-00 "solution builds" contract, NOT as the P1-01 OpenAPI feature scope.
/// P1-01 will add versioning, ProblemDetails, stable error codes, and finalize the
/// OpenAPI document configuration on top of this foundation.
/// The Swagger smoke test below validates the foundation (middleware pipeline runs) —
/// it does not validate the OpenAPI document schema or versioning contract.
/// </summary>
[Trait("TaskId", "P1-00")]
public sealed class ApiStartupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiStartupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "WebApplicationFactory creates HTTP client without throwing during startup")]
    public void WebApplicationFactory_CreatesHttpClient_WithoutStartupException()
    {
        // ACT — creating the client triggers the full DI/startup pipeline
        var act = () => _factory.CreateClient();

        // ASSERT — the factory must not throw during application startup
        act.Should().NotThrow(
            because: "the application's startup pipeline must be buildable without an external database " +
                     "or other external service at P1-00 stage (minimal Program.cs with no DbContext)");
    }

    [Fact(DisplayName = "Swagger UI foundation endpoint responds 200 OK in Development (F8: bootstrap smoke)")]
    public async Task SwaggerUI_Foundation_Returns200_InDevelopment()
    {
        // F8 note: this test validates the middleware pipeline can serve the Swagger foundation
        // that was bootstrapped as part of the minimal Program.cs entry-point fix in P1-00.
        // It does NOT validate the P1-01 OpenAPI document contract, versioning, or schema.

        // ARRANGE — configure the factory to use Development environment so Swagger middleware is active
        var devFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        using var client = devFactory.CreateClient();

        // ACT
        var response = await client.GetAsync("/swagger/index.html");

        // ASSERT
        ((int)response.StatusCode).Should().Be(200,
            because: "the startup pipeline (including Swagger foundation) must serve the UI page " +
                     "to confirm middleware is wired correctly — P1-01 will extend this contract");
    }
}
