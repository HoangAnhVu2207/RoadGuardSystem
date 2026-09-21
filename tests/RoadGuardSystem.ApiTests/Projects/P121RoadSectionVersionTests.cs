using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Trait("TaskId", "P1-21")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P121RoadSectionVersionTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P121RoadSectionVersionTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task CreateRoadSectionSupervisorSuccessPersistsInitialVersionAuditAndReplay()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"road_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"road_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        var request = new
        {
            code = $"RS-{Guid.NewGuid():N}",
            name = "Northern carriageway",
            srid = 32648,
            coordinates = new[]
            {
                new { x = 500000d, y = 1100000d },
                new { x = 500100d, y = 1100100d }
            },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial surveyed alignment",
            operationId = Guid.NewGuid()
        };

        var created = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", request);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var bodyText = await created.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(bodyText);
        body.RootElement.GetProperty("projectId").GetGuid().Should().Be(projectId);
        body.RootElement.GetProperty("roadSectionId").GetGuid().Should().NotBeEmpty();
        body.RootElement.GetProperty("roadSectionVersionId").GetGuid().Should().NotBeEmpty();
        body.RootElement.GetProperty("versionNo").GetInt32().Should().Be(1);
        body.RootElement.GetProperty("isCurrent").GetBoolean().Should().BeTrue();
        created.Headers.Location!.OriginalString.Should().Be(
            $"/api/v1/projects/{projectId}/road-sections/{body.RootElement.GetProperty("roadSectionId").GetGuid()}");

        var replay = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", request);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(bodyText);

        var roadSectionId = body.RootElement.GetProperty("roadSectionId").GetGuid();
        var versionId = body.RootElement.GetProperty("roadSectionVersionId").GetGuid();
        await using var verification = _sql.CreateDbContext();
        (await verification.RoadSections.CountAsync(section => section.Id == roadSectionId)).Should().Be(1);
        (await verification.RoadSectionVersions.CountAsync(version =>
            version.Id == versionId && version.RoadSectionId == roadSectionId && version.IsCurrent)).Should().Be(1);
        var creationAudit = await verification.AuditLogs.SingleAsync(audit =>
            audit.EntityId == roadSectionId && audit.EventType == "road_section_created");
        creationAudit.Reason.Should().Be("ROAD_SECTION_CREATED");
        creationAudit.AfterSnapshot.Should().NotContain(request.changeReason);
        creationAudit.AfterSnapshot.Should().NotContain("geometry").And.NotContain("coordinates");
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.ActorUserId == supervisor.Id &&
            record.ProjectId == projectId &&
            record.Operation == "RoadSectionCreated" &&
            record.IdempotencyKey == request.operationId.ToString("N"))).Should().Be(1);
    }

    [Fact]
    public async Task AddRoadSectionVersionMakesNextVersionCurrentAndRejectsStaleCurrentVersion()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"version_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"version_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        var created = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var initial = await created.Content.ReadFromJsonAsync<JsonElement>();
        var roadSectionId = initial.GetProperty("roadSectionId").GetGuid();
        var initialVersionId = initial.GetProperty("roadSectionVersionId").GetGuid();
        var request = new
        {
            srid = 32648,
            coordinates = new[] { new { x = 500010d, y = 1100010d }, new { x = 500200d, y = 1100200d } },
            effectiveFrom = "2026-10-01T08:00:00+07:00",
            changeReason = "Corrected surveyed alignment",
            expectedCurrentVersionId = initialVersionId,
            operationId = Guid.NewGuid()
        };

        var versioned = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions",
            request);

        versioned.StatusCode.Should().Be(HttpStatusCode.Created);
        var versionedBodyText = await versioned.Content.ReadAsStringAsync();
        using var versionBody = JsonDocument.Parse(versionedBodyText);
        var version = versionBody.RootElement;
        var nextVersionId = version.GetProperty("roadSectionVersionId").GetGuid();
        version.GetProperty("versionNo").GetInt32().Should().Be(2);
        version.GetProperty("isCurrent").GetBoolean().Should().BeTrue();
        versioned.Headers.Location!.OriginalString.Should().Be(
            $"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{nextVersionId}");

        var replay = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions",
            request);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(versionedBodyText);

        var stale = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions",
            new
            {
                srid = 32648,
                coordinates = new[] { new { x = 500020d, y = 1100020d }, new { x = 500300d, y = 1100300d } },
                effectiveFrom = "2026-11-01T08:00:00+07:00",
                changeReason = "Stale update",
                expectedCurrentVersionId = initialVersionId,
                operationId = Guid.NewGuid()
            });
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("road_section_concurrency_conflict");

        await using var verification = _sql.CreateDbContext();
        (await verification.RoadSectionVersions.SingleAsync(version => version.Id == initialVersionId)).IsCurrent.Should().BeFalse();
        (await verification.RoadSectionVersions.SingleAsync(version => version.Id == nextVersionId)).IsCurrent.Should().BeTrue();
        var versionAudit = await verification.AuditLogs.SingleAsync(audit =>
            audit.EntityId == nextVersionId && audit.EventType == "road_section_version_created");
        versionAudit.Reason.Should().Be("ROAD_SECTION_VERSION_CREATED");
        versionAudit.AfterSnapshot.Should().NotContain(request.changeReason);
        versionAudit.AfterSnapshot.Should().NotContain("geometry").And.NotContain("coordinates");
        (await verification.RoadSectionVersions.CountAsync(candidate => candidate.RoadSectionId == roadSectionId)).Should().Be(2);
        (await verification.AuditLogs.CountAsync(audit =>
            audit.EntityId == nextVersionId && audit.EventType == "road_section_version_created")).Should().Be(1);
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.Operation == "RoadSectionVersionCreated" && record.IdempotencyKey == request.operationId.ToString("N"))).Should().Be(1);
    }

    [Fact]
    public async Task CreateRoadSectionRejectsGeometrySridThatDiffersFromProject()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"srid_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"srid_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);

        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32649,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Invalid SRID",
            operationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("validation_error");
        await using var verification = _sql.CreateDbContext();
        (await verification.RoadSections.CountAsync(section => section.ProjectId == projectId)).Should().Be(0);
    }

    [Fact]
    public async Task AddRoadSectionVersionConcurrentRequestsReturnOneCreatedAndOneStaleConflict()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"race_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"race_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        var created = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var initial = await created.Content.ReadFromJsonAsync<JsonElement>();
        var roadSectionId = initial.GetProperty("roadSectionId").GetGuid();
        var expectedCurrentVersionId = initial.GetProperty("roadSectionVersionId").GetGuid();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions", new
            {
                srid = 32648,
                coordinates = new[] { new { x = 500010d, y = 1100010d }, new { x = 500200d, y = 1100200d } },
                effectiveFrom = "2026-10-01T08:00:00+07:00",
                changeReason = "Concurrent alignment A",
                expectedCurrentVersionId,
                operationId = Guid.NewGuid()
            }),
            client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions", new
            {
                srid = 32648,
                coordinates = new[] { new { x = 500020d, y = 1100020d }, new { x = 500300d, y = 1100300d } },
                effectiveFrom = "2026-10-02T08:00:00+07:00",
                changeReason = "Concurrent alignment B",
                expectedCurrentVersionId,
                operationId = Guid.NewGuid()
            }));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("road_section_concurrency_conflict");
    }

    [Fact]
    public async Task CreateRoadSectionRejectsProjectManagerWithoutPersistingData()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"authorization_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"authorization_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);
        await AuthenticateAsync(client, projectManager.UserName!, "Current1!");

        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Unauthorized create",
            operationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("access_forbidden");
        await using var verification = _sql.CreateDbContext();
        (await verification.RoadSections.CountAsync(section => section.ProjectId == projectId)).Should().Be(0);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Guid projectManagerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P-{Guid.NewGuid():N}",
            name = "Road section test project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = projectManagerId,
            handover = new
            {
                documentNo = $"HD-{Guid.NewGuid():N}",
                handoverDate = "2026-08-31"
            },
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("projectId").GetGuid();
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
    }
}
