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
            ("GET", "/api/v1/projects/{projectId}/reports/{reportId}/matching-location"),
            ("POST", "/api/v1/projects/{projectId}/reports/{reportId}/matching-location"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-evidence/{evidenceId}/content"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/geometry"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/history"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/submissions/{submissionId}"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/evidence/{fileId}"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/evidence/{fileId}/content"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/temporary-safety-obligations"),
            ("POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/temporary-safety-obligations"),
            ("GET", "/api/v1/projects/overviews"),
            ("GET", "/api/v1/projects/{projectId}/member-contacts"),
            ("GET", "/api/v1/projects/{projectId}/task-assignees"),
            ("GET", "/api/v1/projects/{projectId}/repair-policies"),
            ("GET", "/api/v1/users/{userId}/organization"),
            ("PUT", "/api/v1/users/{userId}/organization"),
            ("PUT", "/api/v1/projects/{projectId}/locality"),
            ("POST", "/api/v1/notifications/read-batch"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/planning"),
            ("PUT", "/api/v1/projects/{projectId}/repair-packages/{packageId}/planning"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/planning"),
            ("PUT", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/planning"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-defect"),
            ("GET", "/api/v1/catalog/defect-types"),
            ("GET", "/api/v1/catalog/defect-types/admin"),
            ("POST", "/api/v1/catalog/defect-types/admin"),
            ("PUT", "/api/v1/catalog/defect-types/admin/{code}"),
            ("GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures"),
            ("POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/resolution"),
            ("POST", "/api/v1/invitations/preview"),
            ("GET", "/api/v1/projects/{projectId}/members/eligible"),
            ("POST", "/api/v1/projects/{projectId}/members"),
            ("PATCH", "/api/v1/users/{userId}/phone-number"),
            ("GET", "/api/v1/me/phone-number"),
            ("PATCH", "/api/v1/me/phone-number"),
            ("POST", "/api/v1/survey-plans/{planId}/complete"),
            ("POST", "/api/v1/survey-tasks/{taskId}/complete"),
            ("GET", "/api/v1/survey-tasks/{taskId}/datasets"),
            ("POST", "/api/v1/auth/reporter/password-reset/request"),
            ("POST", "/api/v1/auth/reporter/password-reset/confirm"),
            ("POST", "/api/v1/projects/{projectId}/handover-documents"),
            ("GET", "/api/v1/projects/{projectId}/handover-documents"),
            ("POST", "/api/v1/projects/handover-upload-intents"),
            ("GET", "/api/v1/survey-tasks/{taskId}/eligible-drones"),
            ("GET", "/api/v1/cases/{caseId}/reports"),
            ("GET", "/api/v1/cases/{caseId}/reports/{reportId}/evidence/{evidenceId}/content"),
            ("POST", "/api/v1/reporter-evidence/uploads/{uploadId}/cancel"),
            ("POST", "/api/v1/uploads/{uploadId}/cancel"),
            ("POST", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/submit"),
            ("GET", "/api/v1/projects/{projectId}/road-geometry-submission"),
            ("POST", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/reject"),
            ("GET", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-layouts"),
            ("GET", "/api/v1/projects/{projectId}/ai-videos"),
            ("GET", "/api/v1/projects/{projectId}/ai-videos/{workId}/telemetry"),
            ("POST", "/api/v1/projects/{projectId}/ai-videos/{workId}/telemetry-revisions")
        };
        Assert.Empty(expected.Except(actual));
        Assert.Empty(additions.Except(actual));
        Assert.Empty(actual.Except(expected).Except(additions));
    }
}
