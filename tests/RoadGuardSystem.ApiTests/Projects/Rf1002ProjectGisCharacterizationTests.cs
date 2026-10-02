using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Trait("TaskId", "RF-10-02-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1002ProjectGisCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1002ProjectGisCharacterizationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task RoadVersionProductionPath_RoundTripsSqlSpatialFactsAndPreservesPriorVersion()
    {
        var supervisor = await _sql.CreateUserAsync(
            $"rf1002_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        var projectManager = await _sql.CreateUserAsync(
            $"rf1002_pm_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");
        var projectId = await CreateProjectAsync(client, projectManager.Id);

        var initialResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RF1002-{Guid.NewGuid():N}",
            name = "RF-10-02 characterization road",
            srid = 32648,
            coordinates = new[]
            {
                new { x = 500000d, y = 1100000d },
                new { x = 500100d, y = 1100100d }
            },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "RF-10-02 initial geometry",
            operationId = Guid.NewGuid()
        });
        initialResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var initial = await initialResponse.Content.ReadFromJsonAsync<JsonElement>();
        var roadSectionId = initial.GetProperty("roadSectionId").GetGuid();
        var initialVersionId = initial.GetProperty("roadSectionVersionId").GetGuid();

        var initialGeometryFacts = await ReadGeometryFactsAsync(initialVersionId);
        initialGeometryFacts.Type.Should().Be("LineString");
        initialGeometryFacts.Srid.Should().Be(32648);
        initialGeometryFacts.Coordinates.Should().Equal([(500000d, 1100000d), (500100d, 1100100d)]);

        await using (var firstRead = _sql.CreateDbContext())
        {
            var version = await firstRead.RoadSectionVersions.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == initialVersionId);
            version.RoadSectionId.Should().Be(roadSectionId);
            version.VersionNo.Should().Be(1);
            version.IsCurrent.Should().BeTrue();
            version.Geometry.SRID.Should().Be(32648);
            version.Geometry.GeometryType.Should().Be(initialGeometryFacts.Type);
            version.Geometry.SRID.Should().Be(initialGeometryFacts.Srid);
            version.Geometry.Coordinates.Select(point => (point.X, point.Y)).Should().Equal(initialGeometryFacts.Coordinates);
        }

        var nextResponse = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions",
            new
            {
                srid = 32648,
                coordinates = new[]
                {
                    new { x = 500010d, y = 1100010d },
                    new { x = 500200d, y = 1100200d }
                },
                effectiveFrom = "2026-10-01T08:00:00+07:00",
                changeReason = "RF-10-02 replacement geometry",
                expectedCurrentVersionId = initialVersionId,
                operationId = Guid.NewGuid()
            });
        nextResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var next = await nextResponse.Content.ReadFromJsonAsync<JsonElement>();
        var nextVersionId = next.GetProperty("roadSectionVersionId").GetGuid();

        await using var finalRead = _sql.CreateDbContext();
        var versions = await finalRead.RoadSectionVersions.AsNoTracking()
            .Where(candidate => candidate.RoadSectionId == roadSectionId)
            .OrderBy(candidate => candidate.VersionNo)
            .ToListAsync();
        versions.Should().HaveCount(2);
        versions[0].Id.Should().Be(initialVersionId);
        versions[0].IsCurrent.Should().BeFalse();
        versions[0].Geometry.GeometryType.Should().Be(initialGeometryFacts.Type);
        versions[0].Geometry.SRID.Should().Be(initialGeometryFacts.Srid);
        versions[0].Geometry.Coordinates.Select(point => (point.X, point.Y)).Should().Equal(initialGeometryFacts.Coordinates);
        versions[1].Id.Should().Be(nextVersionId);
        versions[1].VersionNo.Should().Be(2);
        versions[1].IsCurrent.Should().BeTrue();
        versions[1].Geometry.SRID.Should().Be(32648);
        versions[1].Geometry.GeometryType.Should().Be("LineString");
        versions[1].Geometry.Coordinates.Select(point => (point.X, point.Y)).Should()
            .Equal([(500010d, 1100010d), (500200d, 1100200d)]);
    }

    private async Task<GeometryFacts> ReadGeometryFactsAsync(Guid versionId)
    {
        await using var context = _sql.CreateDbContext();
        var geometry = await context.RoadSectionVersions.AsNoTracking()
            .Where(candidate => candidate.Id == versionId)
            .Select(candidate => candidate.Geometry)
            .SingleAsync();
        return new GeometryFacts(
            geometry.GeometryType,
            geometry.SRID,
            geometry.Coordinates.Select(point => (point.X, point.Y)).ToArray());
    }

    private sealed record GeometryFacts(string Type, int Srid, (double X, double Y)[] Coordinates);

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Guid projectManagerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"RF1002-P-{Guid.NewGuid():N}",
            name = "RF-10-02 characterization project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = projectManagerId,
            handover = new
            {
                documentNo = $"RF1002-HD-{Guid.NewGuid():N}",
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
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = AuthenticationSqlServerFixture.EmailFor(username), password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
    }
}
