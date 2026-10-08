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
    public async Task CorrelationIdIsDocumentedOnlyAsAResponseHeader()
    {
        using var document = await Document();
        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(method => method.Value.TryGetProperty("x-roadguard-api-id", out _))
            .Select(method => method.Value).ToArray();
        operations.Should().HaveCount(237);
        foreach (var operation in operations)
        {
            var id = operation.GetProperty("x-roadguard-api-id").GetString();
            if (operation.TryGetProperty("parameters", out var parameters))
                parameters.EnumerateArray().Should().NotContain(parameter =>
                    parameter.GetProperty("in").GetString() == "header" &&
                    string.Equals(parameter.GetProperty("name").GetString(), "X-Correlation-ID", StringComparison.OrdinalIgnoreCase), id);
            foreach (var response in operation.GetProperty("responses").EnumerateObject())
            {
                var header = response.Value.GetProperty("headers").GetProperty("X-Correlation-ID");
                header.GetProperty("schema").GetProperty("format").GetString().Should().Be("uuid", id + " " + response.Name);
            }
        }
    }

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
                if (method.Value.TryGetProperty("parameters", out var parameters))
                    foreach (var parameter in parameters.EnumerateArray())
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
            var parameters = op.TryGetProperty("parameters", out var inputs) ? inputs.EnumerateArray().ToArray() : Array.Empty<JsonElement>();
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

    [Fact]
    public async Task RequiredSourceValidatedFieldsAreNonNullableThroughoutTheRequestGraph()
    {
        using var document = await Document();
        using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "swagger-schema-expectations.json")));
        var requiredSourceFields = expected.RootElement.GetProperty("properties").EnumerateArray()
            .Where(x => x.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True &&
                x.GetProperty("baseline").TryGetProperty("nullable", out var nullable) && nullable.ValueKind == JsonValueKind.True)
            .Select(x => x.GetProperty("dto").GetString()!.Split('.')[^1] + "." + x.GetProperty("property").GetString())
            .ToHashSet(StringComparer.Ordinal);
        requiredSourceFields.Should().HaveCount(53);
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var affected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths.EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
            {
                if (!method.Value.TryGetProperty("x-roadguard-api-id", out var id) ||
                    !method.Value.TryGetProperty("requestBody", out var body)) continue;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var media in body.GetProperty("content").EnumerateObject())
                    Visit(media.Value.GetProperty("schema"), id.GetString()!, seen);
            }

        affected.Should().BeEquivalentTo(new[]
        {
            "SWG-121", "SWG-123", "SWG-124", "SWG-125", "SWG-132", "SWG-133", "SWG-136", "SWG-137", "SWG-138",
            "SWG-170", "SWG-171", "SWG-200", "SWG-201", "SWG-202", "SWG-203", "SWG-204", "SWG-205", "SWG-211",
            "SWG-214", "SWG-215", "SWG-216", "SWG-217", "SWG-223", "SWG-225", "SWG-226", "SWG-227"
        });

        void Visit(JsonElement shape, string id, HashSet<string> seen)
        {
            var name = shape.TryGetProperty("$ref", out var reference) ? reference.GetString()!.Split('/')[^1] : null;
            if (name is not null)
            {
                if (!seen.Add(name)) return;
                shape = schemas.GetProperty(name);
            }
            if (shape.TryGetProperty("required", out var required) && shape.TryGetProperty("properties", out var properties))
                foreach (var property in required.EnumerateArray())
                {
                    var value = properties.GetProperty(property.GetString()!);
                    if (!requiredSourceFields.Contains((name ?? "inline") + "." + property.GetString())) continue;
                    affected.Add(id);
                    (value.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean())
                        .Should().BeFalse(id + " " + (name ?? "inline") + "." + property.GetString());
                }
            if (shape.TryGetProperty("properties", out var nested))
                foreach (var property in nested.EnumerateObject()) Visit(property.Value, id, seen);
            if (shape.TryGetProperty("items", out var items)) Visit(items, id, seen);
            foreach (var composition in new[] { "allOf", "oneOf", "anyOf" })
                if (shape.TryGetProperty(composition, out var alternatives))
                    foreach (var item in alternatives.EnumerateArray()) Visit(item, id, seen);
        }
    }

    [Fact]
    public async Task AiCallbackPublishesOnlySourceValidatedConstraintsAndClaim()
    {
        using var document = await Document();
        var callback = document.RootElement.GetProperty("paths").GetProperty("/api/v1/internal/processing-jobs/{jobId}/results").GetProperty("post");
        callback.GetProperty("description").GetString().Should().Contain("client_type=AI_SERVICE").And.NotContain("AI_SERVICE role");
        document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("AiServiceBearer")
            .GetProperty("description").GetString().Should().Contain("client_type=AI_SERVICE");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var detection = schemas.GetProperty("AiDetectionDto").GetProperty("properties");
        detection.GetProperty("detectionId").GetProperty("format").GetString().Should().Be("uuid");
        detection.GetProperty("timestampMs").GetProperty("minimum").GetDecimal().Should().Be(0);
        detection.GetProperty("confidence").GetProperty("minimum").GetDecimal().Should().Be(0);
        detection.GetProperty("confidence").GetProperty("maximum").GetDecimal().Should().Be(1);
        var bbox = detection.GetProperty("bbox");
        bbox.GetProperty("minItems").GetInt32().Should().Be(4);
        bbox.GetProperty("maxItems").GetInt32().Should().Be(4);
        bbox.GetProperty("items").GetProperty("minimum").GetDecimal().Should().Be(0);
        bbox.GetProperty("items").GetProperty("maximum").GetDecimal().Should().Be(1);
        bbox.GetProperty("description").GetString().Should().Contain("x + width <= 1").And.Contain("y + height <= 1");
    }

    [Fact]
    public async Task ProductionInventoryAndEvidenceExcerptsMatchCurrentSource()
    {
        using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Contracts", "swagger-source-expectations.json")));
        using var document = await Document();
        var rows = expected.RootElement.EnumerateArray().ToArray();
        rows.GroupBy(x => x.GetProperty("owner").GetString()).ToDictionary(x => x.Key!, x => x.Count())
            .Should().BeEquivalentTo(new Dictionary<string, int>
            {
                ["HUY"] = 121,
                ["ANH"] = 93,
                ["SHARED_SETUP"] = 19,
                ["SPECIAL_EXTERNAL_OR_NONMANUAL"] = 4
            });
        var live = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject()
                .Where(method => method.Value.TryGetProperty("x-roadguard-api-id", out _))
                .Select(method => (Route: path.Name, Method: method.Name.ToUpperInvariant(), Id: method.Value.GetProperty("x-roadguard-api-id").GetString()!)))
            .ToArray();
        live.Should().HaveCount(237);
        live.Select(x => x.Id).Distinct().Should().HaveCount(237);
        live.Select(x => (x.Route, x.Method, x.Id))
            .Should().BeEquivalentTo(rows.Select(x => (Route: x.GetProperty("route").GetString()!, Method: x.GetProperty("method").GetString()!, Id: x.GetProperty("id").GetString()!)));

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "RoadGuardSystem.slnx")))
            root = root.Parent ?? throw new InvalidOperationException("Repository source root not found");
        var sourceCache = new Dictionary<string, string>(StringComparer.Ordinal);
        var checkedExcerpts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
            foreach (var source in row.GetProperty("sources").EnumerateArray())
            {
                var file = source.GetProperty("file").GetString()!;
                var excerpt = source.GetProperty("evidence").GetString()!;
                if (!checkedExcerpts.Add(file + "\0" + excerpt)) continue;
                if (!sourceCache.TryGetValue(file, out var actual))
                {
                    actual = File.ReadAllText(Path.Combine(root.FullName, file)).Replace("\r\n", "\n", StringComparison.Ordinal);
                    sourceCache.Add(file, actual);
                }
                actual.Should().Contain(excerpt, row.GetProperty("id").GetString() + " source " + file);
            }
        checkedExcerpts.Should().HaveCountGreaterThan(2500);
    }
}
