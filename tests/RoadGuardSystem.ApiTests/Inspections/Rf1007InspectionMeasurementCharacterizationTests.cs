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

/// <summary>
/// RF-10-07-C01: Characterization of inspection task list endpoint.
/// Verifies current behavior: RepairCrew role authorization, project scope filtering,
/// response projection, and read-side SQL immutability.
/// </summary>
[Trait("TaskId", "RF-10-07-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1007InspectionMeasurementCharacterizationTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 32648);
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1007InspectionMeasurementCharacterizationTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task InspectionTaskList_RepairCrewWithScope_ReturnsTasksAndDoesNotMutateDatabase()
    {
        var crew = await _sql.CreateUserAsync($"rf1007_crew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"rf1007_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await SeedInspectionScopeAsync(crew.Id, pm.Id, addMembership: true);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await AuthenticateAsync(client, crew.UserName!, "Current1!");

        await using var beforeContext = _sql.CreateDbContext();
        var taskBefore = await beforeContext.FieldInspectionTasks.AsNoTracking()
            .SingleAsync(t => t.Id == scope.TaskId);
        var assignmentBefore = await beforeContext.FieldInspectionAssignments.AsNoTracking()
            .SingleAsync(a => a.FieldInspectionTaskId == scope.TaskId);
        var defectBefore = await beforeContext.Defects.AsNoTracking()
            .SingleAsync(d => d.Id == scope.DefectId);
        var measurementBefore = await beforeContext.GroundTruthMeasurements.AsNoTracking()
            .SingleAsync(m => m.FieldInspectionSessionId == scope.SessionId);

        var response = await client.GetAsync("/api/v1/me/inspection-tasks?limit=50");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterOrEqualTo(1);
        var item = body.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetGuid() == scope.TaskId);
        item.GetProperty("projectId").GetGuid().Should().Be(scope.ProjectId);
        item.GetProperty("defectIds")[0].GetGuid().Should().Be(scope.DefectId);
        item.GetProperty("mode").GetString().Should().Be("MEASURE_ONLY");
        item.GetProperty("crewId").GetGuid().Should().Be(crew.Id);
        item.GetProperty("policyVersionId").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("status").GetString().Should().Be("ACCEPTED");
        item.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("asOf").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        await using var afterContext = _sql.CreateDbContext();
        var taskAfter = await afterContext.FieldInspectionTasks.AsNoTracking()
            .SingleAsync(t => t.Id == scope.TaskId);
        var assignmentAfter = await afterContext.FieldInspectionAssignments.AsNoTracking()
            .SingleAsync(a => a.FieldInspectionTaskId == scope.TaskId);
        var defectAfter = await afterContext.Defects.AsNoTracking()
            .SingleAsync(d => d.Id == scope.DefectId);
        var measurementAfter = await afterContext.GroundTruthMeasurements.AsNoTracking()
            .SingleAsync(m => m.FieldInspectionSessionId == scope.SessionId);

        taskAfter.Id.Should().Be(taskBefore.Id);
        taskAfter.Status.Should().Be(taskBefore.Status);
        taskAfter.TaskCode.Should().Be(taskBefore.TaskCode);
        taskAfter.ProjectId.Should().Be(taskBefore.ProjectId);
        taskAfter.DefectId.Should().Be(taskBefore.DefectId);
        taskAfter.RowVersion.Should().Equal(taskBefore.RowVersion);
        assignmentAfter.Id.Should().Be(assignmentBefore.Id);
        assignmentAfter.Status.Should().Be(assignmentBefore.Status);
        assignmentAfter.AssignedToUserId.Should().Be(assignmentBefore.AssignedToUserId);
        assignmentAfter.AssignedByUserId.Should().Be(assignmentBefore.AssignedByUserId);
        assignmentAfter.AssignedAt.Should().Be(assignmentBefore.AssignedAt);
        defectAfter.Id.Should().Be(defectBefore.Id);
        defectAfter.Status.Should().Be(defectBefore.Status);
        defectAfter.ProjectId.Should().Be(defectBefore.ProjectId);
        defectAfter.RoadSectionVersionId.Should().Be(defectBefore.RoadSectionVersionId);
        measurementAfter.Id.Should().Be(measurementBefore.Id);
        measurementAfter.DefectId.Should().Be(measurementBefore.DefectId);
        measurementAfter.SurveyId.Should().Be(measurementBefore.SurveyId);
        measurementAfter.RoadSectionVersionId.Should().Be(measurementBefore.RoadSectionVersionId);
        measurementAfter.FieldInspectionSessionId.Should().Be(measurementBefore.FieldInspectionSessionId);
        measurementAfter.MeasurementType.Should().Be(measurementBefore.MeasurementType);
        measurementAfter.Value.Should().Be(measurementBefore.Value);
        measurementAfter.Unit.Should().Be(measurementBefore.Unit);
        Assert.NotNull(measurementAfter.Location);
        Assert.NotNull(measurementBefore.Location);
        measurementAfter.Location.SRID.Should().Be(measurementBefore.Location.SRID);
        measurementAfter.Location.Coordinate.X.Should().Be(measurementBefore.Location.Coordinate.X);
        measurementAfter.Location.Coordinate.Y.Should().Be(measurementBefore.Location.Coordinate.Y);
    }

    [Fact]
    public async Task InspectionTaskList_ProjectManagerRole_ReturnsForbidden()
    {
        var pm = await _sql.CreateUserAsync($"rf1007_pm_forbidden_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, pm.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task InspectionTaskList_SupervisorRole_ReturnsForbidden()
    {
        var supervisor = await _sql.CreateUserAsync($"rf1007_sup_forbidden_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, supervisor.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InspectionTaskList_CrewWithoutProjectMembership_FiltersOutTask()
    {
        var crew = await _sql.CreateUserAsync($"rf1007_no_membership_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"rf1007_no_membership_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await SeedInspectionScopeAsync(crew.Id, pm.Id, addMembership: false);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks?limit=100");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var taskIds = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();
        taskIds.Should().NotContain(scope.TaskId);
    }

    [Fact]
    public async Task InspectionTaskList_MeasurementProvenance_ExistsInDatabase()
    {
        var crew = await _sql.CreateUserAsync($"rf1007_measurement_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"rf1007_measurement_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await SeedInspectionScopeAsync(crew.Id, pm.Id, addMembership: true);

        await using var context = _sql.CreateDbContext();
        var measurement = await context.GroundTruthMeasurements.AsNoTracking()
            .SingleAsync(m => m.FieldInspectionSessionId == scope.SessionId);

        measurement.DefectId.Should().Be(scope.DefectId);
        measurement.SurveyId.Should().Be(scope.SurveyId);
        measurement.RoadSectionVersionId.Should().Be(scope.RoadSectionVersionId);
        measurement.MeasurementType.Should().Be(MeasurementType.DepressionDepth);
        measurement.Value.Should().BeGreaterThan(0);
        measurement.Unit.Should().Be("mm");
        Assert.NotNull(measurement.Location);
        measurement.Location.SRID.Should().Be(4326);
    }

    private async Task<InspectionScope> SeedInspectionScopeAsync(
        Guid crewId,
        Guid pmId,
        bool addMembership)
    {
        await using var context = _sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), $"RF1007-{Guid.NewGuid():N}", "RF-10-07-C01 fixture", null, 32648,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            GeometryFactory.CreateLineString([new Coordinate(500000, 1100000), new Coordinate(500100, 1100100)]), now, "RF-10-07 fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var defectType = DefectType.Create($"RF1007-DT-{Guid.NewGuid():N}", "RF-10-07 defect");
        var cause = CauseCategory.Create($"RF1007-CC-{Guid.NewGuid():N}", "RF-10-07 cause");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, version.Id, null, defectType.Code, cause.Code,
            DefectSeverity.Medium, DefectStatus.Open, GeometryFactory.CreatePoint(new Coordinate(500050, 1100050)), now);
        var task = FieldInspectionTask.Create(Guid.NewGuid(), $"TASK-{Guid.NewGuid():N}", project.Id, defect.Id,
            survey.Id, version.Id, 1, "{\"points\":[1]}", null, null, now.AddDays(1), FieldInspectionTaskStatus.Accepted,
            pmId, null, null, null, null);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crewId, pmId, now, null,
            FieldInspectionAssignmentStatus.Active, null);
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.DefectVerification,
            task.Id, project.Id, version.Id, survey.Id, $"SESSION-{Guid.NewGuid():N}", crewId, "Crew Name",
            now, "clear", "depth gauge", FieldInspectionSessionStatus.Completed, null);
        var measurement = GroundTruthMeasurement.Create(Guid.NewGuid(), session.Id, "sample-1", version.Id,
            survey.Id, defect.Id, MeasurementType.DepressionDepth, 15.5m, "mm",
            new Point(new Coordinate(106.7, 10.8)) { SRID = 4326 }, "depth gauge", "DG-001",
            "straightedge baseline", "Crew Name", now, null, "RF-10-07 characterization fixture");

        context.AddRange(project, road, version, survey, defectType, cause, defect, task, assignment, session, measurement);
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
        return new(project.Id, task.Id, defect.Id, version.Id, survey.Id, session.Id);
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

    private sealed record InspectionScope(
        Guid ProjectId,
        Guid TaskId,
        Guid DefectId,
        Guid RoadSectionVersionId,
        Guid SurveyId,
        Guid SessionId);
}
