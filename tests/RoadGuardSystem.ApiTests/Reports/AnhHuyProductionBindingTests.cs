using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AnhHuyProductionBindingTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ExplicitOptIn_BindsScopedProductionReadersOnce_AndUsesCurrentReporterAuthority()
    {
        var reporter = await sql.CreateUserAsync($"optin-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        ServiceDescriptor[] descriptors = [];
        await using var original = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => descriptors = services.ToArray());
        await using var factory = original.WithWebHostBuilder(builder => builder.UseSetting("Huy01:EnableLifecycleAndCase", "true"));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(reporter.UserName!), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("accessToken").GetString());
        var list = await client.GetAsync("/api/v1/reports");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/cases")).StatusCode);
        foreach (var type in new[] {
            typeof(RoadGuardSystem.Services.Reports.IReporterLifecycleService),
            typeof(RoadGuardSystem.Services.Cases.ICaseWorkflowService),
            typeof(RoadGuardSystem.Services.Defects.ICandidateDecisionService),
            typeof(RoadGuardSystem.Repositories.Reports.IReporterLifecycleRepository),
            typeof(RoadGuardSystem.Repositories.Cases.ICaseWorkflowRepository),
            typeof(RoadGuardSystem.Repositories.Defects.ICandidateDecisionRepository) })
        {
            Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == type)).Lifetime);
            using var scope = factory.Services.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetRequiredService(type));
        }
        Assert.DoesNotContain(descriptors, d => d.ServiceType == typeof(RoadGuardSystem.Services.Integration.ICaseDefectReadReader));
    }
}
