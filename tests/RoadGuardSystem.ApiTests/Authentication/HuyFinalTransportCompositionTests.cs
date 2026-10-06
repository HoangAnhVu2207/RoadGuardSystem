using System.Reflection;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Retention;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class HuyFinalTransportCompositionTests
{
    [Theory]
    [InlineData("/api/v1/notifications")]
    [InlineData("/api/v1/me/inspection-tasks")]
    public async Task UnauthenticatedHttp_UsesProblemMediaType(string path)
    {
        await using var factory = new RoadGuardSystem.ApiTests.Infrastructure.CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("auth_unauthorized", body.GetProperty("code").GetString());
        Assert.Equal(401, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public void ProductionComposition_RegistersInspectionOnceAndPreservesHuy()
    {
        var services = new ServiceCollection();
        services.AddHuy01ReporterPersistence().AddHuy01ReporterPersistence();
        var contributors = services.Where(x => x.ServiceType == typeof(IRetentionInventoryContributor)).ToArray();
        Assert.Single(contributors, x => x.ImplementationType?.Name == "Huy02InspectionRetentionContributor");
        Assert.Single(contributors, x => x.ImplementationType?.Name == "PavementRetentionContributor");
        Assert.Single(contributors, x => x.ImplementationType?.Name == "Huy01RetentionInventoryContributor");
        Assert.Single(contributors, x => x.ImplementationType?.Name == "ExportRetentionInventoryContributor");
        Assert.Single(contributors, x => x.ImplementationType?.Name == "AiRetentionInventoryContributor");
        Assert.Single(contributors, x => x.ImplementationType?.Name == "ReporterIntakeRetentionInventoryContributor");
        Assert.All(contributors, x => Assert.Equal(ServiceLifetime.Scoped, x.Lifetime));
    }

    [Theory]
    [InlineData("GET", "/api/v1/me/inspection-tasks", true)]
    [InlineData("GET", "/api/v1/me/inspection-tasks/", true)]
    [InlineData("POST", "/api/v1/me/inspection-tasks", false)]
    [InlineData("GET", "/api/v1/me/inspection-tasks/extra", false)]
    [InlineData("GET", "/api/v1/notifications", true)]
    [InlineData("GET", "/api/v1/notifications/", true)]
    [InlineData("GET", "/api/v1/notifications/11111111-1111-1111-1111-111111111111/", true)]
    [InlineData("POST", "/api/v1/notifications/11111111-1111-1111-1111-111111111111/read/", true)]
    [InlineData("GET", "/api/v1/notifications/11111111-1111-1111-1111-111111111111/read", false)]
    [InlineData("POST", "/api/v1/notifications", false)]
    [InlineData("DELETE", "/api/v1/notifications/11111111-1111-1111-1111-111111111111", false)]
    [InlineData("POST", "/api/v1/notifications/not-a-guid/read", false)]
    [InlineData("GET", "/api/v1/notifications-extra", false)]
    [InlineData("GET", "/api/v1/notifications//", false)]
    public void CookieAdmission_IsLimitedToAssignedMethodsAndRoutes(string method, string path, bool eligible)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Cookie = "__Host-RoadGuardSession=test";
        Assert.Equal(eligible ? "RoadGuardWeb" : "Bearer", SelectScheme(context));
        context.Request.Headers.Authorization = "Bearer invalid";
        Assert.Equal("Bearer", SelectScheme(context));
    }

    private static string SelectScheme(HttpContext context) => (string)typeof(Program).Assembly
        .GetType("RoadGuardSystem.API.Authentication.WebCookieConfiguration", true)!
        .GetMethod("SelectScheme", BindingFlags.Static | BindingFlags.NonPublic)!
        .Invoke(null, [context])!;
}
