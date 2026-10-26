using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class NotificationSupervisorHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactAssignmentWireRechecksCookieCsrfBearerAuthorityAndHistoricalReplay(bool cookie)
    {
        var pm = await fixture.CreateUserAsync($"assignment-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync($"assignment-sv-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var source = await Seed(pm.Id);
        var path = $"/api/v1/projects/{source.Project}/repair-packages/{source.Package}/items/{source.Item}/notification-supervisor";
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var anonymous = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.True(anonymous.Headers.CacheControl?.NoStore);
        Assert.True(anonymous.Headers.CacheControl?.Private);
        if (cookie)
        {
            await Csrf(client);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = pm.Email, password = "Current1!" })).StatusCode);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            var csrfDenied = await Send(client, HttpMethod.Put, path, "no-csrf", "*", null, supervisor.Id);
            Assert.Equal(HttpStatusCode.Forbidden, csrfDenied.StatusCode);
            Assert.True(csrfDenied.Headers.CacheControl?.NoStore);
            Assert.True(csrfDenied.Headers.CacheControl?.Private);
            await Csrf(client);
        }
        else
        {
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = pm.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await Send(client, HttpMethod.Put, path, "missing-condition", null, null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "invalid-body", "*", null, supervisor.Id, "{\"supervisorId\":\"" + supervisor.Id + "\",\"extra\":1}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "duplicate-body", "*", null, supervisor.Id, "{\"supervisorId\":\"" + supervisor.Id + "\",\"supervisorId\":\"" + supervisor.Id + "\"}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "both-conditions", "*", "\"AQIDBAUGBwg=\"", supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "malformed-tag", null, "W/\"AQIDBAUGBwg=\"", supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Put, path, "wrong-recipient", "*", null, pm.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Put, path.Replace(source.Package.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "wrong-parent", "*", null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Put, path.Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "wrong-project", "*", null, supervisor.Id)).StatusCode);
        var created = await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(path, created.Headers.Location!.OriginalString);
        Assert.True(created.Headers.CacheControl!.NoStore); Assert.True(created.Headers.CacheControl.Private);
        var value = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "assignedAt", "assignmentId", "revokedAt", "status", "supervisorId", "version" }, value.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal("ACTIVE", value.GetProperty("status").GetString());
        Assert.Equal('"' + value.GetProperty("version").GetString() + '"', created.Headers.ETag!.ToString());
        var read = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.True(read.Headers.CacheControl?.NoStore);
        Assert.True(read.Headers.CacheControl?.Private);
        Assert.Equal(created.Headers.ETag, read.Headers.ETag);
        var readValue = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(value.GetRawText(), readValue.GetRawText());
        using var recipientClient = factory.CreateClient();
        var recipientLogin = await recipientClient.PostAsJsonAsync("/api/v1/auth/login", new { email = supervisor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, recipientLogin.StatusCode);
        recipientClient.DefaultRequestHeaders.Authorization = new("Bearer", (await recipientLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        // Global recipient authority grants no PM assignment-management authority.
        Assert.Equal(HttpStatusCode.Forbidden, (await recipientClient.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(recipientClient, HttpMethod.Put, path, "recipient-cannot-assign", "*", null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(recipientClient, HttpMethod.Delete, path, "recipient-cannot-revoke", null, created.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Put, path, "create", "*", null, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(client, HttpMethod.Put, path, "duplicate-create", "*", null, supervisor.Id)).StatusCode);
        var tag = created.Headers.ETag.ToString();
        var replacement = await Send(client, HttpMethod.Put, path, "replace", null, tag, supervisor.Id);
        Assert.Equal(HttpStatusCode.OK, replacement.StatusCode);
        Assert.NotEqual(tag, replacement.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(client, HttpMethod.Delete, path, "stale", null, tag)).StatusCode);
        var removed = await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString());
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Delete, path, "remove-again", null, removed.Headers.ETag!.ToString())).StatusCode);
        var historical = await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id);
        Assert.Equal(HttpStatusCode.Created, historical.StatusCode); Assert.Equal(tag, historical.Headers.ETag!.ToString());
        Assert.Equal("REVOKED", (await (await client.GetAsync(path)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        await using (var db = fixture.CreateDbContext())
        {
            Assert.False(await db.ProjectMembers.AnyAsync(x => x.UserId == supervisor.Id));
            Assert.Equal(2, await db.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == source.Item));
            Assert.Equal(3, await db.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == source.Item));
            await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id)).StatusCode);
        await using (var db = fixture.CreateDbContext())
            await db.Sessions.Where(x => x.UserId == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentsExactAssignmentBodyConditionsAndBothAuthenticationTransports()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(json);
        var route = document.RootElement.GetProperty("paths").GetProperty("/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor");
        foreach (var method in new[] { "get", "put", "delete" })
        {
            var operation = route.GetProperty(method);
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), x => x.TryGetProperty("Bearer", out _));
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), x => x.TryGetProperty("WebSession", out _));
            Assert.True(operation.GetProperty("responses").TryGetProperty("404", out _));
        }
        var body = route.GetProperty("put").GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.False(body.GetProperty("additionalProperties").GetBoolean());
        Assert.Single(body.GetProperty("properties").EnumerateObject());
        Assert.Equal("uuid", body.GetProperty("properties").GetProperty("supervisorId").GetProperty("format").GetString());
        var destination = Environment.GetEnvironmentVariable("ROADGUARD_TEST_OPENAPI_OUTPUT");
        if (!string.IsNullOrWhiteSpace(destination)) await File.WriteAllTextAsync(destination, json);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string key, string? none = null, string? match = null, Guid? supervisor = null, string? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Idempotency-Key", key);
        if (none is not null) request.Headers.TryAddWithoutValidation("If-None-Match", none);
        if (match is not null) request.Headers.TryAddWithoutValidation("If-Match", match);
        if (method == HttpMethod.Put) request.Content = new StringContent(body ?? JsonSerializer.Serialize(new { supervisorId = supervisor }), Encoding.UTF8, "application/json");
        return client.SendAsync(request);
    }
    private static async Task Csrf(HttpClient client)
    {
        var token = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
    }
    private async Task<(Guid Project, Guid Package, Guid Item)> Seed(Guid actor)
    {
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Assignment HTTP resource fixture", null, null, null, null, now);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor, DateOnly.FromDateTime(now.UtcDateTime));
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "R"); var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "No field accuracy assertion");
        var type = DefectType.Create("B2" + Guid.NewGuid().ToString("N"), "Assignment fixture");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, member, road, route, type, defect); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true, RepairActualScope.Create(Guid.NewGuid(), road.Id, "fixture-v1", "R", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, actor, UserRoleCode.ProjectManager, now, new("Assignment-only fixture", "checklist-v1"));
        package.AddItem(item); db.Add(package); await db.SaveChangesAsync();
        // Resource fixture only; genuine lifecycle/eligibility is tested separately by the producer journey.
        db.Entry(obligation).Property(x => x.CurrentRepairItemId).CurrentValue = item.Id;
        await db.SaveChangesAsync();
        return (project.Id, package.Id, item.Id);
    }
}
