using System.Net;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.Repositories.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebPmCoordinationHttpTests(AuthenticationSqlServerFixture sql)
{
    private sealed record Fixture(ApplicationUser Supervisor, ApplicationUser Pm, ApplicationUser Operator,
        ApplicationUser Other, Project Project, RoadSection Road, RoadSectionVersion Route, Defect Defect,
        RepairPackage Package, RepairItem Item);

    private async Task<Fixture> Seed()
    {
        var stamp = Guid.NewGuid().ToString("N"); var now = DateTimeOffset.UtcNow;
        var sup = await sql.CreateUserAsync("web13-sup-" + stamp, "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync("web13-pm-" + stamp, "Current1!", UserRoleCode.ProjectManager);
        var op = await sql.CreateUserAsync("web13-op-" + stamp, "Current1!", UserRoleCode.DroneOperator);
        var other = await sql.CreateUserAsync("web13-other-" + stamp, "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), "WEB13-" + stamp, "Web coordination fixture", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "WEB13");
        var line = new LineString([new(500000, 1100000), new(500100, 1100000)]) { SRID = 32648 };
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, line, now, "isolated fixture");
        var type = DefectType.Create("WEB13-" + stamp, "Fixture defect", null, true);
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null,
            DefectSeverity.Medium, DefectStatus.Verified, new Point(500010, 1100000) { SRID = 32648 }, now);
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "stored-location", road.Code, 0, 20, -2, 2));
        var secondObligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "stored-location", road.Code, 30, 50, -2, 2));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation, secondObligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, pm.Id, UserRoleCode.ProjectManager,
            now, new("Repair only the selected item", "web13-v1"));
        package.AddItem(item);
        await using var db = sql.CreateDbContext();
        db.AddRange(project, road, route, type, defect, package,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime)),
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = op.Id,
                RoleCode = UserRoleCode.DroneOperator,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
                Status = ProjectMemberStatus.Active
            });
        await db.SaveChangesAsync();
        return new(sup, pm, op, other, project, road, route, defect, package, item);
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, actual {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    private static async Task<HttpClient> Bearer(AuthenticationWebApplicationFactory factory, ApplicationUser actor)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
        return client;
    }
    private static Task<HttpResponseMessage> Put(HttpClient client, string path, object input, string version, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        request.Headers.TryAddWithoutValidation("If-Match", '"' + version + '"');
        return client.SendAsync(request);
    }

    [Fact]
    public async Task MetadataContactAndPlanningReadBackWithRealWebScopeVersionsAndReplay()
    {
        var seed = await Seed();
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs);
        using var sup = await WebCookieTestClient.Login(factory, seed.Supervisor.Email!);
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        using var wrong = await WebCookieTestClient.Login(factory, seed.Other.Email!);
        using var operatorBearer = await Bearer(factory, seed.Operator);
        var overview = $"/api/v1/projects/overviews?ids={seed.Project.Id}";
        Assert.Equal(HttpStatusCode.Forbidden, (await operatorBearer.GetAsync(overview)).StatusCode);
        var before = (await Body(await sup.GetAsync(overview)))[0];
        Assert.Null(before.GetProperty("locality").GetString());
        Assert.Equal(100, before.GetProperty("totalLengthMeters").GetDouble());
        Assert.Equal(1, before.GetProperty("openDefects").GetProperty("value").GetInt64());
        var version = before.GetProperty("version").GetString()!;
        var path = $"/api/v1/projects/{seed.Project.Id}/locality";
        var input = new { locality = "Phường thử nghiệm", reason = "Set stored locality" };
        sup.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Put(sup, path, input, version)).StatusCode);
        await WebCookieTestClient.RefreshCsrf(sup);
        var key = Guid.NewGuid().ToString("N");
        var simultaneous = await Task.WhenAll(Put(sup, path, input, version, key), Put(sup, path, input, version, key));
        var mutation = simultaneous[0];
        Assert.Equal(HttpStatusCode.OK, simultaneous[1].StatusCode);
        Assert.True(mutation.IsSuccessStatusCode, logs.CombinedEntries);
        var saved = await Body(mutation);
        Assert.NotEqual(version, saved.GetProperty("version").GetString());
        Assert.Equal(saved.GetRawText(), (await Body(await Put(sup, path, input, version, key))).GetRawText());
        Assert.Equal(HttpStatusCode.Conflict, (await Put(sup, path, input, version)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Put(pm, path, input, version)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await wrong.GetAsync(overview)).StatusCode);
        using var reload = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        Assert.Equal(input.locality, (await Body(await reload.GetAsync(overview)))[0].GetProperty("locality").GetString());

        var organizationPath = $"/api/v1/users/{seed.Operator.Id}/organization";
        var organization = await Body(await sup.GetAsync(organizationPath));
        await Body(await Put(sup, organizationPath, new { organization = "Đơn vị thử nghiệm", reason = "Staff administration" }, organization.GetProperty("version").GetString()!));
        var contacts = await Body(await pm.GetAsync($"/api/v1/projects/{seed.Project.Id}/member-contacts?limit=1"));
        var next = contacts.GetProperty("nextCursor").GetString(); Assert.NotNull(next);
        var page2 = await Body(await pm.GetAsync($"/api/v1/projects/{seed.Project.Id}/member-contacts?limit=1&cursor={Uri.EscapeDataString(next!)}"));
        var rows = contacts.GetProperty("items").EnumerateArray().Concat(page2.GetProperty("items").EnumerateArray()).ToArray();
        var op = Assert.Single(rows.Where(row => row.GetProperty("userId").GetGuid() == seed.Operator.Id));
        Assert.Equal("Đơn vị thử nghiệm", op.GetProperty("organization").GetString());
        Assert.Equal(seed.Operator.Email, op.GetProperty("email").GetString());
        Assert.Null(op.GetProperty("phoneNumber").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await wrong.GetAsync($"/api/v1/projects/{seed.Project.Id}/member-contacts")).StatusCode);

        var packagePath = $"/api/v1/projects/{seed.Project.Id}/repair-packages/{seed.Package.Id}/planning";
        var planningResponse = await pm.GetAsync(packagePath);
        Assert.True(planningResponse.IsSuccessStatusCode, logs.CombinedEntries);
        var planning = await Body(planningResponse);
        Assert.Null(planning.GetProperty("createdBy").GetString());
        var packageSaved = await Put(pm, packagePath, new { name = "Gói thử nghiệm", description = "Stored plan", plannedDueAt = DateTimeOffset.UtcNow.AddDays(2), reason = "PM plan" }, planning.GetProperty("version").GetString()!);
        Assert.True(packageSaved.IsSuccessStatusCode, logs.CombinedEntries);
        await Body(packageSaved);
        Assert.Equal("Gói thử nghiệm", (await Body(await reload.GetAsync(packagePath))).GetProperty("name").GetString());
        var itemPath = $"/api/v1/projects/{seed.Project.Id}/repair-packages/{seed.Package.Id}/items/{seed.Item.Id}/planning";
        var item = await Body(await pm.GetAsync(itemPath));
        await Body(await Put(pm, itemPath, new { plannedQuantity = 12.25m, plannedUnit = "m2", plannedMaterials = "Vật tư dự kiến", reason = "Plan separate from measurement" }, item.GetProperty("version").GetString()!));
        Assert.Equal(12.25m, (await Body(await reload.GetAsync(itemPath))).GetProperty("plannedQuantity").GetDecimal());
        var sourcePath = $"/api/v1/projects/{seed.Project.Id}/repair-packages/{seed.Package.Id}/items/{seed.Item.Id}/source-defect";
        var source = await Body(await sup.GetAsync(sourcePath));
        Assert.Equal(seed.Defect.Id, source.GetProperty("defectId").GetGuid());
        Assert.Contains(source.GetProperty("missingSections").EnumerateArray(), row => row.GetString() == "LEGACY_SOURCE_NOT_STORED");
        Assert.DoesNotContain(source.GetProperty("missingSections").EnumerateArray(), row => row.GetString() == "SOURCE_EVIDENCE_REQUIRES_SOURCE_RESOLVER");
        Assert.Equal(HttpStatusCode.NotFound, (await sup.GetAsync(sourcePath.Replace(seed.Package.Id.ToString(), Guid.NewGuid().ToString()))).StatusCode);
        using var bearer = await Bearer(factory, seed.Pm);
        Assert.Equal(HttpStatusCode.OK, (await bearer.GetAsync(overview)).StatusCode);
        await using var check = sql.CreateDbContext();
        Assert.Equal(input.locality, (await check.Projects.SingleAsync(row => row.Id == seed.Project.Id)).Locality);
        Assert.True(await check.AuditLogs.AnyAsync(row => row.EntityId == seed.Item.Id && row.EventType == "web_metadata_item"));
        Assert.Equal(1, await check.AuditLogs.CountAsync(row => row.EntityId == seed.Project.Id && row.EventType == "web_metadata_locality"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET PlannedQuantity=NULL WHERE Id={seed.Item.Id}"));
        Assert.Equal(12.25m, (await check.RepairItems.AsNoTracking().SingleAsync(row => row.Id == seed.Item.Id)).PlannedQuantity);
        await check.ProjectMembers.Where(row => row.ProjectId == seed.Project.Id && row.UserId == seed.Operator.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        var active = await Body(await pm.GetAsync($"/api/v1/projects/{seed.Project.Id}/member-contacts"));
        Assert.DoesNotContain(active.GetProperty("items").EnumerateArray(), row => row.GetProperty("userId").GetGuid() == seed.Operator.Id);
    }

    [Fact]
    public async Task BulkReadsUseAuthorizedSnapshotCutoffAndDurableReplay()
    {
        var seed = await Seed(); var at = DateTimeOffset.UtcNow.AddMinutes(-2);
        var own = Enumerable.Range(0, 103).Select(i => Notification.Create(Guid.NewGuid(), seed.Pm.Id, "Project", seed.Project.Id, "fixture-" + i, "Fixture", "Stored body", at.AddMilliseconds(i))).ToArray();
        var foreign = Notification.Create(Guid.NewGuid(), seed.Other.Id, "Project", seed.Project.Id, "fixture", "Foreign", "Private", at);
        await using (var db = sql.CreateDbContext()) { db.AddRange(own); db.Add(foreign); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1); var key = Guid.NewGuid().ToString("N");
        async Task<HttpResponseMessage> Mark(string commandKey, string? snapshotVersion = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/notifications/read-batch")
            { Content = JsonContent.Create(new { through = cutoff, limit = 100, snapshotVersion }) };
            request.Headers.Add("Idempotency-Key", commandKey); return await pm.SendAsync(request);
        }
        pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Mark(key)).StatusCode);
        await WebCookieTestClient.RefreshCsrf(pm);
        var simultaneous = await Task.WhenAll(Mark(key), Mark(key));
        var first = await Body(simultaneous[0]);
        Assert.Equal(first.GetRawText(), (await Body(simultaneous[1])).GetRawText()); Assert.Equal(100, first.GetProperty("updated").GetInt32()); Assert.True(first.GetProperty("hasMore").GetBoolean());
        Assert.Equal(first.GetRawText(), (await Body(await Mark(key))).GetRawText());
        var newNotification = Notification.Create(Guid.NewGuid(), seed.Pm.Id, "Project", seed.Project.Id, "fixture-new", "New", "Inserted later with an old occurrence", at);
        await using (var db = sql.CreateDbContext()) { db.Add(newNotification); await db.SaveChangesAsync(); }
        var secondKey = Guid.NewGuid().ToString("N");
        var snapshot = first.GetProperty("snapshotVersion").GetString();
        var second = await Body(await Mark(secondKey, snapshot));
        Assert.Equal(3, second.GetProperty("updated").GetInt32());
        Assert.False(second.GetProperty("hasMore").GetBoolean());
        Assert.Equal(snapshot, second.GetProperty("snapshotVersion").GetString());
        Assert.Equal(second.GetRawText(), (await Body(await Mark(secondKey, snapshot))).GetRawText());
        await using var check = sql.CreateDbContext();
        Assert.Equal(103, await check.Notifications.CountAsync(row => own.Select(n => n.Id).Contains(row.Id) && row.ReadAt != null));
        Assert.Null((await check.Notifications.SingleAsync(row => row.Id == foreign.Id)).ReadAt);
        Assert.Null((await check.Notifications.SingleAsync(row => row.Id == newNotification.Id)).ReadAt);
        await check.ProjectMembers.Where(row => row.ProjectId == seed.Project.Id && row.UserId == seed.Pm.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await Mark(key)).StatusCode);
    }

    [Fact]
    public async Task SurveySummaryBatchesStoredScopeCurrentNamesHistoryAndDatasetCounts()
    {
        var seed = await Seed(); var now = DateTimeOffset.UtcNow;
        var plan = SurveyPlan.Create(Guid.NewGuid(), seed.Project.Id, seed.Road.Id, now, now.AddDays(1), SurveyType.Original, SurveyPlanStatus.Planned); plan.SetBandScope();
        var task = SurveyRequest.Create(Guid.NewGuid(), seed.Project.Id, seed.Road.Id, plan.Id, seed.Pm.Id, SurveyType.Original, SurveyRequestStatus.NewAssigned, now); task.SetBandScope();
        var assignment = SurveyAssignment.Create(Guid.NewGuid(), task.Id, seed.Operator.Id, seed.Pm.Id, now, null, null, null, null, null);
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(plan, task, assignment, SurveyPlanScope.Create(Guid.NewGuid(), plan.Id, seed.Route.Id, Guid.NewGuid(), JsonSerializer.Serialize(new[] { Guid.NewGuid(), Guid.NewGuid() }), "SURFACE"),
                SurveyRequestScope.Create(Guid.NewGuid(), task.Id, seed.Route.Id, Guid.NewGuid(), JsonSerializer.Serialize(new[] { Guid.NewGuid() }), "SURFACE"));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        var plans = await Body(await pm.GetAsync($"/api/v1/projects/{seed.Project.Id}/survey-plans"));
        Assert.Equal(2, Assert.Single(plans.GetProperty("items").EnumerateArray()).GetProperty("scopeSummary")[0].GetProperty("segmentCount").GetInt32());
        var tasks = await Body(await pm.GetAsync($"/api/v1/projects/{seed.Project.Id}/survey-tasks"));
        var summary = Assert.Single(tasks.GetProperty("items").EnumerateArray());
        Assert.Equal(plan.Id, summary.GetProperty("planId").GetGuid()); Assert.Equal(seed.Operator.DisplayName, summary.GetProperty("operatorDisplayName").GetString());
        Assert.Equal(0, summary.GetProperty("datasetCount").GetInt32()); Assert.Equal("NEW_ASSIGNED", summary.GetProperty("status").GetString());
        var actorsPath = $"/api/v1/projects/{seed.Project.Id}/task-assignees?taskIds={task.Id}";
        var actors = await Body(await pm.GetAsync(actorsPath));
        Assert.Equal(seed.Operator.DisplayName, Assert.Single(actors.EnumerateArray()).GetProperty("displayName").GetString());
        await using (var db = sql.CreateDbContext())
        {
            await db.SurveyAssignments.Where(row => row.Id == assignment.Id).ExecuteUpdateAsync(set => set.SetProperty(row => row.EndedAt, DateTimeOffset.UtcNow));
            await db.ProjectMembers.Where(row => row.ProjectId == seed.Project.Id && row.UserId == seed.Operator.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        }
        var historical = Assert.Single((await Body(await pm.GetAsync(actorsPath))).EnumerateArray());
        Assert.False(historical.GetProperty("current").GetBoolean());
        Assert.Equal("CURRENT_ACCOUNT", historical.GetProperty("nameSource").GetString());
        using var sup = await WebCookieTestClient.Login(factory, seed.Supervisor.Email!);
        Assert.Equal(HttpStatusCode.OK, (await sup.GetAsync($"/api/v1/projects/{seed.Project.Id}/survey-tasks")).StatusCode);
    }

    [Fact]
    public async Task UpgradeFromPreviousMigrationPreservesRowsAndExportsSchemaEvidence()
    {
        await using var isolated = new OwnedMigrationFixture(); await isolated.InitializeAsync();
        await using var db = isolated.Sql.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261117101500_ProjectHandoverStaging");
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Projects (Id,ProjectCode,Name,Status,CreatedAt) VALUES ({id},{id.ToString()},{"Legacy survivor"},1,{DateTimeOffset.UtcNow})");
        await migrator.MigrateAsync();
        var row = await db.Projects.SingleAsync(row => row.Id == id); Assert.Null(row.Locality); Assert.Equal("Legacy survivor", row.Name);
        var schema = await SandboxSeedGuard.ReadSchemaAsync(db);
        var output = Environment.GetEnvironmentVariable("RG_WEB13_SCHEMA_EVIDENCE");
        if (output is not null) await File.WriteAllTextAsync(output, JsonSerializer.Serialize(schema));
        Assert.EndsWith("_WebPmCoordinationMetadata", schema.Migrations[^1]);
        Assert.False(db.Database.HasPendingModelChanges());
    }
    private sealed class OwnedMigrationFixture : IAsyncDisposable
    {
        public AuthenticationSqlServerFixture Sql { get; } = new();
        public Task InitializeAsync() => Sql.InitializeAtMigrationAsync("20261117101500_ProjectHandoverStaging");
        public async ValueTask DisposeAsync() => await Sql.DisposeAsync();
    }

    [Fact]
    public async Task PolicyDiscoveryUsesActualDraftAndRevisionVersionsAndKeepsRevokedHistory()
    {
        var seed = await Seed();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        using var wrong = await WebCookieTestClient.Login(factory, seed.Other.Email!);
        var root = $"/api/v1/projects/{seed.Project.Id}/repair-policies";
        async Task<HttpResponseMessage> Command(string path, object input, string? version = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", '"' + version + '"');
            return await pm.SendAsync(request);
        }
        var draft = await Body(await Command(root + "/drafts", new RepairPolicyDefinitionInput(seed.Defect.DefectTypeCode,
            "discovery-v1", [], [], "policy fixture")), HttpStatusCode.Created);
        var drafts = await Body(await pm.GetAsync(root + "?kind=DRAFT&limit=1"));
        Assert.Equal(draft.GetProperty("id").GetGuid(), Assert.Single(drafts.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var revision = await Body(await Command(root + $"/drafts/{draft.GetProperty("id").GetGuid()}/publish",
            new RepairPolicyReasonInput("Published for discovery"), draft.GetProperty("version").GetString()), HttpStatusCode.Created);
        var candidates = await Body(await pm.GetAsync(root + "?applicableOnly=true&defectTypeCode=" + seed.Defect.DefectTypeCode));
        var chosen = Assert.Single(candidates.GetProperty("items").EnumerateArray());
        Assert.Equal(revision.GetProperty("version").GetString(), chosen.GetProperty("version").GetString());
        var revisionId = revision.GetProperty("id").GetGuid();
        await Body(await Command(root + $"/revisions/{revisionId}/revoke", new RepairPolicyReasonInput("No longer applicable"),
            chosen.GetProperty("version").GetString()), HttpStatusCode.Created);
        Assert.Empty((await Body(await pm.GetAsync(root + "?applicableOnly=true"))).GetProperty("items").EnumerateArray());
        var history = await Body(await pm.GetAsync(root + $"/revisions/{revisionId}"));
        Assert.Equal("REVOKED", history.GetProperty("state").GetString());
        Assert.Equal("Published for discovery", history.GetProperty("publishReason").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await wrong.GetAsync(root)).StatusCode);
        using var reload = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        Assert.Equal("REVOKED", Assert.Single((await Body(await reload.GetAsync(root))).GetProperty("items").EnumerateArray()).GetProperty("state").GetString());
    }

    [Fact]
    public async Task OpenDefectMetricUsesPersistedLifecycleAndKeepsLegacyUncertaintyExplicit()
    {
        var seed = await Seed();
        var now = DateTimeOffset.UtcNow;
        var rows = new[] { DefectStatus.Open, DefectStatus.Closed, DefectStatus.Rejected, DefectStatus.Resolved }
            .Select(status => Defect.Create(Guid.NewGuid(), seed.Project.Id, seed.Route.Id, null, seed.Defect.DefectTypeCode,
                null, DefectSeverity.Medium, status, new Point(500020, 1100000) { SRID = 32648 }, now)).ToArray();
        await using var db = sql.CreateDbContext(); db.AddRange(rows); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        var path = $"/api/v1/projects/overviews?ids={seed.Project.Id}";
        var metric = (await Body(await pm.GetAsync(path)))[0].GetProperty("openDefects");
        Assert.Equal(JsonValueKind.Null, metric.GetProperty("value").ValueKind);
        Assert.Equal(1, metric.GetProperty("unknownCount").GetInt64());
        Assert.Equal("INCOMPLETE", metric.GetProperty("availability").GetString());
        Assert.Equal("LEGACY_DEFECT_STATUS_UNCLASSIFIED", Assert.Single(metric.GetProperty("reasonCodes").EnumerateArray()).GetString());
        // Projection fixture updates persisted status; lifecycle command authority remains in its own tests.
        var legacy = rows.Single(row => row.Status == DefectStatus.Resolved).Id;
        await db.Defects.Where(row => row.Id == legacy).ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, DefectStatus.Open));
        metric = (await Body(await pm.GetAsync(path)))[0].GetProperty("openDefects");
        Assert.Equal(3, metric.GetProperty("value").GetInt64());
        Assert.Equal("AVAILABLE", metric.GetProperty("availability").GetString());
        Assert.Empty(metric.GetProperty("reasonCodes").EnumerateArray());
        var closed = rows.Single(row => row.Status == DefectStatus.Closed).Id;
        await db.Defects.Where(row => row.Id == closed).ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, DefectStatus.Open));
        Assert.Equal(4, (await Body(await pm.GetAsync(path)))[0].GetProperty("openDefects").GetProperty("value").GetInt64());
    }

    [Fact]
    public async Task RuntimeOpenApiDescribesWebRangesBatchAndScopedExport()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var swagger = factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        foreach (var path in new[] { "/api/v1/projects/overviews", "/api/v1/notifications/read-batch",
            "/api/v1/projects/{projectId}/member-contacts", "/api/v1/projects/{projectId}/repair-policies" })
        {
            var operation = swagger.Paths[path].Operations.Values.Single();
            Assert.Contains(operation.Security, row => row.Keys.Any(key => key.Reference?.Id == "Bearer"));
            Assert.Contains(operation.Security, row => row.Keys.Any(key => key.Reference?.Id == "WebSession"));
        }
        var download = swagger.Paths["/api/v1/files/{fileId}/content"].Operations[Microsoft.OpenApi.Models.OperationType.Get];
        Assert.Contains("206", download.Responses.Keys); Assert.Contains("416", download.Responses.Keys);
        Assert.Contains(download.Parameters, row => row.Name == "Range");
        Assert.Contains(swagger.Components.Schemas.Values, row => row.Properties.ContainsKey("repairItemIds"));
        var output = Environment.GetEnvironmentVariable("RG_WEB13_OPENAPI_EVIDENCE");
        if (output is not null)
        {
            using var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            var writer = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(text);
            swagger.SerializeAsV3(writer);
            await File.WriteAllTextAsync(output, text.ToString());
        }
    }

    [MinioSmokeFact]
    public async Task ScopedRepairExportPersistsOnlySelectedItemAndRendersPdfAndZipToRealStorage()
    {
        var seed = await Seed();
        using var exportLogs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, allowExternalStorage: true, loggerProvider: exportLogs,
            configureTestServices: services => services.PostConfigure<ExportOptions>(options =>
                options.UnicodeFontPath = Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT") ?? throw new InvalidOperationException("A licensed Unicode font is required.")));
        // Persist a real verified source object, an immutable attempt and a different item.
        // This is a SQL history fixture, not a claim that the Crew command workflow was executed here.
        var now = DateTimeOffset.UtcNow;
        var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6VVAAAAAASUVORK5CYII=");
        var hash = Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant();
        var fileId = Guid.NewGuid(); var objectKey = $"uploads/{fileId:N}";
        var surveyTask = SurveyRequest.Create(Guid.NewGuid(), seed.Project.Id, seed.Road.Id, null, seed.Pm.Id,
            SurveyType.Original, SurveyRequestStatus.Submitted, now, roadSectionVersionId: seed.Route.Id);
        var survey = Survey.Create(Guid.NewGuid(), surveyTask.Id, seed.Project.Id, seed.Route.Id,
            SurveyType.Original, SurveyStatus.Completed, false, null, null);
        var task = FieldInspectionTask.Create(Guid.NewGuid(), "EXPORT-" + fileId.ToString("N"), seed.Project.Id,
            seed.Defect.Id, survey.Id, seed.Route.Id, 1, "{}", null, null, now.AddDays(1),
            FieldInspectionTaskStatus.NewAssigned, seed.Pm.Id, null, null, null, null);
        var crew = await sql.CreateUserAsync("export-crew-" + fileId.ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crew.Id, seed.Pm.Id, now, null,
            FieldInspectionAssignmentStatus.Active, null);
        var file = StoredFile.Create(fileId, objectKey, "after.png", "image/png", image.Length, hash, crew.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), fileId, crew.Id, objectKey, "AFTER", "image/png", image.Length, hash, 8388608, now.AddHours(1));
        upload.StartUploading("fixture", now);
        var outside = RepairItem.ProposeWithPlan(Guid.NewGuid(), seed.Package.Obligations[^1], RepairMode.Normal,
            seed.Pm.Id, UserRoleCode.ProjectManager, now, new("MUST NOT LEAK INTO SELECTED DOSSIER", "outside-v1"));
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(surveyTask, survey, task, assignment, file, upload,
                FileScope.Create(Guid.NewGuid(), fileId, seed.Project.Id, task.Id, crew.Id, "AFTER", now));
            await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
            await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
            var assignedItem = await db.Set<RepairItem>().SingleAsync(row => row.Id == seed.Item.Id);
            assignedItem.Approve(seed.Supervisor.Id, UserRoleCode.Supervisor, now);
            assignedItem.Assign(crew.Id, seed.Pm.Id, UserRoleCode.ProjectManager, now);
            await db.SaveChangesAsync();
            db.Add(RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), seed.Item.Id, seed.Item.ObligationId,
                seed.Project.Id, seed.Defect.Id, crew.Id, task.Id, assignment.Id, null, "stored-location", null, true, null,
                now.AddMinutes(-2), now.AddMinutes(-1), now, RepairTimeProvenance.VerifiedOnline,
                [new(fileId, Convert.ToBase64String(upload.RowVersion), hash, RepairEvidencePurpose.After, true, true,
                    "FIELD", task.Id, now, null, false)]));
            var package = await db.Set<RepairPackage>().Include(row => row.Items).Include(row => row.Obligations).SingleAsync(row => row.Id == seed.Package.Id);
            package.AddItem(outside); db.Entry(package).Property<long>("MutationRevision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        var storage = factory.Services.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
        using var s3 = new AmazonS3Client(storage.AccessKey, storage.SecretKey,
            new AmazonS3Config { ServiceURL = storage.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = storage.BucketName,
            Key = objectKey,
            InputStream = new MemoryStream(image),
            ContentType = "image/png"
        });
        using var pm = await WebCookieTestClient.Login(factory, seed.Pm.Email!);
        using var wrong = await WebCookieTestClient.Login(factory, seed.Other.Email!);
        var root = $"/api/v1/projects/{seed.Project.Id}/exports";
        Guid lastExportId = default;
        foreach (var format in new[] { "PDF", "ZIP" })
        {
            var key = Guid.NewGuid().ToString("N");
            var input = new { kind = "DOSSIER", format, repairItemIds = new[] { seed.Item.Id }, includeOriginalFiles = format == "ZIP" };
            async Task<HttpResponseMessage> Create(HttpClient client)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, root) { Content = JsonContent.Create(input) };
                request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
            }
            Assert.Equal(HttpStatusCode.Forbidden, (await Create(wrong)).StatusCode);
            var createResponse = await Create(pm);
            Assert.True(createResponse.StatusCode == HttpStatusCode.Accepted, exportLogs.CombinedEntries);
            var admitted = await Body(createResponse, HttpStatusCode.Accepted); var id = admitted.GetProperty("id").GetGuid();
            lastExportId = id;
            Assert.Equal(id, (await Body(await Create(pm), HttpStatusCode.Accepted)).GetProperty("id").GetGuid());
            for (var retry = 0; retry < 5; retry++)
            {
                using var scope = factory.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default);
                var status = await Body(await pm.GetAsync(root + $"/{id}"));
                if (status.GetProperty("status").GetString() == "SUCCEEDED") break;
                Assert.NotEqual("FAILED", status.GetProperty("status").GetString());
                await Task.Delay(150);
            }
            var completed = await Body(await pm.GetAsync(root + $"/{id}")); Assert.Equal("SUCCEEDED", completed.GetProperty("status").GetString());
            var manifest = await Body(await pm.GetAsync(root + $"/{id}/manifest"));
            Assert.Equal(seed.Item.Id, manifest.GetProperty("filters").GetProperty("repairItemIds")[0].GetGuid());
            Assert.DoesNotContain(manifest.GetProperty("sourceRevisions").EnumerateArray(), row => row.GetProperty("kind").GetString() == "Survey");
            Assert.Contains(manifest.GetProperty("sections").EnumerateArray(), row => row.GetProperty("name").GetString()!.EndsWith("/finalConfirmation", StringComparison.Ordinal) && row.GetProperty("availability").GetString() == "UNAVAILABLE");
            using var content = await pm.GetAsync(root + $"/{id}/content"); Assert.Equal(HttpStatusCode.OK, content.StatusCode);
            var bytes = await content.Content.ReadAsByteArrayAsync(); Assert.NotEmpty(bytes);
            using var proofStream = new MemoryStream(bytes);
            Assert.True(await ExportArchive.ProvesSnapshotAsync(proofStream,
                JsonSerializer.Deserialize<RoadGuardSystem.DTOs.Exports.ExportManifestV1Dto>(manifest.GetRawText(), ExportSerialization.Options)!, default));
            if (format == "PDF") Assert.True(bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8));
            else
            {
                using var zip = new ZipArchive(new MemoryStream(bytes));
                Assert.NotNull(zip.GetEntry("dossier.pdf"));
                using var original = new MemoryStream();
                await zip.GetEntry($"repair-evidence/{fileId:D}.png")!.Open().CopyToAsync(original);
                Assert.Equal(image, original.ToArray());
                using var snapshot = JsonDocument.Parse(zip.GetEntry("repair-dossier.json")!.Open());
                var selected = Assert.Single(snapshot.RootElement.GetProperty("items").EnumerateArray());
                Assert.Equal(seed.Item.Id, selected.GetProperty("history").GetProperty("item").GetProperty("id").GetGuid());
                Assert.Single(selected.GetProperty("history").GetProperty("attempts").EnumerateArray());
                Assert.DoesNotContain(outside.Id.ToString(), snapshot.RootElement.GetRawText());
            }
            Assert.Equal(HttpStatusCode.Forbidden, (await wrong.GetAsync(root + $"/{id}/content")).StatusCode);
        }
        var packageRequest = new HttpRequestMessage(HttpMethod.Post, root)
        { Content = JsonContent.Create(new { kind = "DOSSIER", format = "PDF", repairPackageIds = new[] { seed.Package.Id } }) };
        packageRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var packageExport = await Body(await pm.SendAsync(packageRequest), HttpStatusCode.Accepted);
        var packageManifest = await Body(await pm.GetAsync(root + $"/{packageExport.GetProperty("id").GetGuid()}/manifest"));
        Assert.Equal(2, packageManifest.GetProperty("sourceRevisions").EnumerateArray().Count(row => row.GetProperty("kind").GetString() == "RepairItem"));
        await using (var revoked = sql.CreateDbContext())
            await revoked.ProjectMembers.Where(row => row.ProjectId == seed.Project.Id && row.UserId == seed.Pm.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(root + $"/{lastExportId}/content")).StatusCode);
        await s3.DeleteObjectAsync(storage.BucketName, objectKey);
    }
}
