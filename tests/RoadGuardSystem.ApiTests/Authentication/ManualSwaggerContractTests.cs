using System.Net;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class ManualSwaggerContractTests
{
    [Fact]
    public async Task VisibleDescriptionsStayConciseWithoutAuditNarrative()
    {
        using var document = await Document();
        var triage = document.RootElement.GetProperty("paths").GetProperty("/api/v1/cases/{caseId}/triage").GetProperty("post");
        triage.GetProperty("responses").GetProperty("200").GetProperty("headers").GetProperty("ETag").GetProperty("description")
            .GetString().Should().Contain("Base64 rowversion").And.NotContain("content-hash");
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        foreach (var method in path.Value.EnumerateObject())
        {
            if (!method.Value.TryGetProperty("x-roadguard-api-id", out _)) continue;
            var description = method.Value.GetProperty("description").GetString()!;
            description.Length.Should().BeLessThanOrEqualTo(320, path.Name);
            foreach (var forbidden in new[] { "source review", "stable inventory", "UNKNOWN", "unadvertisedErrorCandidates", "family candidates", "rebootstrap", "repository audit", "manual evidence ledger" })
                description.Should().NotContain(forbidden, path.Name);
            foreach (var parameter in method.Value.GetProperty("parameters").EnumerateArray())
                if (parameter.TryGetProperty("description", out var text))
                    text.GetString()!.Length.Should().BeLessThanOrEqualTo(250, path.Name + " parameter description");
        }
    }
    [Fact]
    public async Task SourceValidatedRequestPropertiesAndQueryConstraintsAreExposed()
    {
        using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "swagger-schema-expectations.json")));
        using var document = await Document();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var rule in expected.RootElement.GetProperty("properties").EnumerateArray())
        {
            var name = rule.GetProperty("dto").GetString()!.Split('.')[^1];
            var schema = schemas.GetProperty(name);
            var propertyName = rule.GetProperty("property").GetString()!;
            var property = schema.GetProperty("properties").GetProperty(propertyName);
            foreach (var constraint in new[] { "enum", "minimum", "maximum", "minLength", "maxLength", "minItems", "maxItems", "pattern", "nullable", "exclusiveMinimum", "exclusiveMaximum" })
            {
                if (!rule.TryGetProperty(constraint, out var value) || value.ValueKind == JsonValueKind.Null) continue;
                if (value.ValueKind == JsonValueKind.False) (property.TryGetProperty(constraint, out var actual) && actual.GetBoolean()).Should().BeFalse(name + "." + propertyName);
                else if (value.ValueKind == JsonValueKind.Array) property.GetProperty(constraint).EnumerateArray().Select(x => x.ToString()).Should().Equal(value.EnumerateArray().Select(x => x.ToString()), name + "." + propertyName);
                else property.GetProperty(constraint).ToString().Should().Be(value.ToString(), name + "." + propertyName + " " + constraint);
            }
            if (rule.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True)
                schema.GetProperty("required").EnumerateArray().Any(x => x.GetString() == propertyName).Should().BeTrue(name + "." + propertyName);
        }
        var page = document.RootElement.GetProperty("paths").GetProperty("/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}").GetProperty("get");
        var hash = page.GetProperty("parameters").EnumerateArray().Single(x => x.GetProperty("name").GetString() == "If-Match");
        (hash.TryGetProperty("required", out var hashRequired) && hashRequired.GetBoolean()).Should().BeFalse("query hash OR header, not both mandatory");
        var web = document.RootElement.GetProperty("paths").GetProperty("/api/v1/auth/web/session").GetProperty("get");
        web.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("properties").TryGetProperty("user", out _).Should().BeTrue();
    }
    [Fact]
    public async Task EveryProductionOperationMatchesIndependentSourceAudit()
    {
        using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "swagger-source-expectations.json")));
        using var document = await Document();
        var rows = expected.RootElement.EnumerateArray().ToArray();
        rows.Should().HaveCount(237);
        rows.Select(x => x.GetProperty("id").GetString()).Distinct().Should().HaveCount(237);
        var paths = document.RootElement.GetProperty("paths");
        foreach (var row in rows)
        {
            var id = row.GetProperty("id").GetString();
            var op = paths.GetProperty(row.GetProperty("route").GetString()!).GetProperty(row.GetProperty("method").GetString()!.ToLowerInvariant());
            op.GetProperty("x-roadguard-api-id").GetString().Should().Be(id);
            var parameters = op.GetProperty("parameters").EnumerateArray().ToArray();
            parameters.Select(x => x.GetProperty("in").GetString() + ":" + x.GetProperty("name").GetString()).Distinct().Should().HaveCount(parameters.Length, id);
            foreach (var header in row.GetProperty("headers").EnumerateArray())
            {
                var name = header.GetProperty("name").GetString();
                var p = parameters.Single(x => x.GetProperty("in").GetString() == "header" && x.GetProperty("name").GetString() == name);
                (p.TryGetProperty("required", out var required) && required.GetBoolean()).Should().Be(header.GetProperty("required").GetBoolean(), id + " " + name);
                foreach (var property in new[] { "minLength", "maxLength", "pattern" })
                    if (header.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null)
                        p.GetProperty("schema").GetProperty(property).ToString().Should().Be(value.ToString(), id + " " + name + " " + property);
            }
            var bodyRequired = row.GetProperty("bodyRequired").GetBoolean();
            op.TryGetProperty("requestBody", out var body).Should().Be(bodyRequired, id);
            if (bodyRequired) body.GetProperty("required").GetBoolean().Should().BeTrue(id);
            foreach (var success in row.GetProperty("successResponses").EnumerateArray())
            {
                var response = op.GetProperty("responses").GetProperty(success.GetProperty("status").ToString());
                foreach (var header in success.GetProperty("headers").EnumerateArray())
                    response.GetProperty("headers").TryGetProperty(header.GetProperty("name").GetString()!, out _).Should().BeTrue(id);
                var expectsEtag = success.GetProperty("headers").EnumerateArray().Any(x => x.GetProperty("name").GetString() == "ETag");
                response.GetProperty("headers").TryGetProperty("ETag", out _).Should().Be(expectsEtag, id + " ETag negative control");
                if (success.GetProperty("status").GetInt32() == 204) response.TryGetProperty("content", out _).Should().BeFalse(id);
            }
            row.GetProperty("sources").GetArrayLength().Should().BeGreaterThan(0, id + " independent source evidence");
        }
    }
    private static async Task<JsonDocument> Document()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CaseTriageExposesAllRequiredManualInputsAndProducedVersion()
    {
        using var document = await Document();
        var op = document.RootElement.GetProperty("paths").GetProperty("/api/v1/cases/{caseId}/triage").GetProperty("post");
        var parameters = op.GetProperty("parameters").EnumerateArray().ToArray();
        foreach (var name in new[] { "caseId", "If-Match", "Idempotency-Key" })
            parameters.Should().ContainSingle(p => p.GetProperty("name").GetString() == name && p.GetProperty("required").GetBoolean());
        op.GetProperty("requestBody").GetProperty("required").GetBoolean().Should().BeTrue();
        op.GetProperty("responses").GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _).Should().BeTrue();
        op.GetProperty("responses").TryGetProperty("428", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAndBodylessAuthRetainTheirActualNegativeControls()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        var preview = paths.GetProperty("/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview").GetProperty("post");
        preview.TryGetProperty("requestBody", out _).Should().BeFalse();
        if (preview.TryGetProperty("parameters", out var p))
            p.EnumerateArray().Where(x => x.GetProperty("in").GetString() == "header")
                .Any(x => x.TryGetProperty("required", out var required) && required.GetBoolean()).Should().BeFalse();
        var refresh = paths.GetProperty("/api/v1/auth/refresh").GetProperty("post");
        var refreshKey = refresh.GetProperty("parameters").EnumerateArray().Single(x => x.GetProperty("name").GetString() == "Idempotency-Key");
        (refreshKey.TryGetProperty("required", out var requiredKey) && requiredKey.GetBoolean()).Should().BeFalse();
        refresh.TryGetProperty("security", out _).Should().BeFalse();
        paths.GetProperty("/api/v1/auth/web/renew").GetProperty("post").TryGetProperty("requestBody", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CookieOnlyAndAiCallbackNeverAdvertiseHumanBearer()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        var session = paths.GetProperty("/api/v1/auth/web/session").GetProperty("get");
        session.GetProperty("security")[0].TryGetProperty("WebSession", out _).Should().BeTrue();
        session.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeFalse();
        var callback = paths.GetProperty("/api/v1/internal/processing-jobs/{jobId}/results").GetProperty("post");
        callback.GetProperty("security")[0].TryGetProperty("AiServiceBearer", out _).Should().BeTrue();
        var triage = paths.GetProperty("/api/v1/cases/{caseId}/triage").GetProperty("post");
        var auth = triage.GetProperty("security").EnumerateArray().ToArray();
        auth.Any(x => x.TryGetProperty("Bearer", out _)).Should().BeTrue();
        auth.Any(x => x.TryGetProperty("WebSession", out _) && x.TryGetProperty("CsrfToken", out _)).Should().BeTrue();
        var csrf = triage.GetProperty("parameters").EnumerateArray().Single(x => x.GetProperty("name").GetString() == "X-CSRF-TOKEN");
        (csrf.TryGetProperty("required", out var requiredCsrf) && requiredCsrf.GetBoolean()).Should().BeFalse();
    }
}
