using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

[Collection(SandboxMethodOwnedSqlGroup.Name)]
public sealed class SandboxNotificationBoundaryTests
{
    [Fact]
    public async Task RecipientPrivacyCurrentVersionReplayAndStaleVersionPreserveSqlEffects()
    {
        await using var context = new SandboxScenarioContext();
        await context.InitializeCoreAsync();
        context.Fixture.GetOwnedSeedDatabaseIdentity();
        await using var db = context.Fixture.CreateDbContext();
        var recipient = context.Catalog.Actors.Single(a => a.Key == "pm-a");
        var id = Guid.NewGuid();
        // Synthetic inbox fixture only; this does not assert a production event producer or delivery.
        db.Notifications.Add(Notification.Create(id, recipient.Id, "Project", SandboxFixtureIds.ProjectA,
            "SyntheticInboxBoundary", "Synthetic notification", "Synthetic private message", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var original = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        var url = $"/api/v1/notifications/{id}";
        using var owner = await context.LoginAsync("pm-a");
        using var get = await owner.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var etag = get.Headers.ETag!.ToString();
        var body = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Convert.ToBase64String(original.RowVersion), body.GetProperty("version").GetString());
        Assert.False(body.GetProperty("read").GetBoolean());
        using var page = await owner.GetAsync("/api/v1/notifications?limit=100");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains((await page.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
            n => n.GetProperty("id").GetGuid() == id);

        foreach (var key in new[] { "supervisor", "pm-b", "reporter-a", "operator-a", "crew-a" })
        {
            using var other = await context.LoginAsync(key);
            using var deniedGet = await other.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, deniedGet.StatusCode);
            using var deniedList = await other.GetAsync("/api/v1/notifications?limit=100");
            Assert.Equal(HttpStatusCode.OK, deniedList.StatusCode);
            Assert.DoesNotContain((await deniedList.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
                n => n.GetProperty("id").GetGuid() == id);
            using var deniedRead = await MarkRead(other, url, Guid.NewGuid().ToString("N"), etag);
            Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
        }
        foreach (var query in new[] { "limit=0", "limit=101", "cursor=invalid" })
        {
            using var invalid = await owner.GetAsync("/api/v1/notifications?" + query);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var missing = await owner.PostAsync(url + "/read", null);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        var before = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.Null(before.ReadAt);
        Assert.Equal(original.RowVersion, before.RowVersion);
        var receiptsBefore = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient.Id && r.Operation == "NotificationRead");
        var requestKey = Guid.NewGuid().ToString("N");
        using var read = await MarkRead(owner, url, requestKey, etag);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var committed = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.NotNull(committed.ReadAt);
        Assert.NotEqual(original.RowVersion, committed.RowVersion);
        using var replay = await MarkRead(owner, url, requestKey, etag);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(read.Headers.ETag, replay.Headers.ETag);
        Assert.Equal(receiptsBefore + 1, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient.Id && r.Operation == "NotificationRead"));
        using var stale = await MarkRead(owner, url, Guid.NewGuid().ToString("N"), etag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using var changed = await MarkRead(owner, url, requestKey, read.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var final = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.Equal(committed.ReadAt, final.ReadAt);
        Assert.Equal(committed.RowVersion, final.RowVersion);
    }

    private static Task<HttpResponseMessage> MarkRead(HttpClient client, string url, string key, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url + "/read");
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", version);
        return SendAndDispose(client, request);
    }

    private static async Task<HttpResponseMessage> SendAndDispose(HttpClient client, HttpRequestMessage request)
    {
        using (request) return await client.SendAsync(request);
    }
}
