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
    public async Task GeometrySubmissionSwaggerDocumentsWebAndBearerWithVersionedReceipt()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        const string root = "/api/v1/projects/{projectId}";
        var submit = paths.GetProperty(root + "/road-geometry-drafts/{draftId}/submit").GetProperty("post");
        Assert.False(submit.TryGetProperty("requestBody", out _));
        Assert.Contains(submit.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("Bearer", out _));
        Assert.Contains(submit.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("WebSession", out _) && item.TryGetProperty("CsrfToken", out _));
        foreach (var header in new[] { "If-Match", "Idempotency-Key" })
            Assert.Contains(submit.GetProperty("parameters").EnumerateArray(), parameter =>
                parameter.GetProperty("name").GetString() == header && parameter.GetProperty("required").GetBoolean());
        Assert.True(submit.GetProperty("responses").GetProperty("201").GetProperty("headers").TryGetProperty("ETag", out _));
        var lookup = paths.GetProperty(root + "/road-geometry-submission").GetProperty("get");
        Assert.Contains(lookup.GetProperty("security").EnumerateArray(), item => item.TryGetProperty("WebSession", out _) && !item.TryGetProperty("CsrfToken", out _));
        Assert.True(lookup.GetProperty("responses").TryGetProperty("404", out _));
        var confirm = paths.GetProperty(root + "/road-geometry-drafts/{draftId}/confirm").GetProperty("post");
        Assert.Contains("road_geometry_submission_required", confirm.GetProperty("responses").GetProperty("409").GetProperty("description").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GeometryDraftReadsDocumentMetadataAndCoordinatesReadiness()
    {
        using var document = await Document();
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        const string draft = "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}";
        const string sectionDraft = "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}";
        foreach (var path in new[] { draft, sectionDraft })
        {
            var schema = paths.GetProperty(path).GetProperty("get").GetProperty("responses").GetProperty("200")
                .GetProperty("content").GetProperty("application/json").GetProperty("schema");
            Assert.Equal("#/components/schemas/GeometryDraftView", schema.GetProperty("$ref").GetString());
        }
        var properties = root.GetProperty("components").GetProperty("schemas").GetProperty("GeometryDraftView").GetProperty("properties");
        foreach (var field in new[] { "nativeAlignment", "routeSystemId", "routeKind", "parentRouteVersionId",
            "junctionOffsetMeters", "declaredLengthMeters", "chainageCalibration", "tessellationToleranceMeters" })
            Assert.True(properties.TryGetProperty(field, out _), field);
        Assert.False(properties.TryGetProperty("resolvedCrsProfile", out _));
        var readiness = paths.GetProperty(draft + "/readiness").GetProperty("get");
        Assert.Contains("COORDINATES", readiness.GetProperty("description").GetString());
        Assert.Contains("unsupported_crs", readiness.GetProperty("responses").GetProperty("422").GetProperty("description").GetString());
    }

    [Theory]
    [InlineData("/api/v1/reporter-evidence/uploads/{uploadId}/cancel", false)]
    [InlineData("/api/v1/uploads/{uploadId}/cancel", true)]
    public async Task UploadCancelAdvertisesBearerAndTerminalResponse(string route, bool crew)
    {
        using var document = await Document();
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var cancel = paths.GetProperty(route).GetProperty("post");
        var security = Assert.Single(cancel.GetProperty("security").EnumerateArray());
        Assert.True(security.TryGetProperty("Bearer", out _));
        Assert.False(cancel.TryGetProperty("requestBody", out _));
        var responses = cancel.GetProperty("responses");
        foreach (var status in new[] { "200", "401", "404", "409" })
            Assert.True(responses.TryGetProperty(status, out _));
        Assert.True(responses.GetProperty("409").GetProperty("content").TryGetProperty("application/problem+json", out _));
        var schema = root.GetProperty("components").GetProperty("schemas").GetProperty("UploadCancelResponseDto");
        Assert.True(schema.GetProperty("properties").TryGetProperty("failureCode", out _));
        Assert.Equal(crew, responses.TryGetProperty("403", out _));
        if (crew)
        {
            var description = cancel.GetProperty("description").GetString();
            Assert.Contains("DroneOperator", description, StringComparison.Ordinal);
            Assert.Contains("SURVEY_VIDEO and TELEMETRY", description, StringComparison.Ordinal);
            Assert.Contains("RepairCrew", description, StringComparison.Ordinal);
        }
        Assert.All(cancel.GetProperty("parameters").EnumerateArray(),
            parameter => Assert.Equal("path", parameter.GetProperty("in").GetString()));
        Assert.Equal("#/components/schemas/UploadCancelResponseDto",
            responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task PavementLayoutDocumentsOptionalSavedConfiguration()
    {
        using var document = await Document();
        var root = document.RootElement;
        var schema = root.GetProperty("components").GetProperty("schemas");
        var layout = schema.GetProperty("PavementLayoutView").GetProperty("properties").GetProperty("layout");
        Assert.True(layout.GetProperty("nullable").GetBoolean());
        Assert.Equal("#/components/schemas/PavementLayoutInput", layout.GetProperty("allOf")[0].GetProperty("$ref").GetString());
        var input = schema.GetProperty("PavementLayoutInput").GetProperty("properties");
        foreach (var name in new[] { "slabThicknessCentimeters", "contractionJointSpacingMeters",
            "expansionJointSpacingMeters", "expansionJointGapMillimeters" })
        {
            var field = input.GetProperty(name);
            Assert.True(field.GetProperty("nullable").GetBoolean());
            Assert.Equal(0, field.GetProperty("minimum").GetInt32());
            Assert.Equal(name != "expansionJointGapMillimeters",
                field.TryGetProperty("exclusiveMinimum", out var exclusive) && exclusive.GetBoolean());
        }
    }

    [Fact]
    public async Task StepTwoWritesExposeBearerAndWebCsrfInRuntimeSwagger()
    {
        using var document = await Document();
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var writes = new (string Route, string Method, string? RequiredHeader)[]
        {
            ("/api/v1/invitations", "post", "Idempotency-Key"),
            ("/api/v1/users/{userId}", "patch", "Idempotency-Key"),
            ("/api/v1/users/{userId}/password-reset", "post", "Idempotency-Key"),
            ("/api/v1/projects/{projectId}", "put", null),
            ("/api/v1/projects/{projectId}/road-sections", "post", null),
            ("/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions", "post", null)
        };
        foreach (var (route, method, requiredHeader) in writes)
        {
            var operation = paths.GetProperty(route).GetProperty(method);
            var security = operation.GetProperty("security").EnumerateArray().ToArray();
            Assert.Equal(2, security.Length);
            Assert.Contains(security, x => x.TryGetProperty("Bearer", out _) && x.EnumerateObject().Count() == 1);
            var web = Assert.Single(security, x => x.TryGetProperty("WebSession", out _));
            Assert.Equal(3, web.EnumerateObject().Count());
            Assert.True(web.TryGetProperty("CsrfToken", out _));
            Assert.True(web.TryGetProperty("AntiforgeryCookie", out _));
            Assert.Contains("csrf_failed", operation.GetProperty("responses").GetProperty("403").GetProperty("description").GetString());
            var headers = operation.GetProperty("parameters").EnumerateArray()
                .Where(x => x.GetProperty("in").GetString() == "header").ToArray();
            Assert.DoesNotContain(headers, x => x.GetProperty("name").GetString() == "X-CSRF-TOKEN");
            Assert.Equal(requiredHeader is not null, headers.Any(x => x.GetProperty("name").GetString() == "Idempotency-Key" &&
                x.GetProperty("required").GetBoolean()));
        }
    }

    [Fact]
    public async Task EligibleSurveyDronesExposeOnlySelectionFieldsAndBoundedBearerRead()
    {
        using var document = await Document();
        var root = document.RootElement;
        var operation = root.GetProperty("paths").GetProperty("/api/v1/survey-tasks/{taskId}/eligible-drones").GetProperty("get");
        var security = Assert.Single(operation.GetProperty("security").EnumerateArray());
        Assert.True(security.TryGetProperty("Bearer", out _));
        Assert.Contains("shared registry", operation.GetProperty("description").GetString());
        var limit = operation.GetProperty("parameters").EnumerateArray().Single(p => p.GetProperty("name").GetString() == "limit").GetProperty("schema");
        Assert.Equal(25, limit.GetProperty("default").GetInt32());
        Assert.Equal(1, limit.GetProperty("minimum").GetInt32());
        Assert.Equal(100, limit.GetProperty("maximum").GetInt32());
        var fields = root.GetProperty("components").GetProperty("schemas").GetProperty("EligibleDroneDto").GetProperty("properties");
        Assert.Equal(new[] { "id", "serialNo" }, fields.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        foreach (var status in new[] { "200", "400", "401", "403", "404" })
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
    }

    [Fact]
    public async Task SurveyDetailsAdvertiseWebAndBearerAtResourceRoutes()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        foreach (var route in new[] { "/api/v1/survey-plans/{id}", "/api/v1/survey-tasks/{taskId}" })
        {
            var security = paths.GetProperty(route).GetProperty("get").GetProperty("security").EnumerateArray().ToArray();
            Assert.Equal(2, security.Length);
            Assert.Contains(security, x => x.TryGetProperty("Bearer", out _) && x.EnumerateObject().Count() == 1);
            Assert.Contains(security, x => x.TryGetProperty("WebSession", out _) && x.EnumerateObject().Count() == 1);
            Assert.False(paths.TryGetProperty(route.Replace("/api/v1/", "/api/v1/projects/{projectId}/", StringComparison.Ordinal), out _));
        }
    }

    [Theory]
    [InlineData("primary-project-manager", "put", true)]
    [InlineData("work-package", "get", false)]
    public async Task ProjectOperationsAdvertiseBearerOrWebSession(string suffix, string verb, bool csrf)
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        var security = paths.GetProperty("/api/v1/projects/{projectId}/" + suffix)
            .GetProperty(verb).GetProperty("security").EnumerateArray().ToArray();
        Assert.Equal(2, security.Length);
        Assert.Contains(security, x => x.TryGetProperty("Bearer", out _) && x.EnumerateObject().Count() == 1);
        var web = Assert.Single(security, x => x.TryGetProperty("WebSession", out _));
        Assert.Equal(csrf ? 3 : 1, web.EnumerateObject().Count());
        Assert.Equal(csrf, web.TryGetProperty("CsrfToken", out _));
        Assert.Equal(csrf, web.TryGetProperty("AntiforgeryCookie", out _));
    }

    [Fact]
    public async Task StaffPhoneWriteDocumentsBearerCookieCsrfAndRequiredHeaders()
    {
        using var document = await Document();
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/v1/users/{userId}/phone-number").GetProperty("patch");
        var security = operation.GetProperty("security");
        Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("Bearer", out _));
        Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("WebSession", out _) &&
            x.TryGetProperty("CsrfToken", out _) && x.TryGetProperty("AntiforgeryCookie", out _));
        var headers = operation.GetProperty("parameters").EnumerateArray()
            .Where(x => x.GetProperty("in").GetString() == "header").ToArray();
        foreach (var name in new[] { "If-Match", "Idempotency-Key" })
            Assert.Contains(headers, x => x.GetProperty("name").GetString() == name && x.GetProperty("required").GetBoolean());
        Assert.Contains("Supervisor", operation.GetProperty("description").GetString());
        foreach (var status in new[] { "204", "400", "401", "403", "404", "409", "412", "428" })
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _), status);
    }

    [Fact]
    public async Task ApprovedCollectionsAndPublicWritesDocumentCookieAlternativeWithoutSecuringAnonymousRead()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        foreach (var suffix in new[] { "repair-packages", "repair-packages/{packageId}/items", "exports", "survey-plans", "survey-tasks", "processing-jobs", "ai-mock-runs", "offline/devices", "offline/packages", "offline/grants", "retention/evaluations" })
        {
            var security = paths.GetProperty("/api/v1/projects/{projectId}/" + suffix).GetProperty("get").GetProperty("security");
            Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("Bearer", out _));
            Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("WebSession", out _) && x.EnumerateObject().Count() == 1);
        }
        foreach (var route in new[] { "/api/v1/projects/{projectId}/defects/{defectId}/publications", "/api/v1/projects/{projectId}/publications/{publicId}/withdraw" })
        {
            var security = paths.GetProperty(route).GetProperty("post").GetProperty("security");
            Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("Bearer", out _) && x.EnumerateObject().Count() == 1);
            Assert.Contains(security.EnumerateArray(), x => x.TryGetProperty("WebSession", out _) && x.TryGetProperty("CsrfToken", out _) && x.TryGetProperty("AntiforgeryCookie", out _));
        }
        var read = paths.GetProperty("/api/v1/public/publications/{publicId}").GetProperty("get");
        Assert.True(!read.TryGetProperty("security", out var anonymous) || anonymous.GetArrayLength() == 0);
        var capture = Environment.GetEnvironmentVariable("ROADGUARD_OPENAPI_CAPTURE_PATH");
        if (!string.IsNullOrWhiteSpace(capture)) await File.WriteAllTextAsync(capture, document.RootElement.GetRawText());
    }

    [Fact]
    public async Task ApprovedDiscoveryCollectionsHaveDistinctTypedPages()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        foreach (var suffix in new[] { "repair-packages", "repair-packages/{packageId}/items", "exports", "survey-plans", "survey-tasks", "processing-jobs", "ai-mock-runs", "offline/devices", "offline/packages", "offline/grants", "retention/evaluations" })
        {
            var path = "/api/v1/projects/{projectId}/" + suffix;
            Assert.True(paths.TryGetProperty(path, out var operation) && operation.TryGetProperty("get", out _), "Missing approved GET " + path);
            var get = operation.GetProperty("get");
            Assert.True(get.GetProperty("responses").TryGetProperty("200", out _));
            var parameters = get.GetProperty("parameters").EnumerateArray().ToArray();
            var limit = parameters.Single(x => x.GetProperty("name").GetString() == "limit").GetProperty("schema");
            Assert.Equal(25, limit.GetProperty("default").GetInt32());
            Assert.Equal(100, limit.GetProperty("maximum").GetInt32());
            Assert.Contains(parameters, x => x.GetProperty("name").GetString() == "cursor");
            Assert.Contains("CollectionPage", get.GetProperty("responses").GetProperty("200").GetRawText());
        }
    }
    [Fact]
    public async Task PrivateFileReadDocumentsOpaqueVersionAndNoStoreWithoutNewRoutes()
    {
        using var document = await Document();
        var paths = document.RootElement.GetProperty("paths");
        foreach (var route in new[] { "/api/v1/files/{fileId}", "/api/v1/files/{fileId}/content" })
        {
            var get = paths.GetProperty(route).GetProperty("get");
            get.GetProperty("responses").GetProperty("200").GetProperty("headers")
                .GetProperty("Cache-Control").GetProperty("description").GetString().Should().Be("private, no-store");
            get.GetProperty("description").GetString().Should().Contain("AI frame");
        }
        var etag = paths.GetProperty("/api/v1/files/{fileId}").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("headers").GetProperty("ETag");
        etag.GetProperty("description").GetString().Should().Contain("opaque").And.NotContain("8-byte");
    }

    [Fact]
    public async Task CrewTaskBootstrapDocumentsSeparateItemVersionWithoutChangingTaskVersion()
    {
        using var document = await Document();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var task = schemas.GetProperty("FieldTaskViewFact").GetProperty("properties");
        task.GetProperty("version").GetProperty("type").GetString().Should().Be("string");
        task.GetProperty("repairContext").GetRawText().Should().Contain("CrewRepairContextFact");
        Assert.True(task.GetProperty("repairContext").GetProperty("nullable").GetBoolean());
        var context = schemas.GetProperty("CrewRepairContextFact").GetProperty("properties");
        context.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo(new[]
        {
            "packageId", "itemId", "bindingId", "mode", "status", "itemVersion", "repairPlan", "checklistVersion", "attempt"
        });
        context.GetProperty("itemVersion").GetProperty("type").GetString().Should().Be("string");
        context.GetProperty("attempt").GetRawText().Should().Contain("CrewRepairAttemptFact");
        Assert.True(context.GetProperty("attempt").GetProperty("nullable").GetBoolean());
        var attempt = schemas.GetProperty("CrewRepairAttemptFact").GetProperty("properties");
        attempt.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("attemptId", "effectiveSubmissionId", "review");
        attempt.GetProperty("review").GetRawText().Should().Contain("CrewRepairReviewFact");
        Assert.True(attempt.GetProperty("review").GetProperty("nullable").GetBoolean());
        schemas.GetProperty("CrewRepairReviewFact").GetProperty("properties").EnumerateObject()
            .Select(x => x.Name).Should().BeEquivalentTo("reviewId", "decision");
    }

    [Fact]
    public async Task OnlyRequiredCookieFlowsExposeCsrfAsAManualInput()
    {
        using var document = await Document();
        var visible = new List<string>();
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
            {
                var operation = method.Value;
                if (!operation.TryGetProperty("x-roadguard-api-id", out _)) continue;
                var parameters = operation.TryGetProperty("parameters", out var inputs) ? inputs.EnumerateArray().ToArray() : Array.Empty<JsonElement>();
                var csrf = parameters.Where(p => p.GetProperty("in").GetString() == "header" && p.GetProperty("name").GetString() == "X-CSRF-TOKEN").ToArray();
                var security = operation.TryGetProperty("security", out var schemes) ? schemes.EnumerateArray().ToArray() : Array.Empty<JsonElement>();
                var bearer = security.Any(s => s.TryGetProperty("Bearer", out _));
                if (bearer)
                {
                    csrf.Should().BeEmpty(path.Name);
                    if (method.Name is not ("get" or "head" or "options"))
                        foreach (var cookie in security.Where(s => s.TryGetProperty("WebSession", out _)))
                        {
                            cookie.TryGetProperty("CsrfToken", out _).Should().BeTrue(path.Name);
                            cookie.TryGetProperty("AntiforgeryCookie", out _).Should().BeTrue(path.Name);
                        }
                }
                if (csrf.Length == 0) continue;
                csrf.Should().ContainSingle(path.Name);
                csrf[0].GetProperty("required").GetBoolean().Should().BeTrue(path.Name);
                security.Any(s => s.TryGetProperty("CsrfToken", out _) && s.TryGetProperty("AntiforgeryCookie", out _)).Should().BeTrue(path.Name);
                visible.Add(method.Name.ToUpperInvariant() + " " + path.Name);
            }
        visible.Should().BeEquivalentTo(new[]
        {
            "POST /api/v1/auth/web/login",
            "POST /api/v1/auth/web/renew",
            "POST /api/v1/auth/web/logout"
        });
    }

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
            var bearer = op.TryGetProperty("security", out var security) && security.EnumerateArray().Any(s => s.TryGetProperty("Bearer", out _));
            var visibleHeaders = row.GetProperty("headers").EnumerateArray()
                .Where(h => !(bearer && h.GetProperty("name").GetString() == "X-CSRF-TOKEN")).ToArray();
            parameters.Where(p => p.GetProperty("in").GetString() == "header").Select(p => p.GetProperty("name").GetString())
                .Should().BeEquivalentTo(visibleHeaders.Select(h => h.GetProperty("name").GetString()), id + " only source-defined manual headers");
            foreach (var header in visibleHeaders)
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
        parameters.Select(p => p.GetProperty("name").GetString()).Should().BeEquivalentTo(new[] { "caseId", "If-Match", "Idempotency-Key" });
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
        triage.GetProperty("parameters").EnumerateArray().Should().NotContain(x => x.GetProperty("name").GetString() == "X-CSRF-TOKEN");
        triage.GetProperty("responses").GetProperty("403").GetProperty("description").GetString().Should().Contain("csrf_failed");
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
        callback.GetProperty("description").GetString().Should().Contain("RS256").And.Contain("jobId").And.Contain("attemptId").And.Contain("roadguard-ai-callback").And.Contain("300 seconds");
        document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("AiServiceBearer")
            .GetProperty("description").GetString().Should().Contain("roadguard-ai-callback").And.Contain("300 seconds");
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
