using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authorization;

[Trait("TaskId", "P1-12")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P112ProjectAuthorizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P112ProjectAuthorizationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task WorkPackage_Unauthenticated_ReturnsStableUnauthorizedProblem()
    {
        var project = await CreateProjectAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.GetAsync($"/api/v1/projects/{project.Id}/work-package");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ProblemCodeAsync(response)).Should().Be(ApiErrorCodes.Unauthorized);
    }

    [Theory]
    [InlineData(UserRoleCode.DroneOperator, true)]
    [InlineData(UserRoleCode.Supervisor, false)]
    public async Task WorkPackage_CurrentAssignedMemberOrSupervisor_ReturnsProjectedData(
        UserRoleCode role,
        bool createMembership)
    {
        var user = await _sql.CreateUserAsync($"scope_{role}_{Guid.NewGuid():N}", "Current1!", role);
        var project = await CreateProjectAsync();
        var children = await CreateWorkPackageChildrenAsync(project);
        if (createMembership)
        {
            await CreateMembershipAsync(project.Id, user.Id, role);
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user.UserName!, "Current1!");

        var response = await client.GetAsync($"/api/v1/projects/{project.Id}/work-package");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("projectId").GetGuid().Should().Be(project.Id);
        body.GetProperty("projectCode").GetString().Should().Be(project.ProjectCode);
        body.GetProperty("name").GetString().Should().Be(project.Name);
        body.GetProperty("accessRole").GetString().Should().Be(role.ToDbCode());
        var roadSection = body.GetProperty("roadSections").EnumerateArray().Single();
        roadSection.GetProperty("roadSectionId").GetGuid().Should().Be(children.SectionId);
        roadSection.GetProperty("currentVersionId").GetGuid().Should().Be(children.VersionId);
        roadSection.GetProperty("versionNo").GetInt32().Should().Be(1);
        roadSection.GetProperty("geometryWkt").GetString().Should().Be("LINESTRING (500000 1000000, 500100 1000100)");
        roadSection.GetProperty("srid").GetInt32().Should().Be(32648);

        var warranty = body.GetProperty("warranties").EnumerateArray().Single();
        warranty.GetProperty("warrantyId").GetGuid().Should().Be(children.WarrantyId);
        warranty.GetProperty("roadSectionId").GetGuid().Should().Be(children.SectionId);
        warranty.GetProperty("scope").GetString().Should().Be("ROAD_SECTION");
        warranty.GetProperty("status").GetString().Should().Be("ACTIVE");

        if (role == UserRoleCode.Supervisor)
        {
            var missing = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/work-package");
            missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ProblemCodeAsync(missing)).Should().Be("project_not_found");
        }
    }

    [Fact]
    public async Task WorkPackage_EmptyCollections_PreservesHttpShapeAndBusinessRows()
    {
        var user = await _sql.CreateUserAsync($"empty_package_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = await CreateProjectAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user.UserName!, "Current1!");

        byte[] beforeVersion;
        await using (var before = _sql.CreateDbContext())
        {
            beforeVersion = await before.Projects.Where(item => item.Id == project.Id)
                .Select(item => item.RowVersion).SingleAsync();
        }

        var correlationId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{project.Id}/work-package");
        request.Headers.Add("X-Correlation-ID", correlationId.ToString());
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.GetValues("X-Correlation-ID").Single().Should().Be(correlationId.ToString());
        response.Headers.Contains("ETag").Should().BeFalse();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("projectId").GetGuid().Should().Be(project.Id);
        body.GetProperty("description").GetString().Should().Be(project.Description);
        body.GetProperty("engineeringUtmSrid").GetInt32().Should().Be(32648);
        body.GetProperty("status").GetString().Should().Be("ACTIVE");
        body.GetProperty("startDate").GetString().Should().Be("2026-01-01");
        body.GetProperty("endDate").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("accessRole").GetString().Should().Be("SUPERVISOR");
        body.GetProperty("rowVersion").GetString().Should().Be(Convert.ToBase64String(beforeVersion));
        body.GetProperty("roadSections").GetArrayLength().Should().Be(0);
        body.GetProperty("warranties").GetArrayLength().Should().Be(0);

        await using var after = _sql.CreateDbContext();
        (await after.Projects.Where(item => item.Id == project.Id).Select(item => item.RowVersion).SingleAsync())
            .Should().Equal(beforeVersion);
        (await after.RoadSections.CountAsync(item => item.ProjectId == project.Id)).Should().Be(0);
        (await after.Warranties.CountAsync(item => item.ProjectId == project.Id)).Should().Be(0);
    }

    [Fact]
    public async Task WorkPackage_MultipleChildren_PreservesOrderingNullableProjectionAndBusinessRows()
    {
        var user = await _sql.CreateUserAsync($"multi_package_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = await CreateProjectAsync();
        var later = await CreateWorkPackageChildrenAsync(
            project, "B-road", Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var earlier = await CreateWorkPackageChildrenAsync(
            project, "A-road", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var projectWarranty = Warranty.Create(
            Guid.NewGuid(), project.Id, null, null,
            new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), new DateOnly(2027, 2, 1),
            null, WarrantyScope.Project, null, null, WarrantyStatus.Active);
        await using (var setup = _sql.CreateDbContext())
        {
            setup.Warranties.Add(projectWarranty);
            await setup.SaveChangesAsync();
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user.UserName!, "Current1!");
        await using var before = _sql.CreateDbContext();
        var beforeProjectVersion = await before.Projects.Where(item => item.Id == project.Id)
            .Select(item => item.RowVersion).SingleAsync();
        var beforeSections = await before.RoadSections.Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.Code).Select(item => new { item.Id, item.Code, item.Name }).ToArrayAsync();
        var beforeVersions = await before.RoadSectionVersions
            .Where(item => item.RoadSectionId == earlier.SectionId || item.RoadSectionId == later.SectionId)
            .OrderBy(item => item.Id).Select(item => new { item.Id, item.VersionNo, item.IsCurrent, item.ChangeReason })
            .ToArrayAsync();
        var beforeWarranties = await before.Warranties.Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.RoadSectionId, item.WarrantyStartDate, item.RetainedValue, item.Status })
            .ToArrayAsync();
        var beforeOutboxCount = await before.OutboxMessages.CountAsync();
        using var response = await client.GetAsync($"/api/v1/projects/{project.Id}/work-package");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var sections = body.GetProperty("roadSections").EnumerateArray().ToArray();
        sections.Select(item => item.GetProperty("code").GetString()).Should().Equal("A-road", "B-road");
        sections.Select(item => item.GetProperty("roadSectionId").GetGuid()).Should()
            .Equal(earlier.SectionId, later.SectionId);
        sections.Select(item => item.GetProperty("currentVersionId").GetGuid()).Should()
            .Equal(earlier.VersionId, later.VersionId);
        var warranties = body.GetProperty("warranties").EnumerateArray().ToArray();
        warranties.Select(item => item.GetProperty("warrantyId").GetGuid()).Should()
            .Equal(earlier.WarrantyId, later.WarrantyId, projectWarranty.Id);
        var nullable = warranties[2];
        nullable.GetProperty("roadSectionId").ValueKind.Should().Be(JsonValueKind.Null);
        nullable.GetProperty("handoverDocumentId").ValueKind.Should().Be(JsonValueKind.Null);
        nullable.GetProperty("retainedValue").ValueKind.Should().Be(JsonValueKind.Null);
        nullable.GetProperty("terms").ValueKind.Should().Be(JsonValueKind.Null);
        nullable.GetProperty("scope").GetString().Should().Be("PROJECT");

        await using var after = _sql.CreateDbContext();
        (await after.Projects.Where(item => item.Id == project.Id).Select(item => item.RowVersion).SingleAsync())
            .Should().Equal(beforeProjectVersion);
        (await after.RoadSections.Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.Code).Select(item => new { item.Id, item.Code, item.Name }).ToArrayAsync())
            .Should().Equal(beforeSections);
        (await after.RoadSectionVersions
            .Where(item => item.RoadSectionId == earlier.SectionId || item.RoadSectionId == later.SectionId)
            .OrderBy(item => item.Id).Select(item => new { item.Id, item.VersionNo, item.IsCurrent, item.ChangeReason })
            .ToArrayAsync()).Should().Equal(beforeVersions);
        (await after.Warranties.Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.RoadSectionId, item.WarrantyStartDate, item.RetainedValue, item.Status })
            .ToArrayAsync()).Should().Equal(beforeWarranties);
        (await after.OutboxMessages.CountAsync()).Should().Be(beforeOutboxCount);
    }

    [Fact]
    public async Task WorkPackage_MembershipEnds_IsForbiddenOnNextRequest()
    {
        var user = await _sql.CreateUserAsync(
            $"membership_change_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.RepairCrew);
        var project = await CreateProjectAsync();
        var membership = await CreateMembershipAsync(project.Id, user.Id, UserRoleCode.RepairCrew);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user.UserName!, "Current1!");

        (await client.GetAsync($"/api/v1/projects/{project.Id}/work-package"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var context = _sql.CreateDbContext())
        {
            var persisted = await context.ProjectMembers.SingleAsync(item => item.Id == membership.Id);
            persisted.Status = ProjectMemberStatus.Ended;
            await context.SaveChangesAsync();
        }

        var denied = await client.GetAsync($"/api/v1/projects/{project.Id}/work-package");
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(denied)).Should().Be("project_access_forbidden");

        await using (var context = _sql.CreateDbContext())
        {
            var persisted = await context.ProjectMembers.SingleAsync(item => item.Id == membership.Id);
            persisted.Status = ProjectMemberStatus.Active;
            persisted.ValidTo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            await context.SaveChangesAsync();
        }

        var expired = await client.GetAsync($"/api/v1/projects/{project.Id}/work-package");
        expired.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(expired)).Should().Be("project_access_forbidden");

        var otherProject = await CreateProjectAsync();
        var crossProject = await client.GetAsync($"/api/v1/projects/{otherProject.Id}/work-package");
        crossProject.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(crossProject)).Should().Be("project_access_forbidden");

        var mismatchedUser = await _sql.CreateUserAsync(
            $"membership_mismatch_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.DroneOperator);
        await CreateMembershipAsync(project.Id, mismatchedUser.Id, UserRoleCode.ProjectManager);
        using var mismatchedClient = CreateClient(factory);
        var mismatchedToken = await AuthenticateAsync(mismatchedClient, mismatchedUser.UserName!, "Current1!");
        var mismatch = await mismatchedClient.GetAsync($"/api/v1/projects/{project.Id}/work-package");
        mismatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(mismatch)).Should().Be("project_access_forbidden");

        mismatchedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AddForgedProjectClaim(mismatchedToken, otherProject.Id));
        var forgedProjectClaim = await mismatchedClient.GetAsync(
            $"/api/v1/projects/{otherProject.Id}/work-package");
        forgedProjectClaim.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemCodeAsync(forgedProjectClaim)).Should().Be("project_access_forbidden");
    }

    private async Task<Project> CreateProjectAsync()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P-{Guid.NewGuid():N}",
            Name = "Authorization fixture project",
            Description = "Scoped work-package fixture",
            EngineeringUtmSrid = 32648,
            Status = ProjectStatus.Active,
            StartDate = new DateOnly(2026, 1, 1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var context = _sql.CreateDbContext();
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<ProjectMember> CreateMembershipAsync(Guid projectId, Guid userId, UserRoleCode role)
    {
        var membership = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleCode = role,
            IsPrimary = role == UserRoleCode.ProjectManager,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            Status = ProjectMemberStatus.Active
        };

        await using var context = _sql.CreateDbContext();
        context.ProjectMembers.Add(membership);
        await context.SaveChangesAsync();
        return membership;
    }

    private async Task<(Guid SectionId, Guid VersionId, Guid WarrantyId)> CreateWorkPackageChildrenAsync(
        Project project,
        string? sectionCode = null,
        Guid? warrantyId = null)
    {
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, sectionCode ?? $"RS-{Guid.NewGuid():N}", "Main road");
        var geometry = new LineString(
        [
            new Coordinate(500000, 1000000),
            new Coordinate(500100, 1000100)
        ])
        {
            SRID = 32648
        };
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(),
            section.Id,
            1,
            true,
            geometry,
            DateTimeOffset.UtcNow,
            "Initial geometry");
        var warranty = Warranty.Create(
            warrantyId ?? Guid.NewGuid(),
            project.Id,
            section.Id,
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            1000m,
            WarrantyScope.RoadSection,
            "Road warranty",
            null,
            WarrantyStatus.Active);

        await using var context = _sql.CreateDbContext();
        context.RoadSections.Add(section);
        await context.SaveChangesAsync();
        context.RoadSectionVersions.Add(version);
        context.Warranties.Add(warranty);
        await context.SaveChangesAsync();
        return (section.Id, version.Id, warranty.Id);
    }

    private static HttpClient CreateClient(AuthenticationWebApplicationFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task<string> AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        return accessToken;
    }

    private static string AddForgedProjectClaim(string accessToken, Guid projectId)
    {
        var original = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var claims = original.Claims.Append(new Claim("project_id", projectId.ToString()));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(AuthenticationWebApplicationFactory.CurrentSigningKey)
            {
                KeyId = AuthenticationWebApplicationFactory.CurrentKeyId
            },
            SecurityAlgorithms.HmacSha256);
        var forged = new JwtSecurityToken(
            AuthenticationWebApplicationFactory.TestIssuer,
            AuthenticationWebApplicationFactory.TestAudience,
            claims,
            original.ValidFrom,
            original.ValidTo,
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(forged);
    }

    private static async Task<string> ProblemCodeAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized
                ? "application/problem+json" : "application/json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be((int)response.StatusCode);
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out var correlationId)
            .Should().BeTrue();
        body.GetProperty("correlationId").GetString().Should().Be(correlationId.ToString());
        return body.GetProperty("code").GetString()!;
    }
}
