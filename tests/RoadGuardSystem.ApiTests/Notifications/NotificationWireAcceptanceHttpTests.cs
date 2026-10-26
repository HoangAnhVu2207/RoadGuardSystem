using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

// SQL-backed consumer wire tests. Domain-valid technical clocks/notifications do not
// certify a repair producer or authorize any pending clock transition policy.
public sealed class NotificationWireAcceptanceHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task InboxListDetailScopeAndReadHaveExactWireAndDurableLegacyAudit()
    {
        var (actor, project) = await Seed();
        var at = DateTimeOffset.UtcNow.AddMinutes(-1);
        var first = Notification.Create(Guid.NewGuid(), actor, "Project", project, "TEST_ONLY", "private title", "Finite inbox body", at);
        var second = Notification.Create(Guid.NewGuid(), actor, "Project", project, "TEST_ONLY_SECOND", "private title", "Second body", at.AddMinutes(-1));
        var unknown = Notification.Create(Guid.NewGuid(), actor, "Unregistered", Guid.NewGuid(), "TEST_ONLY", "private", "DO_NOT_LEAK_UNKNOWN", at);
        await using (var db = sql.CreateDbContext()) { db.AddRange(first, second, unknown); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Client(factory, actor);
        var root = "/api/v1/notifications";
        var firstPage = await Get(client, root + "?limit=1");
        Fields(firstPage, "items", "nextCursor", "asOf");
        Assert.Equal(JsonValueKind.String, firstPage.GetProperty("nextCursor").ValueKind);
        Assert.True(firstPage.GetProperty("asOf").GetDateTimeOffset() >= at);
        var item = Assert.Single(firstPage.GetProperty("items").EnumerateArray());
        AssertNotification(item, first, false);
        var next = await Get(client, root + "?limit=1&cursor=" + Uri.EscapeDataString(firstPage.GetProperty("nextCursor").GetString()!));
        Fields(next, "items", "nextCursor", "asOf");
        AssertNotification(Assert.Single(next.GetProperty("items").EnumerateArray()), second, false);
        Assert.Equal(JsonValueKind.Null, next.GetProperty("nextCursor").ValueKind);
        using var detail = await client.GetAsync(root + "/" + first.Id);
        var detailJson = await Success(detail);
        AssertNotification(detailJson, first, false);
        Assert.Equal("\"" + detailJson.GetProperty("version").GetString() + "\"", detail.Headers.ETag!.ToString());
        var scope = await Get(client, root + "/" + first.Id + "/scope");
        Fields(scope, "notificationId", "projectId", "occurrenceId", "classification", "sourceKind", "sourceId", "eventType", "resolverVersion");
        Assert.Equal(first.Id, scope.GetProperty("notificationId").GetGuid());
        Assert.Equal(project, scope.GetProperty("projectId").GetGuid());
        Assert.Equal(project, scope.GetProperty("sourceId").GetGuid());
        Assert.Equal("PROJECT", scope.GetProperty("classification").GetString());
        Assert.Equal("Project", scope.GetProperty("sourceKind").GetString());
        Assert.Equal("TEST_ONLY", scope.GetProperty("eventType").GetString());
        Assert.Equal(JsonValueKind.Null, scope.GetProperty("occurrenceId").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(scope.GetProperty("resolverVersion").GetString()));
        foreach (var suffix in new[] { "?limit=0", "?limit=101", "?cursor=invalid", "?cursor=" + Uri.EscapeDataString(Convert.ToBase64String("{}"u8.ToArray())) })
            await Problem(await client.GetAsync(root + suffix), 400, "validation_error");
        await Problem(await client.GetAsync(root + "/" + Guid.NewGuid()), 404, "notification_not_found");
        await Problem(await client.GetAsync(root + "/" + unknown.Id + "/scope"), 404, "notification_not_found");
        var key = Guid.NewGuid().ToString("N");
        using var command = new HttpRequestMessage(HttpMethod.Post, root + "/" + first.Id + "/read");
        command.Headers.Add("Idempotency-Key", key); command.Headers.TryAddWithoutValidation("If-Match", detail.Headers.ETag!.ToString());
        using var read = await client.SendAsync(command);
        var readJson = await Success(read); AssertNotification(readJson, first, true);
        Assert.Equal("\"" + readJson.GetProperty("version").GetString() + "\"", read.Headers.ETag!.ToString());
        Assert.NotEqual(detail.Headers.ETag, read.Headers.ETag);
        var reread = await Get(client, root + "/" + first.Id); Assert.True(reread.GetProperty("read").GetBoolean());
        var count = 0;
        await using (var db = sql.CreateDbContext())
        {
            Assert.NotNull((await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id)).ReadAt);
            Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == second.Id)).ReadAt);
            var audits = await db.Set<H6NotificationAuditRow>().AsNoTracking().Where(a => a.NotificationId == first.Id || a.NotificationId == second.Id || a.NotificationId == unknown.Id).ToArrayAsync();
            Assert.Equal(3, audits.Length); Assert.Equal(3, audits.Select(a => a.NotificationId).Distinct().Count());
            Assert.Equal("UNKNOWN_PROTECTED", Assert.Single(audits.Where(a => a.NotificationId == unknown.Id)).Classification);
            count = audits.Length;
        }
        for (var i = 0; i < 2; i++)
        {
            var page = await Get(client, root); Assert.Equal(2, page.GetProperty("items").GetArrayLength());
            Assert.DoesNotContain("DO_NOT_LEAK_UNKNOWN", page.GetRawText());
            await Problem(await client.GetAsync(root + "/" + unknown.Id), 404, "notification_not_found");
        }
        await using var check = sql.CreateDbContext();
        Assert.Equal(count, await check.Set<H6NotificationAuditRow>().CountAsync(a => a.NotificationId == first.Id || a.NotificationId == second.Id || a.NotificationId == unknown.Id));
        Assert.Single(await check.IdempotencyRecords.Where(r => r.ActorUserId == actor).ToArrayAsync());
    }

    [Fact]
    public async Task ClockWireCursorBindingAndStaleAnchorRequireExplicitReload()
    {
        var (actor, project) = await Seed();
        var first = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.ProjectManagerReview, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-2));
        var second = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.ProjectManagerReview, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1));
        await using (var db = sql.CreateDbContext()) { db.AddRange(first, second); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Client(factory, actor);
        var root = $"/api/v1/projects/{project}/clocks";
        var page = await Get(client, root + "?limit=1");
        Fields(page, "status", "items", "continuation"); Assert.Equal("READY", page.GetProperty("status").GetString());
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Fields(item, "id", "projectId", "kind", "targetId", "originEventId", "originAt", "originalDueAt", "currentDueAt", "overdue", "completedAt", "acknowledgedAt", "version", "extensions", "breaches", "pendingCapabilities", "appointedActorId", "appointedRole", "appointments");
        Assert.Equal(first.Id, item.GetProperty("id").GetGuid()); Assert.Equal(project, item.GetProperty("projectId").GetGuid());
        Assert.Equal(first.TargetId, item.GetProperty("targetId").GetGuid()); Assert.Equal(first.OriginEventId, item.GetProperty("originEventId").GetGuid());
        Assert.Equal(first.OriginAt, item.GetProperty("originAt").GetDateTimeOffset()); Assert.Equal(first.CurrentDueAt, item.GetProperty("currentDueAt").GetDateTimeOffset());
        Assert.Equal(first.OriginalDueAt, item.GetProperty("originalDueAt").GetDateTimeOffset()); Assert.False(item.GetProperty("overdue").GetBoolean());
        Assert.Equal("ProjectManagerReview", item.GetProperty("kind").GetString());
        Assert.Equal(8, Convert.FromBase64String(item.GetProperty("version").GetString()!).Length);
        foreach (var name in new[] { "completedAt", "acknowledgedAt", "appointedActorId", "appointedRole" }) Assert.Equal(JsonValueKind.Null, item.GetProperty(name).ValueKind);
        foreach (var name in new[] { "extensions", "breaches", "appointments" }) Assert.Empty(item.GetProperty(name).EnumerateArray());
        Assert.NotEmpty(item.GetProperty("pendingCapabilities").EnumerateArray());
        var cursor = page.GetProperty("continuation"); Fields(cursor, "actorId", "projectId", "role", "afterDueAtUtc", "afterClockId");
        Assert.Equal(actor, cursor.GetProperty("actorId").GetGuid()); Assert.Equal(project, cursor.GetProperty("projectId").GetGuid());
        Assert.Equal("ProjectManager", cursor.GetProperty("role").GetString()); Assert.Equal(first.Id, cursor.GetProperty("afterClockId").GetGuid());
        var cursorText = cursor.GetRawText();
        var next = await Get(client, root + "?limit=1&cursor=" + Uri.EscapeDataString(cursorText));
        Assert.Equal(second.Id, Assert.Single(next.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, next.GetProperty("continuation").ValueKind);
        foreach (var tampered in new[] { cursorText.Replace(actor.ToString(), Guid.NewGuid().ToString()), cursorText.Replace(project.ToString(), Guid.NewGuid().ToString()), cursorText.Replace("ProjectManager", "Supervisor") })
            await Problem(await client.GetAsync(root + "?cursor=" + Uri.EscapeDataString(tampered)), 400, "validation_error");
        foreach (var suffix in new[] { "?limit=0", "?limit=101", "?cursor=null", "?cursor=not-json", "?cursor=" + new string('x', 1025) })
            await Problem(await client.GetAsync(root + suffix), 400, "validation_error");
        await using (var db = sql.CreateDbContext())
        {
            var clock = await db.Set<DeadlineClock>().SingleAsync(c => c.Id == first.Id);
            clock.Extend(Guid.NewGuid(), actor, clock.CurrentDueAt.AddHours(2), "TEST_ONLY cursor anchor change", DateTimeOffset.UtcNow); await db.SaveChangesAsync();
        }
        await Problem(await client.GetAsync(root + "?cursor=" + Uri.EscapeDataString(cursorText)), 409, "cursor_stale");
        var reloaded = await Get(client, root); Assert.Equal(2, reloaded.GetProperty("items").GetArrayLength());
    }

    private async Task<(Guid Actor, Guid Project)> Seed()
    {
        var actor = await sql.CreateUserAsync("INBOX-WIRE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Owned notification wire", null, null, null, null, DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext(); db.Add(project);
        db.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        await db.SaveChangesAsync(); return (actor.Id, project.Id);
    }
    private async Task<HttpClient> Client(AuthenticationWebApplicationFactory factory, Guid actor)
    {
        await using var db = sql.CreateDbContext(); var email = await db.Users.Where(u => u.Id == actor).Select(u => u.Email).SingleAsync();
        var client = factory.CreateClient(); var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()); return client;
    }
    private static void Fields(JsonElement json, params string[] names) => Assert.Equal(names.Order(), json.EnumerateObject().Select(p => p.Name).Order());
    private static void AssertNotification(JsonElement value, Notification row, bool read)
    {
        Fields(value, "id", "message", "resourceId", "read", "occurredAt", "version");
        Assert.Equal(row.Id, value.GetProperty("id").GetGuid()); Assert.Equal(row.Body, value.GetProperty("message").GetString());
        Assert.Equal(row.SourceEntityId, value.GetProperty("resourceId").GetGuid()); Assert.Equal(read, value.GetProperty("read").GetBoolean());
        Assert.Equal(row.OccurredAtUtc, value.GetProperty("occurredAt").GetDateTimeOffset()); Assert.Equal(8, Convert.FromBase64String(value.GetProperty("version").GetString()!).Length);
    }
    private static async Task<JsonElement> Get(HttpClient client, string path) { using var response = await client.GetAsync(path); return await Success(response); }
    private static async Task<JsonElement> Success(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _)); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        using (response)
        {
            Assert.Equal(status, (int)response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType); Assert.Null(response.Headers.ETag);
            Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(status, json.GetProperty("status").GetInt32()); Assert.Equal(code, json.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("title").GetString())); Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("correlationId").GetString()));
            Assert.DoesNotContain("DO_NOT_LEAK_UNKNOWN", json.GetRawText());
        }
    }
}
