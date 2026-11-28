using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Surveys;

[Trait("TaskId", "P1-22")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P122SurveyPlanningTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P122SurveyPlanningTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task CreateSurveyPlan_AnonymousCaller_IsUnauthorized()
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/road-sections/{Guid.NewGuid()}/survey-plans",
            new
            {
                plannedStartAt = "2026-10-01T08:00:00Z",
                plannedEndAt = "2026-10-01T10:00:00Z",
                surveyType = 2,
                outputRequirements = "{\"formats\":[\"video\"]}",
                operationId = Guid.NewGuid()
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlanningCommands_RejectDatesOutsideTheWarranty_WithProblemDetails()
    {
        var supervisor = await _sql.CreateUserAsync($"p122_supervisor_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync($"p122_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        var roadSection = await CreateRoadSectionAsync(client, projectId);
        await AuthenticateAsync(client, projectManager.UserName!, "Current1!");

        var url = $"/api/v1/projects/{projectId}/road-sections/{roadSection.RoadSectionId}/survey-plans";
        var request = new
        {
            plannedStartAt = "2027-08-31T22:00:00+07:00",
            plannedEndAt = "2027-09-01T00:01:00+07:00",
            roadSectionVersionId = roadSection.VersionId,
            surveyType = 2,
            outputRequirements = "{\"formats\":[\"video\"]}",
            operationId = Guid.NewGuid()
        };
        using var rejectedPlan = await client.PostAsJsonAsync(url, request);
        rejectedPlan.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        rejectedPlan.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await rejectedPlan.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("survey_outside_warranty");
        problem.GetProperty("field").GetString().Should().Be("plannedEndAt");
        problem.GetProperty("warrantyEndDate").GetString().Should().Be("2027-08-31");
        problem.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();

        using var zeroDuration = await client.PostAsJsonAsync(url, request with
        {
            plannedEndAt = request.plannedStartAt,
            operationId = Guid.NewGuid()
        });
        zeroDuration.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await zeroDuration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("survey_time_range_invalid");

        using var acceptedPlan = await client.PostAsJsonAsync(url, request with { plannedEndAt = "2027-09-01T00:00:00+07:00" });
        acceptedPlan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await acceptedPlan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("planId").GetGuid();

        var taskUrl = $"/api/v1/projects/{projectId}/road-sections/{roadSection.RoadSectionId}/survey-requests";
        var taskRequest = new
        {
            surveyPlanId = planId,
            roadSectionVersionId = roadSection.VersionId,
            surveyType = request.surveyType,
            dueAt = (string?)"2027-09-01T00:01:00+07:00",
            outputRequirements = request.outputRequirements,
            operationId = Guid.NewGuid()
        };
        using var rejectedTask = await client.PostAsJsonAsync(taskUrl, taskRequest);
        rejectedTask.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var taskProblem = await rejectedTask.Content.ReadFromJsonAsync<JsonElement>();
        taskProblem.GetProperty("code").GetString().Should().Be("survey_outside_warranty");
        taskProblem.GetProperty("field").GetString().Should().Be("dueAt");

        using var implicitDeadlineTask = await client.PostAsJsonAsync(taskUrl, taskRequest with { dueAt = null });
        implicitDeadlineTask.StatusCode.Should().Be(HttpStatusCode.Created);

        using var rejectedPostponement = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/survey-plans/{planId}/postpone", new
        {
            newPlannedStartAt = "2026-08-30T16:59:59Z",
            reason = "Weather delay",
            operationId = Guid.NewGuid()
        });
        rejectedPostponement.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var postponeProblem = await rejectedPostponement.Content.ReadFromJsonAsync<JsonElement>();
        postponeProblem.GetProperty("code").GetString().Should().Be("survey_outside_warranty");
        postponeProblem.GetProperty("field").GetString().Should().Be("newPlannedStartAt");
    }

    [Fact]
    public async Task PlanningCommands_CreateRequestAndPostpone_WithReplaySafeResponses()
    {
        var supervisor = await _sql.CreateUserAsync($"p122_supervisor_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync($"p122_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        var roadSection = await CreateRoadSectionAsync(client, projectId);
        var roadSectionId = roadSection.RoadSectionId;
        await AuthenticateAsync(client, projectManager.UserName!, "Current1!");

        await using (var schema = _sql.CreateDbContext())
        {
            var versionColumns = await schema.Database.SqlQueryRaw<int>("""
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.columns
                WHERE ([object_id] = OBJECT_ID(N'[dbo].[SurveyPlans]') AND [name] = N'RoadSectionVersionId')
                   OR ([object_id] = OBJECT_ID(N'[dbo].[SurveyRequests]') AND [name] = N'RoadSectionVersionId')
                """).SingleAsync();
            versionColumns.Should().Be(2);
        }

        var planRequest = new
        {
            plannedStartAt = "2026-10-01T08:00:00Z",
            plannedEndAt = "2026-10-01T10:00:00Z",
            roadSectionVersionId = roadSection.VersionId,
            surveyType = 2,
            outputRequirements = "{\"formats\":[\"video\"]}",
            operationId = Guid.NewGuid()
        };
        var createdPlan = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans", planRequest);
        var createdPlanBody = await createdPlan.Content.ReadAsStringAsync();
        if (!createdPlan.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create plan failed: {(int)createdPlan.StatusCode} {createdPlanBody}");
        }
        createdPlan.StatusCode.Should().Be(HttpStatusCode.Created);
        var plan = JsonSerializer.Deserialize<JsonElement>(createdPlanBody);
        var planId = plan.GetProperty("planId").GetGuid();
        plan.GetProperty("status").GetByte().Should().Be(1);

        var wrongVersion = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans", new
        {
            plannedStartAt = "2026-10-02T08:00:00Z",
            plannedEndAt = "2026-10-02T10:00:00Z",
            roadSectionVersionId = Guid.NewGuid(),
            surveyType = 2,
            outputRequirements = "{\"formats\":[\"video\"]}",
            operationId = Guid.NewGuid()
        });
        wrongVersion.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var replay = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans", planRequest);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(createdPlanBody);

        var createdRequest = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests", new
        {
            surveyPlanId = planId,
            roadSectionVersionId = roadSection.VersionId,
            surveyType = 2,
            dueAt = "2026-10-04T17:00:00Z",
            outputRequirements = "{\"formats\":[\"video\",\"srt\"]}",
            operationId = Guid.NewGuid()
        });
        createdRequest.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await createdRequest.Content.ReadFromJsonAsync<JsonElement>();
        request.GetProperty("status").GetByte().Should().Be(1);
        request.GetProperty("dueAt").GetDateTimeOffset().Should().Be(new DateTimeOffset(2026, 10, 4, 17, 0, 0, TimeSpan.Zero));

        var postponed = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/survey-plans/{planId}/postpone", new
        {
            newPlannedStartAt = "2026-10-01T09:00:00Z",
            reason = "Weather delay",
            operationId = Guid.NewGuid()
        });
        postponed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await postponed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetByte().Should().Be(2);
    }

    private async Task<Guid> CreateProjectAsync(HttpClient client, Guid projectManagerId)
    {
        var response = await VerifiedProjectHandoverFixture.CreateProjectAsync(_sql, client, new
        {
            projectCode = $"P-{Guid.NewGuid():N}",
            name = "P1-22 planning project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-08-31",
            primaryProjectManagerUserId = projectManagerId,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    private static async Task<(Guid RoadSectionId, Guid VersionId)> CreateRoadSectionAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("roadSectionId").GetGuid(), body.GetProperty("roadSectionVersionId").GetGuid());
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
}
