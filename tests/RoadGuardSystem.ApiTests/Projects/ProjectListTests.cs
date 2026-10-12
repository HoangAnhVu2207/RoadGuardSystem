using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProjectListTests(AuthenticationSqlServerFixture sql)
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task SupervisorListsAllMatchingProjectsIncludingClosedWithoutMembership()
    {
        var user = await sql.CreateUserAsync($"list_sup_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var prefix = $"LIST-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var active = Project.Create(Guid.NewGuid(), prefix + "-A", prefix + " active", null, null, null, null, now);
        var closed = Project.Create(Guid.NewGuid(), prefix + "-C", prefix + " closed", null, null, null, null, now);
        closed.Status = ProjectStatus.Closed;
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(active, closed);
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var identity = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.GetProperty("accessToken").GetString());
        var response = await client.GetAsync("/api/v1/projects?search=" + prefix);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Contains(body.GetProperty("items").EnumerateArray(), x => x.GetProperty("projectId").GetGuid() == closed.Id);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("nextCursor").ValueKind);
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task MembershipChangesAfterLoginApplyToEveryRequest(UserRoleCode role)
    {
        var actor = await UserAsync(role);
        var other = await UserAsync(role);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, role);
        if (role != UserRoleCode.ProjectManager) await TaskAsync(project, actor.Id, role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Single(await ItemsAsync(client, project.ProjectCode));
        using var otherClient = await ClientAsync(factory, other);
        Assert.Empty(await ItemsAsync(otherClient, project.ProjectCode));

        foreach (var condition in new[] { "ended", "expired", "future", "wrong-role" })
        {
            await using (var db = sql.CreateDbContext())
            {
                var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id && x.UserId == actor.Id);
                member.Status = condition == "ended" ? ProjectMemberStatus.Ended : ProjectMemberStatus.Active;
                member.RoleCode = condition == "wrong-role" ? UserRoleCode.Supervisor : role;
                member.ValidFrom = Today.AddDays(condition == "future" ? 1 : -2);
                member.ValidTo = condition == "expired" ? Today.AddDays(-1) : null;
                await db.SaveChangesAsync();
            }
            Assert.Empty(await ItemsAsync(client, project.ProjectCode));
        }
    }

    [Fact]
    public async Task PmNonPrimaryMembershipIsVisibleAndReassignmentUsesCurrentMembership()
    {
        var previous = await UserAsync(UserRoleCode.ProjectManager);
        var current = await UserAsync(UserRoleCode.ProjectManager);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, previous.Id, UserRoleCode.ProjectManager, primary: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var oldClient = await ClientAsync(factory, previous);
        using var newClient = await ClientAsync(factory, current);
        Assert.Equal(previous.Id, Assert.Single(await ItemsAsync(oldClient, project.ProjectCode)).GetProperty("primaryProjectManagerUserId").GetGuid());
        await using (var db = sql.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id);
            member.EndPrimaryAssignment(Today.AddDays(-1));
            await db.SaveChangesAsync();
        }
        await MemberAsync(project.Id, current.Id, UserRoleCode.ProjectManager, primary: true);
        Assert.Empty(await ItemsAsync(oldClient, project.ProjectCode));
        var item = Assert.Single(await ItemsAsync(newClient, project.ProjectCode));
        Assert.Equal(current.Id, item.GetProperty("primaryProjectManagerUserId").GetGuid());
        Assert.Equal(current.DisplayName, item.GetProperty("primaryProjectManagerName").GetString());
        await using (var db = sql.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id && x.UserId == current.Id);
            member.IsPrimary = false;
            member.ValidTo = Today;
            await db.SaveChangesAsync();
        }
        item = Assert.Single(await ItemsAsync(newClient, project.ProjectCode));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("primaryProjectManagerUserId").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("primaryProjectManagerName").ValueKind);
    }

    [Theory]
    [InlineData(UserRoleCode.DroneOperator, false, true)]
    [InlineData(UserRoleCode.DroneOperator, true, false)]
    [InlineData(UserRoleCode.RepairCrew, false, true)]
    [InlineData(UserRoleCode.RepairCrew, true, false)]
    public async Task TaskAndMembershipAreBothRequired(UserRoleCode role, bool membership, bool task)
    {
        var actor = await UserAsync(role);
        var project = await ProjectAsync();
        if (membership) await MemberAsync(project.Id, actor.Id, role);
        if (task) await TaskAsync(project, actor.Id, role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Empty(await ItemsAsync(client, project.ProjectCode));
    }

    [Theory]
    [InlineData(SurveyRequestStatus.NewAssigned, true)]
    [InlineData(SurveyRequestStatus.Reassigned, true)]
    [InlineData(SurveyRequestStatus.Accepted, true)]
    [InlineData(SurveyRequestStatus.InProgress, true)]
    [InlineData(SurveyRequestStatus.Submitted, true)]
    [InlineData(SurveyRequestStatus.SupplementRequired, true)]
    [InlineData(SurveyRequestStatus.Completed, true)]
    [InlineData(SurveyRequestStatus.Postponed, true)]
    [InlineData(SurveyRequestStatus.Rejected, false)]
    [InlineData(SurveyRequestStatus.Cancelled, false)]
    public async Task OperatorCurrentAssignmentTaskStatesIncludeCompleted(SurveyRequestStatus status, bool visible)
    {
        var actor = await UserAsync(UserRoleCode.DroneOperator);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, actor.RoleCode);
        await TaskAsync(project, actor.Id, actor.RoleCode, surveyStatus: status);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Equal(visible ? 1 : 0, (await ItemsAsync(client, project.ProjectCode)).Length);
    }

    [Theory]
    [InlineData(FieldInspectionTaskStatus.NewAssigned, true)]
    [InlineData(FieldInspectionTaskStatus.Accepted, true)]
    [InlineData(FieldInspectionTaskStatus.InProgress, true)]
    [InlineData(FieldInspectionTaskStatus.Submitted, true)]
    [InlineData(FieldInspectionTaskStatus.SupplementRequired, true)]
    [InlineData(FieldInspectionTaskStatus.Completed, true)]
    [InlineData(FieldInspectionTaskStatus.Rejected, false)]
    [InlineData(FieldInspectionTaskStatus.Cancelled, false)]
    public async Task CrewCurrentAssignmentTaskStatesIncludeCompleted(FieldInspectionTaskStatus status, bool visible)
    {
        var actor = await UserAsync(UserRoleCode.RepairCrew);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, actor.RoleCode);
        await TaskAsync(project, actor.Id, actor.RoleCode, fieldStatus: status);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Equal(visible ? 1 : 0, (await ItemsAsync(client, project.ProjectCode)).Length);
    }

    [Theory]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task ReassignedHistoryCannotGrantScopeButAnotherCurrentTaskCan(UserRoleCode role)
    {
        var actor = await UserAsync(role);
        var replacement = await UserAsync(role);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, role);
        await MemberAsync(project.Id, replacement.Id, role);
        var taskId = await TaskAsync(project, actor.Id, role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Single(await ItemsAsync(client, project.ProjectCode));
        await using (var db = sql.CreateDbContext())
        {
            if (role == UserRoleCode.DroneOperator)
            {
                var assignment = await db.SurveyAssignments.SingleAsync(x => x.SurveyRequestId == taskId);
                assignment.EndForReassignment(DateTimeOffset.UtcNow, "fixture reassignment");
                db.Add(SurveyAssignment.Create(Guid.NewGuid(), taskId, replacement.Id, replacement.Id, DateTimeOffset.UtcNow, null, null, null, null, null));
            }
            else
            {
                var assignment = await db.FieldInspectionAssignments.SingleAsync(x => x.FieldInspectionTaskId == taskId);
                assignment.End(DateTimeOffset.UtcNow, "fixture reassignment");
                db.Add(FieldInspectionAssignment.Create(Guid.NewGuid(), taskId, replacement.Id, replacement.Id, DateTimeOffset.UtcNow, null, FieldInspectionAssignmentStatus.Active, null));
            }
            await db.SaveChangesAsync();
        }
        Assert.Empty(await ItemsAsync(client, project.ProjectCode));
        using var replacementClient = await ClientAsync(factory, replacement);
        Assert.Single(await ItemsAsync(replacementClient, project.ProjectCode));
        await TaskAsync(project, actor.Id, role);
        Assert.Single(await ItemsAsync(client, project.ProjectCode));
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task PaginationFiltersBeforeLimitDoesNotDuplicateOrExposeHiddenProjects(UserRoleCode role)
    {
        var actor = await UserAsync(role);
        var prefix = $"PAGE-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var visibleIds = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var project = await ProjectAsync(prefix + "-" + i, now);
            if (i == 0) continue;
            visibleIds.Add(project.Id);
            await MemberAsync(project.Id, actor.Id, role);
            if (role != UserRoleCode.ProjectManager)
            {
                await TaskAsync(project, actor.Id, role);
                await TaskAsync(project, actor.Id, role);
            }
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        string? cursor = null;
        var actual = new List<Guid>();
        do
        {
            var response = await client.GetAsync($"/api/v1/projects?search={prefix}&status=CLOSED&limit=1&cursor={Uri.EscapeDataString(cursor ?? "")}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await response.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(page.GetProperty("items").EnumerateArray());
            actual.Add(item.GetProperty("projectId").GetGuid());
            Assert.Equal(6, item.EnumerateObject().Count());
            cursor = page.GetProperty("nextCursor").GetString();
            Assert.True(actual.Count <= 3, "Cursor did not advance");
        } while (cursor is not null);
        Assert.Equal(3, actual.Distinct().Count());
        Assert.True(visibleIds.ToHashSet().SetEquals(actual));
        Assert.Empty(await ItemsAsync(client, prefix, "&status=ACTIVE"));
        Assert.Empty(await ItemsAsync(client, prefix + "-0"));
        Assert.Single(await ItemsAsync(client, prefix + "-1"));
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=101")]
    [InlineData("limit=abc")]
    [InlineData("status=INVALID")]
    [InlineData("status=2")]
    [InlineData("cursor=broken")]
    [InlineData("cursor=MHwwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDA=")]
    public async Task InvalidQueryUsesProblemResponse(string query)
    {
        var actor = await UserAsync(UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        var response = await client.GetAsync("/api/v1/projects?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AnonymousReporterAndEmptyAllowedScopeFollowExistingPipeline()
    {
        var reporter = await UserAsync(UserRoleCode.Reporter);
        var pm = await UserAsync(UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/projects")).StatusCode);
        using var reporterClient = await ClientAsync(factory, reporter);
        Assert.Equal(HttpStatusCode.Forbidden, (await reporterClient.GetAsync("/api/v1/projects")).StatusCode);
        using var pmClient = await ClientAsync(factory, pm);
        Assert.Empty(await ItemsAsync(pmClient, ""));
    }

    [Theory]
    [InlineData("suspended", HttpStatusCode.Unauthorized)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    [InlineData("forced-password", HttpStatusCode.Forbidden)]
    [InlineData("role-changed", HttpStatusCode.Unauthorized)]
    public async Task AccountAndSessionChangesAreEnforcedBeforeList(string change, HttpStatusCode expected)
    {
        var actor = await UserAsync(UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        await using (var db = sql.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(x => x.Id == actor.Id);
            if (change == "suspended") user.Status = UserStatus.Suspended;
            if (change == "forced-password") user.MustChangePassword = true;
            if (change == "revoked")
                await db.Database.ExecuteSqlAsync($"UPDATE [Sessions] SET [RevokedAt]={DateTimeOffset.UtcNow} WHERE [UserId]={actor.Id}");
            await db.SaveChangesAsync();
            if (change == "role-changed")
            {
                using var scope = factory.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IIdentityRepository>().ChangeUserRoleAtomicAsync(
                    actor.Id, UserRoleCode.Reporter, user.RowVersion, actor.Id, Guid.NewGuid(), reason: "isolated fixture role change");
            }
        }
        var response = await client.GetAsync("/api/v1/projects");
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("current", true)]
    [InlineData("binding-ended", false)]
    [InlineData("crew-replaced", false)]
    [InlineData("item-cancelled", false)]
    public async Task NativeRepairRequiresCurrentItemBindingEvenWithCompletedTask(string condition, bool visible)
    {
        var actor = await UserAsync(UserRoleCode.RepairCrew);
        var replacement = await UserAsync(UserRoleCode.RepairCrew);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, actor.RoleCode);
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Projects.SingleAsync(x => x.Id == project.Id);
            row.Status = ProjectStatus.Active;
            await db.SaveChangesAsync();
        }
        var taskId = await TaskAsync(project, actor.Id, actor.RoleCode, nativeRepair: true);
        await using (var db = sql.CreateDbContext())
        {
            var task = await db.FieldInspectionTasks.SingleAsync(x => x.Id == taskId);
            var item = await db.RepairItems.SingleAsync(x => x.Id == task.RepairItemId);
            if (condition == "binding-ended") db.Entry(item).Property(x => x.CurrentBindingId).CurrentValue = null;
            if (condition == "crew-replaced") db.Entry(item).Property(x => x.CrewId).CurrentValue = replacement.Id;
            if (condition == "item-cancelled") db.Entry(item).Property(x => x.State).CurrentValue = RepairItemState.Cancelled;
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Equal(visible ? 1 : 0, (await ItemsAsync(client, project.ProjectCode)).Length);
    }

    [Fact]
    public async Task SearchMatchesNameAndLiteralWildcardAndOversizedInputsAreRejected()
    {
        var actor = await UserAsync(UserRoleCode.Supervisor);
        var project = await ProjectAsync();
        var uniqueName = $"NameOnly-{Guid.NewGuid():N}-%_";
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Projects.SingleAsync(x => x.Id == project.Id);
            row.Name = uniqueName;
            row.Status = ProjectStatus.Planning;
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Single(await ItemsAsync(client, uniqueName, "&status=PLANNING"));
        Assert.Empty(await ItemsAsync(client, uniqueName.Replace("%_", "%missing")));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/projects?search=" + new string('x', 256))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/projects?cursor=" + new string('x', 257))).StatusCode);
    }

    [Fact]
    public async Task SupervisorCanDiscoverProjectsCreatedByDifferentSupervisors()
    {
        var first = await UserAsync(UserRoleCode.Supervisor);
        var second = await UserAsync(UserRoleCode.Supervisor);
        var pm = await UserAsync(UserRoleCode.ProjectManager);
        var prefix = $"CREATOR-{Guid.NewGuid():N}";
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var firstClient = await ClientAsync(factory, first);
        using var secondClient = await ClientAsync(factory, second);
        var i = 0;
        foreach (var client in new[] { firstClient, secondClient })
        {
            var response = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                code = prefix + "-" + i++,
                name = "TEST_ONLY different creator",
                primaryPmId = pm.Id,
                handoverDate = Today,
                warrantyEndDate = Today.AddYears(1),
                handoverFileIds = Array.Empty<Guid>(),
                operationId = Guid.NewGuid()
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        Assert.Equal(2, (await ItemsAsync(firstClient, prefix)).Length);
        Assert.Equal(2, (await ItemsAsync(secondClient, prefix)).Length);
    }

    [Fact]
    public async Task DefaultLimitAndClientSelectorsCannotWidenScope()
    {
        var supervisor = await UserAsync(UserRoleCode.Supervisor);
        var pm = await UserAsync(UserRoleCode.ProjectManager);
        var prefix = $"LIMIT-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var projects = Enumerable.Range(0, 21).Select(i => Project.Create(Guid.NewGuid(), prefix + "-" + i,
            "SQL pagination fixture", null, null, null, null, now)).ToArray();
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(projects);
            await db.SaveChangesAsync();
        }
        await MemberAsync(projects[0].Id, pm.Id, pm.RoleCode);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var supervisorClient = await ClientAsync(factory, supervisor);
        Assert.Equal(20, (await ItemsAsync(supervisorClient, prefix)).Length);
        Assert.Equal(21, (await ItemsAsync(supervisorClient, prefix, "&limit=100")).Length);
        using var pmClient = await ClientAsync(factory, pm);
        var item = Assert.Single(await ItemsAsync(pmClient, prefix, $"&role=SUPERVISOR&userId={supervisor.Id}&crewId={supervisor.Id}"));
        Assert.Equal(projects[0].Id, item.GetProperty("projectId").GetGuid());
    }

    [Fact]
    public async Task MembershipDateBoundaryUsesTimeProviderUtcAndBusinessRowsRemainUnchanged()
    {
        var pm = await UserAsync(UserRoleCode.ProjectManager);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, pm.Id, pm.RoleCode);
        await using var db = sql.CreateDbContext();
        var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id);
        member.ValidTo = Today;
        await db.SaveChangesAsync();
        var originalMemberVersion = member.RowVersion.ToArray();
        var originalProjectVersion = (await db.Projects.SingleAsync(x => x.Id == project.Id)).RowVersion.ToArray();
        var midnight = new DateTimeOffset(Today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var clock = new MutableClock(midnight.AddMinutes(-1));
        var service = new ProjectListService(new ProjectListReadModel(db), clock);
        Assert.Single((await service.ListAsync(pm.Id, pm.RoleCode, project.ProjectCode, null, null, 20)).Page!.Items);
        clock.Now = midnight;
        Assert.Empty((await service.ListAsync(pm.Id, pm.RoleCode, project.ProjectCode, null, null, 20)).Page!.Items);
        await db.Entry(member).ReloadAsync();
        Assert.Equal(originalMemberVersion, member.RowVersion);
        Assert.Equal(originalProjectVersion, (await db.Projects.AsNoTracking().SingleAsync(x => x.Id == project.Id)).RowVersion);
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task ExistingPmReassignmentImmediatelyChangesListAndPrimaryProjection()
    {
        var supervisor = await UserAsync(UserRoleCode.Supervisor);
        var previous = await UserAsync(UserRoleCode.ProjectManager);
        var current = await UserAsync(UserRoleCode.ProjectManager);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, previous.Id, previous.RoleCode, primary: true);
        string expectedVersion;
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.Projects.SingleAsync(x => x.Id == project.Id);
            row.Status = ProjectStatus.Active;
            await db.SaveChangesAsync();
            expectedVersion = Convert.ToBase64String((await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id)).RowVersion);
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var oldClient = await ClientAsync(factory, previous);
        using var newClient = await ClientAsync(factory, current);
        using var supervisorClient = await ClientAsync(factory, supervisor);
        Assert.Single(await ItemsAsync(oldClient, project.ProjectCode));
        Assert.Empty(await ItemsAsync(newClient, project.ProjectCode));
        var response = await supervisorClient.PutAsJsonAsync($"/api/v1/projects/{project.Id}/primary-project-manager", new
        {
            primaryProjectManagerUserId = current.Id,
            effectiveFrom = Today,
            reason = "isolated scope regression",
            expectedCurrentMembershipRowVersion = expectedVersion,
            operationId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ItemsAsync(oldClient, project.ProjectCode));
        var item = Assert.Single(await ItemsAsync(newClient, project.ProjectCode));
        Assert.Equal(current.Id, item.GetProperty("primaryProjectManagerUserId").GetGuid());
        Assert.Equal(current.DisplayName, item.GetProperty("primaryProjectManagerName").GetString());
    }

    [Theory]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task DeclinedAssignmentCannotGrantScope(UserRoleCode role)
    {
        var actor = await UserAsync(role);
        var project = await ProjectAsync();
        await MemberAsync(project.Id, actor.Id, role);
        var taskId = await TaskAsync(project, actor.Id, role, surveyStatus: SurveyRequestStatus.NewAssigned, fieldStatus: FieldInspectionTaskStatus.NewAssigned);
        await using (var db = sql.CreateDbContext())
        {
            if (role == UserRoleCode.DroneOperator)
                (await db.SurveyAssignments.SingleAsync(x => x.SurveyRequestId == taskId)).Reject(DateTimeOffset.UtcNow, "fixture rejected");
            else
                (await db.FieldInspectionAssignments.SingleAsync(x => x.FieldInspectionTaskId == taskId)).End(DateTimeOffset.UtcNow, "fixture rejected", rejected: true);
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await ClientAsync(factory, actor);
        Assert.Empty(await ItemsAsync(client, project.ProjectCode));
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    private Task<ApplicationUser> UserAsync(UserRoleCode role) =>
        sql.CreateUserAsync($"list_{Guid.NewGuid():N}", "Current1!", role);

    private async Task<Project> ProjectAsync(string? code = null, DateTimeOffset? created = null)
    {
        code ??= $"LIST-{Guid.NewGuid():N}";
        var project = Project.Create(Guid.NewGuid(), code, "Name " + code, null, null, null, null, created ?? DateTimeOffset.UtcNow);
        project.Status = ProjectStatus.Closed;
        await using var db = sql.CreateDbContext();
        db.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private async Task MemberAsync(Guid projectId, Guid userId, UserRoleCode role, bool primary = false)
    {
        await using var db = sql.CreateDbContext();
        db.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleCode = role,
            IsPrimary = primary,
            Status = ProjectMemberStatus.Active,
            ValidFrom = Today.AddDays(-2)
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> TaskAsync(Project project, Guid actor, UserRoleCode role,
        SurveyRequestStatus surveyStatus = SurveyRequestStatus.Completed,
        FieldInspectionTaskStatus fieldStatus = FieldInspectionTaskStatus.Completed, bool nativeRepair = false)
    {
        var now = DateTimeOffset.UtcNow;
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        await using var db = sql.CreateDbContext();
        db.Add(road);
        Guid taskId;
        if (role == UserRoleCode.DroneOperator)
        {
            var task = SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, actor, SurveyType.Periodic,
                surveyStatus == SurveyRequestStatus.Cancelled ? SurveyRequestStatus.NewAssigned : surveyStatus, now);
            if (surveyStatus == SurveyRequestStatus.Cancelled) task.Cancel("fixture cancellation", now);
            db.AddRange(task, SurveyAssignment.Create(Guid.NewGuid(), task.Id, actor, actor, now, null, null, null, null, null));
            taskId = task.Id;
        }
        else
        {
            var geometry = new GeometryFactory(new PrecisionModel(), 32648);
            var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
                geometry.CreateLineString([new Coordinate(500000, 1100000), new Coordinate(500100, 1100100)]), now, "SQL test fixture");
            var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.Completed, false, null, null);
            var type = DefectType.Create($"DT-{Guid.NewGuid():N}", "Fixture");
            var cause = CauseCategory.Create($"CC-{Guid.NewGuid():N}", "Fixture");
            var defect = Defect.Create(Guid.NewGuid(), project.Id, version.Id, null, type.Code, cause.Code,
                DefectSeverity.Medium, DefectStatus.Open, geometry.CreatePoint(new Coordinate(500050, 1100050)), now);
            RepairItem? item = null;
            var assigner = actor;
            FieldInspectionTask task;
            if (nativeRepair)
            {
                var reporter = await UserAsync(UserRoleCode.Reporter);
                var pm = await UserAsync(UserRoleCode.ProjectManager);
                var supervisor = await UserAsync(UserRoleCode.Supervisor);
                assigner = pm.Id;
                await MemberAsync(project.Id, pm.Id, pm.RoleCode, primary: true);
                db.AddRange(version, survey, type, cause);
                await db.SaveChangesAsync();
                var file = StoredFile.Create(Guid.NewGuid(), "private/list-fixture-" + Guid.NewGuid().ToString("N"), "source.jpg",
                    "image/jpeg", 4, new string('b', 64), reporter.Id, now, null);
                db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now));
                await db.SaveChangesAsync();
                var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, file.StorageUri, "REPORT_PHOTO", "image/jpeg",
                    4, new string('b', 64), 8388608, now.AddHours(24));
                upload.StartUploading("SQL fixture", now);
                db.Add(upload);
                await db.SaveChangesAsync();
                upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
                await db.SaveChangesAsync();
                upload.MarkVerified();
                await db.SaveChangesAsync();
                var report = Report.Create(Guid.NewGuid(), reporter.Id, "SQL list scope fixture", now,
                    [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
                var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
                incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "SQL fixture source", now);
                db.AddRange(report, incident);
                db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now });
                await db.SaveChangesAsync();
                var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id, "fixture-source"),
                    project.Id, "fixture-geometry");
                var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id, source, CandidateDecisionKind.KeepNew,
                    CandidateClassification.Create(version.Id, type.Code, null, DefectSeverity.Low, null), null, null, null,
                    "SQL fixture keep-new", null, default);
                defect = await db.Defects.SingleAsync(x => x.Id == accepted.DefectId);
                var scope = RepairActualScope.Create(Guid.NewGuid(), road.Id, "fixture-v1", "R", 0, 10, 0, 1);
                var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true, scope);
                item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, pm.Id,
                    UserRoleCode.ProjectManager, now, new("fixture repair plan", "fixture-checklist"));
                item.Approve(supervisor.Id, UserRoleCode.Supervisor, now);
                item.Assign(actor, pm.Id, UserRoleCode.ProjectManager, now);
                task = FieldInspectionTask.CreateRepair(Guid.NewGuid(), $"TASK-{Guid.NewGuid():N}", item, null, "REPORTER",
                    version.Id, null, null, null, FieldInspectionPurpose.PostRepair, 1, "{\"points\":[1]}", null, now.AddDays(1), pm.Id);
                db.AddRange(obligation, item);
                await db.SaveChangesAsync();
                db.Entry(task).Property(x => x.Status).CurrentValue = fieldStatus;
            }
            else
                task = FieldInspectionTask.Create(Guid.NewGuid(), $"TASK-{Guid.NewGuid():N}", project.Id, defect.Id, survey.Id,
                    version.Id, 1, "{\"points\":[1]}", null, null, now.AddDays(1), fieldStatus, actor, null, null, null, null);
            var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, actor, assigner, now, null, FieldInspectionAssignmentStatus.Active, null);
            if (!nativeRepair) db.AddRange(version, survey, type, cause, defect);
            db.AddRange(task, assignment);
            if (item is not null)
            {
                await db.SaveChangesAsync();
                var binding = new RepairFieldTaskBinding(Guid.NewGuid(), project.Id, defect.Id, item.ObligationId,
                    item.Id, task.Id, assignment.Id, actor, item.Mode, null, null, null, item.ProposalPlanHash!, item.ChecklistVersion!,
                    version.Id, null, null, null, null, null, "fixture-v1", assigner, now, "fixture binding",
                    Convert.ToBase64String(task.RowVersion), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        JsonSerializer.SerializeToUtf8Bytes(new
                        {
                            assignment.Id,
                            assignment.FieldInspectionTaskId,
                            assignment.AssignedToUserId,
                            assignment.AssignedByUserId,
                            assignment.AssignedAt
                        }, WebJson))).ToLowerInvariant());
                db.Add(binding);
                await db.SaveChangesAsync();
                db.Entry(item).Property(x => x.CurrentBindingId).CurrentValue = binding.Id;
            }
            taskId = task.Id;
        }
        await db.SaveChangesAsync();
        return taskId;
    }

    private static async Task<HttpClient> ClientAsync(AuthenticationWebApplicationFactory factory, ApplicationUser user)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var identity = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement[]> ItemsAsync(HttpClient client, string search, string additionalQuery = "")
    {
        var response = await client.GetAsync("/api/v1/projects?search=" + Uri.EscapeDataString(search) + additionalQuery);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        return page.GetProperty("items").EnumerateArray().ToArray();
    }
}
