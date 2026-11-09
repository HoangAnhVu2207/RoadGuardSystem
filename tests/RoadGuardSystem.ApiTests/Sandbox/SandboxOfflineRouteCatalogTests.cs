using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.API.Extensions;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

// Reflection only: no collection fixture, WebApplicationFactory, Program startup or network.
public sealed class SandboxOfflineRouteCatalogTests
{
    [Fact]
    public void LedgerExactlyMatchesCurrentControllerMethodRoutes()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "deploy", "fe-handoff", "FE-API-CATALOG.json")));
        var expected = json.RootElement.GetProperty("operations").EnumerateArray().Select(row =>
            (row.GetProperty("method").GetString()!.ToUpperInvariant(), row.GetProperty("path").GetString()!)).ToHashSet();
        var actual = new HashSet<(string, string)>();
        // HandoffBootstrap conditionally maps this operational minimal endpoint.
        var bootstrap = File.ReadAllText(Path.Combine(root.FullName, "RoadGuardSystem.API", "Extensions", "HandoffBootstrap.cs"));
        Assert.Contains("app.MapGet(\"/ready\"", bootstrap);
        Assert.NotNull(typeof(HandoffBootstrap).GetMethod(nameof(HandoffBootstrap.MapReadiness)));
        actual.Add(("GET", "/ready"));
        foreach (var controller in typeof(ProjectsController).Assembly.GetTypes()
                     .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t)))
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                foreach (var verb in action.GetCustomAttributes<HttpMethodAttribute>())
                    foreach (var prefix in controller.GetCustomAttributes<RouteAttribute>().Select(a => a.Template).DefaultIfEmpty(""))
                        foreach (var method in verb.HttpMethods)
                        {
                            var template = verb.Template ?? "";
                            var combined = template.StartsWith('/') || template.StartsWith("~/", StringComparison.Ordinal)
                                ? template.TrimStart('~', '/') : prefix.TrimEnd('/') + "/" + template;
                            var route = "/" + combined.Trim('/').Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal);
                            route = Regex.Replace(route, @"\{(\w+):[^}]+\}", "{$1}");
                            if (route.StartsWith("/api/v1/", StringComparison.Ordinal))
                                Assert.True(actual.Add((method, route)), $"Duplicate route: {method} {route} ({controller.Name}.{action.Name})");
                        }
        Assert.Equal(260, expected.Count);
        // These newer routes are source-verified; the 260-operation FE handoff is a historical snapshot.
        var additions = new HashSet<(string, string)>
        {
            ("GET", "/api/v1/projects/{projectId}/members/eligible"),
            ("POST", "/api/v1/projects/{projectId}/members"),
            ("PATCH", "/api/v1/users/{userId}/phone-number"),
            ("GET", "/api/v1/me/phone-number"),
            ("PATCH", "/api/v1/me/phone-number"),
            ("GET", "/api/v1/survey-tasks/{taskId}/eligible-drones"),
            ("GET", "/api/v1/cases/{caseId}/reports"),
            ("GET", "/api/v1/cases/{caseId}/reports/{reportId}/evidence/{evidenceId}/content"),
            ("POST", "/api/v1/reporter-evidence/uploads/{uploadId}/cancel")
        };
        Assert.Empty(expected.Except(actual));
        Assert.Empty(additions.Except(actual));
        Assert.Empty(actual.Except(expected).Except(additions));
    }
}
