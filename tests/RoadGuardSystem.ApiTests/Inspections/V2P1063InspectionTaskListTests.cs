using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
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
        item.GetProperty("version").GetString().Should().Be(Convert.ToBase64String(visible.RowVersion));
        body.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("asOf").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        await using var verification = _sql.CreateDbContext();
        (await verification.FieldInspectionTasks.CountAsync(task => task.Id == visible.TaskId)).Should().Be(1);
        (await verification.FieldInspectionTasks.CountAsync(task => task.Id == hidden.TaskId)).Should().Be(1);
        (await verification.FieldInspectionAssignments.CountAsync(assignment =>
            assignment.FieldInspectionTaskId == visible.TaskId && assignment.Status == FieldInspectionAssignmentStatus.Active)).Should().Be(1);
    }

    [Fact]
    public async Task ListAssignedTasks_SameProject_DifferentAssignment_Isolated()
    {
        var crew = await _sql.CreateUserAsync($"p1063_same_project_crew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var otherCrew = await _sql.CreateUserAsync($"p1063_other_crew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"p1063_same_project_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var visible = await SeedTaskAsync(crew.Id, pm.Id, addMembership: true);
        var hidden = await SeedTaskAsync(crew.Id, pm.Id, addMembership: true, assignmentCrewId: otherCrew.Id,
            existingProjectId: visible.ProjectId);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks?limit=100");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().ContainSingle().Which.Should().Be(visible.TaskId);
        hidden.TaskId.Should().NotBe(visible.TaskId);
    }

    [Fact]
    public async Task ListAssignedTasks_DifferentProject_Isolated()
    {
        var crew = await _sql.CreateUserAsync($"p1063_cross_project_crew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"p1063_cross_project_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var visible = await SeedTaskAsync(crew.Id, pm.Id, addMembership: true);
        var hidden = await SeedTaskAsync(crew.Id, pm.Id, addMembership: false, assignmentCrewId: crew.Id);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");

        var response = await client.GetAsync("/api/v1/me/inspection-tasks?limit=100");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().ContainSingle().Which.Should().Be(visible.TaskId);
        hidden.ProjectId.Should().NotBe(visible.ProjectId);
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

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("wrong-role")]
    public async Task Huy02MembershipChangedAfterLoginFiltersTask(string change)
    {
        var crew = await _sql.CreateUserAsync($"h02_mem_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"h02_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var task = await SeedTaskAsync(crew.Id, pm.Id, true);
        var clock = new FixedTimeProvider(DateTimeOffset.UtcNow.AddDays(10));
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IProjectScopeGuard>();
                services.AddScoped<IProjectScopeGuard>(sp => new ProjectScopeGuard(
                    sp.GetRequiredService<IProjectMembershipRepository>(), clock));
            });
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");
        await using (var db = _sql.CreateDbContext())
        {
            var membership = await db.ProjectMembers.SingleAsync(x => x.ProjectId == task.ProjectId && x.UserId == crew.Id);
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (change == "revoked") membership.Status = ProjectMemberStatus.Ended;
            if (change == "expired") membership.ValidTo = today.AddDays(-1);
            if (change == "future") membership.ValidFrom = today.AddDays(1);
            if (change == "wrong-role") membership.RoleCode = UserRoleCode.DroneOperator;
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/api/v1/me/inspection-tasks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("session")]
    public async Task Huy02AuthorityRevokedAfterLoginUsesRealAuthentication(string change)
    {
        var crew = await _sql.CreateUserAsync($"h02_auth_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");
        await using (var db = _sql.CreateDbContext())
        {
            if (change == "user") (await db.Users.SingleAsync(x => x.Id == crew.Id)).Status = UserStatus.Suspended;
            if (change == "role") (await db.Roles.SingleAsync(x => x.Code == UserRoleCode.RepairCrew)).IsActive = false;
            if (change == "session") (await db.Sessions.SingleAsync(x => x.UserId == crew.Id)).RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me/inspection-tasks")).StatusCode);
        }
        finally
        {
            if (change == "role")
            {
                await using var db = _sql.CreateDbContext();
                (await db.Roles.SingleAsync(x => x.Code == UserRoleCode.RepairCrew)).IsActive = true;
                await db.SaveChangesAsync();
            }
        }
    }

    [Theory]
    [InlineData(FieldInspectionAssignmentStatus.Ended)]
    [InlineData(FieldInspectionAssignmentStatus.Rejected)]
    public async Task Huy02EndedOrRejectedAssignmentsAndReassignmentAreFiltered(FieldInspectionAssignmentStatus status)
    {
        var crew = await _sql.CreateUserAsync($"h02_assignment_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"h02_assign_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var other = await _sql.CreateUserAsync($"h02_assign_other_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var task = await SeedTaskAsync(crew.Id, pm.Id, true);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");
        await using (var db = _sql.CreateDbContext())
        {
            var old = await db.FieldInspectionAssignments.SingleAsync(x => x.FieldInspectionTaskId == task.TaskId);
            db.Entry(old).Property(x => x.Status).CurrentValue = status;
            db.Entry(old).Property(x => x.EndedAt).CurrentValue = DateTimeOffset.UtcNow;
            db.Entry(old).Property(x => x.Reason).CurrentValue = "Ended for test";
            await db.SaveChangesAsync();
        }
        Assert.Empty((await (await client.GetAsync("/api/v1/me/inspection-tasks")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").EnumerateArray());
        await using (var db = _sql.CreateDbContext())
        {
            db.FieldInspectionAssignments.Add(FieldInspectionAssignment.Create(Guid.NewGuid(), task.TaskId,
                other.Id, pm.Id, DateTimeOffset.UtcNow, null, FieldInspectionAssignmentStatus.Active, null));
            await db.SaveChangesAsync();
        }
        Assert.Empty((await (await client.GetAsync("/api/v1/me/inspection-tasks")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Huy02SameDueTimeHiddenBoundaryAdvancesWithoutLeaksOrBusinessWrites()
    {
        var crew = await _sql.CreateUserAsync($"h02_page_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await _sql.CreateUserAsync($"h02_page_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var due = DateTimeOffset.UtcNow.AddDays(1);
        var hidden = await SeedTaskAsync(crew.Id, pm.Id, false, dueAt: due,
            taskId: Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var visible = await SeedTaskAsync(crew.Id, pm.Id, true, dueAt: due,
            taskId: Guid.Parse("00000000-0000-0000-0000-000000000002"));
        await using (var db = _sql.CreateDbContext())
        {
            var task = await db.FieldInspectionTasks.SingleAsync(x => x.Id == visible.TaskId);
            db.Entry(task).Property(x => x.Status).CurrentValue = FieldInspectionTaskStatus.NewAssigned;
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, crew.UserName!, "Current1!");
        await using var snapshot = _sql.CreateDbContext();
        async Task<string> FactsAsync()
        {
            var projects = new[] { hidden.ProjectId, visible.ProjectId };
            var tasks = await snapshot.FieldInspectionTasks.AsNoTracking().Where(x => projects.Contains(x.ProjectId)).OrderBy(x => x.Id).ToArrayAsync();
            var assignments = await snapshot.FieldInspectionAssignments.AsNoTracking().Where(x => x.AssignedToUserId == crew.Id).OrderBy(x => x.Id).ToArrayAsync();
            var sessions = await snapshot.FieldInspectionSessions.AsNoTracking().Where(x => projects.Contains(x.ProjectId)).OrderBy(x => x.Id).ToArrayAsync();
            var measurements = await snapshot.GroundTruthMeasurements.AsNoTracking().Where(x => tasks.Select(t => t.DefectId).Contains(x.DefectId ?? Guid.Empty))
                .Select(x => new { x.Id, x.Value, x.EvidenceFileId }).OrderBy(x => x.Id).ToArrayAsync();
            var defects = await snapshot.Defects.AsNoTracking().Where(x => x.ProjectId != null && projects.Contains(x.ProjectId.Value)).Select(x => new { x.Id, x.Status }).OrderBy(x => x.Id).ToArrayAsync();
            var receipts = await snapshot.IdempotencyRecords.AsNoTracking().Where(x => x.ActorUserId == crew.Id).OrderBy(x => x.Id).ToArrayAsync();
            var authSessions = await snapshot.Sessions.AsNoTracking().Where(x => x.UserId == crew.Id)
                .Select(x => new { x.Id, x.UserId, x.IssuedAt, x.ExpiresAt, x.RevokedAt, x.LastActivityAt, x.RowVersion })
                .OrderBy(x => x.Id).ToArrayAsync();
            var outbox = await snapshot.OutboxMessages.CountAsync();
            return JsonSerializer.Serialize(new { tasks, assignments, sessions, measurements, defects, receipts, authSessions, outbox });
        }
        var before = await FactsAsync();
        var first = await client.GetAsync("/api/v1/me/inspection-tasks?limit=1");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var page = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(page.GetProperty("items").EnumerateArray());
        var cursor = page.GetProperty("nextCursor").GetString();
        Assert.NotNull(cursor);
        var second = await client.GetAsync($"/api/v1/me/inspection-tasks?limit=1&cursor={Uri.EscapeDataString(cursor)}");
        page = await second.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(visible.TaskId, item.GetProperty("id").GetGuid());
        Assert.Equal("NEWASSIGNED", item.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, page.GetProperty("nextCursor").ValueKind);
        Assert.Equal(before, await FactsAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/me/inspection-tasks?cursor=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/me/inspection-tasks?limit=101")).StatusCode);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task<SeededInspectionTask> SeedTaskAsync(
        Guid crewId,
        Guid pmId,
        bool addMembership,
        Guid? assignmentCrewId = null,
        Guid? existingProjectId = null,
        DateTimeOffset? dueAt = null,
        Guid? taskId = null)
    {
        await using var context = _sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var project = existingProjectId.HasValue
            ? await context.Projects.SingleAsync(candidate => candidate.Id == existingProjectId.Value)
            : Project.Create(Guid.NewGuid(), $"P1063-{Guid.NewGuid():N}", "P1-063 inspection project", null, 32648,
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
        var task = FieldInspectionTask.Create(taskId ?? Guid.NewGuid(), $"P1063-TASK-{Guid.NewGuid():N}", project.Id, defect.Id,
            survey.Id, version.Id, 1, "{\"points\":[1]}", null, null, dueAt ?? now.AddDays(1), FieldInspectionTaskStatus.Accepted,
            pmId, null, null, null, null);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, assignmentCrewId ?? crewId, pmId, now, null,
            FieldInspectionAssignmentStatus.Active, null);

        context.AddRange(road, version, survey, defectType, cause, defect, task, assignment);
        if (!existingProjectId.HasValue)
        {
            context.Add(project);
        }
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
        return new(project.Id, task.Id, defect.Id, task.TaskCode, task.RowVersion);
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

    private sealed record SeededInspectionTask(Guid ProjectId, Guid TaskId, Guid DefectId, string TaskCode, byte[] RowVersion);
}
