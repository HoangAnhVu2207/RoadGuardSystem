using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.ApiTests.Inspections;

[Trait("TaskId", "V2-P1-063")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class V2P1063InspectionTaskListTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 32648);
    private readonly AuthenticationSqlServerFixture _sql;

    public V2P1063InspectionTaskListTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task ListAssignedTasks_ReturnsOnlyCurrentCrewScope_AndDoesNotMutateSql()
    {
        var crew = await _sql.CreateUserAsync($"p1063_crew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"p1063_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var visible = await SeedTaskAsync(crew.Id, pm.Id, addMembership: true);
        var hidden = await SeedTaskAsync(crew.Id, pm.Id, addMembership: false);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, crew.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks?limit=100");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("items").GetArrayLength().Should().Be(1);
        var item = body.GetProperty("items")[0];
        item.GetProperty("id").GetGuid().Should().Be(visible.TaskId);
        item.GetProperty("projectId").GetGuid().Should().Be(visible.ProjectId);
        item.GetProperty("defectIds")[0].GetGuid().Should().Be(visible.DefectId);
        item.GetProperty("mode").GetString().Should().Be("MEASURE_ONLY");
        item.GetProperty("crewId").GetGuid().Should().Be(crew.Id);
        item.GetProperty("policyVersionId").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("version").GetString().Should().Be(visible.TaskCode);
        body.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("asOf").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        await using var verification = _sql.CreateDbContext();
        (await verification.FieldInspectionTasks.CountAsync(task => task.Id == visible.TaskId)).Should().Be(1);
        (await verification.FieldInspectionTasks.CountAsync(task => task.Id == hidden.TaskId)).Should().Be(1);
        (await verification.FieldInspectionAssignments.CountAsync(assignment =>
            assignment.FieldInspectionTaskId == visible.TaskId && assignment.Status == FieldInspectionAssignmentStatus.Active)).Should().Be(1);
    }

    [Fact]
    public async Task ListAssignedTasks_NonCrew_IsForbidden()
    {
        var pm = await _sql.CreateUserAsync($"p1063_forbidden_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, pm.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<SeededInspectionTask> SeedTaskAsync(Guid crewId, Guid pmId, bool addMembership)
    {
        await using var context = _sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), $"P1063-{Guid.NewGuid():N}", "P1-063 inspection project", null, 32648,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"R-{Guid.NewGuid():N}");
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            GeometryFactory.CreateLineString([new Coordinate(500000, 1100000), new Coordinate(500100, 1100100)]), now, "P1-063 fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var defectType = DefectType.Create($"P1063-DT-{Guid.NewGuid():N}", "P1-063 fixture defect");
        var cause = CauseCategory.Create($"P1063-CC-{Guid.NewGuid():N}", "P1-063 fixture cause");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, version.Id, null, defectType.Code, cause.Code,
            DefectSeverity.Medium, DefectStatus.Open, GeometryFactory.CreatePoint(new Coordinate(500050, 1100050)), now);
        var task = FieldInspectionTask.Create(Guid.NewGuid(), $"P1063-TASK-{Guid.NewGuid():N}", project.Id, defect.Id,
            survey.Id, version.Id, 1, "{\"points\":[1]}", null, null, now.AddDays(1), FieldInspectionTaskStatus.Accepted,
            pmId, null, null, null, null);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crewId, pmId, now, null,
            FieldInspectionAssignmentStatus.Active, null);

        context.AddRange(project, road, version, survey, defectType, cause, defect, task, assignment);
        if (addMembership)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = crewId,
                RoleCode = UserRoleCode.RepairCrew,
                IsPrimary = false,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
                Status = ProjectMemberStatus.Active
            });
        }

        await context.SaveChangesAsync();
        return new(project.Id, task.Id, defect.Id, task.TaskCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.GetProperty("accessToken").GetString());
    }

    private sealed record SeededInspectionTask(Guid ProjectId, Guid TaskId, Guid DefectId, string TaskCode);
}
