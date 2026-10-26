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
public sealed class AssignmentExactWireHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactManagementWirePersistsFiniteStateAndReplaysEveryReceiptWithoutRestoration(bool cookie)
    {
        var pm = await fixture.CreateUserAsync($"assignment-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync($"assignment-sv-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var source = await Seed(pm.Id);
        var secondSource = await Seed(pm.Id); // Same PM, real second project: scope must still match every parent.
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
        foreach (var body in new[] { "null", "[]", "{", "{}", "{\"supervisorId\":1}", "{\"supervisorId\":\"00000000-0000-0000-0000-000000000000\"}" })
        {
            using var invalid = await Send(client, HttpMethod.Put, path, "bad-body", "*", null, supervisor.Id, body);
            await Error(invalid, 400, "validation_error");
        }
        foreach (var method in new[] { HttpMethod.Put, HttpMethod.Delete })
        {
            using var missing = await Send(client, method, path, "missing-condition-exact", null, null, supervisor.Id);
            await Error(missing, 428, "precondition_required");
            using var malformed = await Send(client, method, path, "bad-tag-exact", null, "\"bad-base64\"", supervisor.Id);
            await Error(malformed, 400, "validation_error");
        }
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await Send(client, HttpMethod.Put, path, "missing-condition", null, null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "invalid-body", "*", null, supervisor.Id, "{\"supervisorId\":\"" + supervisor.Id + "\",\"extra\":1}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "duplicate-body", "*", null, supervisor.Id, "{\"supervisorId\":\"" + supervisor.Id + "\",\"supervisorId\":\"" + supervisor.Id + "\"}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "both-conditions", "*", "\"AQIDBAUGBwg=\"", supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, path, "malformed-tag", null, "W/\"AQIDBAUGBwg=\"", supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Put, path, "wrong-recipient", "*", null, pm.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Put, path.Replace(source.Package.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "wrong-parent", "*", null, supervisor.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Put, path.Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "wrong-project", "*", null, supervisor.Id)).StatusCode);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete })
        {
            using var denied = await Send(client, method, path.Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "project-denied", null, "\"AQIDBAUGBwg=\"", supervisor.Id);
            await Error(denied, 403, "access_forbidden");
            using var parent = await Send(client, method, path.Replace(source.Package.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), "parent-denied", null, "\"AQIDBAUGBwg=\"", supervisor.Id);
            await Error(parent, 404, "not_found");
            using var realOtherProject = await Send(client, method,
                path.Replace(source.Project.ToString(), secondSource.Project.ToString(), StringComparison.Ordinal),
                "real-project-mismatch", null, "\"AQIDBAUGBwg=\"", supervisor.Id);
            await Error(realOtherProject, 404, "not_found");
            using var realOtherPackage = await Send(client, method,
                path.Replace(source.Package.ToString(), secondSource.Package.ToString(), StringComparison.Ordinal),
                "real-package-mismatch", null, "\"AQIDBAUGBwg=\"", supervisor.Id);
            await Error(realOtherPackage, 404, "not_found");
        }
        var before = DateTimeOffset.UtcNow;
        var created = await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(path, created.Headers.Location!.OriginalString);
        Assert.True(created.Headers.CacheControl!.NoStore); Assert.True(created.Headers.CacheControl.Private);
        var value = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "assignedAt", "assignmentId", "revokedAt", "status", "supervisorId", "version" }, value.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal("ACTIVE", value.GetProperty("status").GetString());
        Wire(created);
        Assert.NotEqual(Guid.Empty, value.GetProperty("assignmentId").GetGuid());
        Assert.Equal(supervisor.Id, value.GetProperty("supervisorId").GetGuid());
        Assert.InRange(value.GetProperty("assignedAt").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal(JsonValueKind.Null, value.GetProperty("revokedAt").ValueKind);
        Assert.Equal(8, Convert.FromBase64String(value.GetProperty("version").GetString()!).Length);
        Assert.Equal('"' + value.GetProperty("version").GetString() + '"', created.Headers.ETag!.ToString());
        var read = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.True(read.Headers.CacheControl?.NoStore);
        Assert.True(read.Headers.CacheControl?.Private);
        Wire(read); Assert.Equal(created.Headers.ETag, read.Headers.ETag);
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
        Wire(replacement); Assert.NotEqual(tag, replacement.Headers.ETag!.ToString());
        var replacementJson = await replacement.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(value.EnumerateObject().Select(x => x.Name).Order(), replacementJson.EnumerateObject().Select(x => x.Name).Order());
        Assert.NotEqual(value.GetProperty("assignmentId").GetGuid(), replacementJson.GetProperty("assignmentId").GetGuid());
        Assert.Equal(supervisor.Id, replacementJson.GetProperty("supervisorId").GetGuid());
        Assert.Equal("ACTIVE", replacementJson.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, replacementJson.GetProperty("revokedAt").ValueKind);
        Assert.Equal('"' + replacementJson.GetProperty("version").GetString() + '"', replacement.Headers.ETag.ToString());
        using var replaceReplay = await Send(client, HttpMethod.Put, path, "replace", null, tag, supervisor.Id);
        Assert.Equal(HttpStatusCode.OK, replaceReplay.StatusCode);
        Assert.Equal(replacementJson.GetRawText(), (await replaceReplay.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        Assert.Equal(replacement.Headers.ETag, replaceReplay.Headers.ETag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(client, HttpMethod.Delete, path, "stale", null, tag)).StatusCode);
        var removed = await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString());
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode); Wire(removed, false);
        Assert.Equal("", await removed.Content.ReadAsStringAsync());
        Assert.NotEqual(replacement.Headers.ETag, removed.Headers.ETag);
        using var removeReplay = await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString());
        Assert.Equal(HttpStatusCode.NoContent, removeReplay.StatusCode);
        Assert.Equal("", await removeReplay.Content.ReadAsStringAsync()); Assert.Equal(removed.Headers.ETag, removeReplay.Headers.ETag);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Delete, path, "remove-again", null, removed.Headers.ETag!.ToString())).StatusCode);
        using var changedDelete = await Send(client, HttpMethod.Delete, path, "remove", null, tag);
        await Error(changedDelete, 409, "idempotency_key_reused");
        using var changedMethod = await Send(client, HttpMethod.Put, path, "remove", null, replacement.Headers.ETag.ToString(), supervisor.Id);
        await Error(changedMethod, 409, "idempotency_key_reused");
        var historical = await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id);
        Assert.Equal(HttpStatusCode.Created, historical.StatusCode); Assert.Equal(tag, historical.Headers.ETag!.ToString());
        Assert.Equal(value.GetRawText(), (await historical.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        using var historicalReplace = await Send(client, HttpMethod.Put, path, "replace", null, tag, supervisor.Id);
        Assert.Equal(HttpStatusCode.OK, historicalReplace.StatusCode);
        Assert.Equal(replacementJson.GetRawText(), (await historicalReplace.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        Assert.Equal(replacement.Headers.ETag, historicalReplace.Headers.ETag);
        using var current = await client.GetAsync(path); Wire(current);
        Assert.Equal(removed.Headers.ETag, current.Headers.ETag);
        var currentJson = await current.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(replacementJson.GetProperty("assignmentId").GetGuid(), currentJson.GetProperty("assignmentId").GetGuid());
        Assert.InRange(currentJson.GetProperty("revokedAt").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal("REVOKED", currentJson.GetProperty("status").GetString());
        Assert.Equal("REVOKED", (await (await client.GetAsync(path)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        await using (var db = fixture.CreateDbContext())
        {
            Assert.False(await db.ProjectMembers.AnyAsync(x => x.UserId == supervisor.Id));
            Assert.Equal(2, await db.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == source.Item));
            var audits = await db.Set<NotificationSupervisorAudit>().Where(x => x.ItemId == source.Item).ToArrayAsync();
            Assert.Equal(3, audits.Length); Assert.All(audits, x => Assert.Equal(pm.Id, x.ActorId));
            Assert.Equal(new[] { "CREATED", "REPLACED", "REVOKED" }, audits.Select(x => x.Action).Order());
            Assert.All(audits, x => Assert.InRange(x.At, before, DateTimeOffset.UtcNow));
            Assert.Equal(3, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == pm.Id));
            await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        using var deniedDelete = await Send(client, HttpMethod.Delete, path, "remove", null, replacement.Headers.ETag.ToString());
        await Error(deniedDelete, 403, "access_forbidden");
        await using (var preserved = fixture.CreateDbContext())
        {
            Assert.Equal(3, await preserved.IdempotencyRecords.CountAsync(x => x.ActorUserId == pm.Id));
            Assert.Equal(3, await preserved.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == source.Item));
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Put, path, "create", "*", null, supervisor.Id)).StatusCode);
        // Same key in a different resource is independent; it must not replay the revoked first head.
        var secondPath = $"/api/v1/projects/{secondSource.Project}/repair-packages/{secondSource.Package}/items/{secondSource.Item}/notification-supervisor";
        using var separateResource = await Send(client, HttpMethod.Put, secondPath, "create", "*", null, supervisor.Id);
        Assert.Equal(HttpStatusCode.Created, separateResource.StatusCode);
        Assert.NotEqual(value.GetProperty("assignmentId").GetGuid(),
            (await separateResource.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("assignmentId").GetGuid());
        var otherPm = await fixture.CreateUserAsync($"assignment-other-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using (var db = fixture.CreateDbContext())
        {
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = secondSource.Project,
                UserId = otherPm.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                IsPrimary = false,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            await db.SaveChangesAsync();
        }
        using var otherClient = factory.CreateClient();
        var otherLogin = await otherClient.PostAsJsonAsync("/api/v1/auth/login", new { email = otherPm.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, otherLogin.StatusCode);
        otherClient.DefaultRequestHeaders.Authorization = new("Bearer", (await otherLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete })
        {
            using var actualOutsideAuthority = await Send(otherClient, method, path, "actual-outside-project", null, replacement.Headers.ETag.ToString(), supervisor.Id);
            await Error(actualOutsideAuthority, 403, "access_forbidden");
        }
        using var separateActor = await Send(otherClient, HttpMethod.Put, secondPath, "create", "*", null, supervisor.Id);
        await Error(separateActor, 412, "precondition_failed"); // Neither another PM's receipt201 nor a global-key409.
        using var separateDelete = await Send(client, HttpMethod.Delete, secondPath, "remove", null, separateResource.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.NoContent, separateDelete.StatusCode);
        Assert.Equal("", await separateDelete.Content.ReadAsStringAsync());
        using var otherDelete = await Send(otherClient, HttpMethod.Delete, secondPath, "remove", null, separateResource.Headers.ETag.ToString());
        await Error(otherDelete, 412, "precondition_failed");
        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal(5, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == pm.Id));
            Assert.Empty(await db.IdempotencyRecords.Where(x => x.ActorUserId == otherPm.Id).ToArrayAsync());
        }
        await using (var db = fixture.CreateDbContext())
            await db.Sessions.Where(x => x.UserId == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    private static void Wire(HttpResponseMessage response, bool json = true)
    {
        Assert.True(response.Headers.CacheControl?.NoStore); Assert.True(response.Headers.CacheControl?.Private);
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        Assert.NotNull(response.Headers.ETag);
        if (json) Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
    private static async Task Error(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore); Assert.True(response.Headers.CacheControl?.Private);
        Assert.Null(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, body.GetProperty("status").GetInt32()); Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
        Assert.All(body.EnumerateObject(), property => Assert.Contains(property.Name,
            new[] { "type", "title", "status", "detail", "instance", "code", "correlationId" }));
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
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
