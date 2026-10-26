using System.Net;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

[Collection(SandboxMethodOwnedSqlGroup.Name)]
public sealed class SandboxNotificationConcurrencyTests
{
    [Fact]
    public async Task TwoCurrentVersionWritersCommitOneReadAndRejectTheStaleWriter()
    {
        await using var context = new SandboxScenarioContext();
        await context.InitializeCoreAsync();
        context.Fixture.GetOwnedSeedDatabaseIdentity();
        var id = await AddInbox(context);
        using var first = await context.LoginAsync("pm-a");
        using var second = await context.LoginAsync("pm-a");
        using var read = await first.GetAsync($"/api/v1/notifications/{id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var etag = read.Headers.ETag!.ToString();
        var responses = await Task.WhenAll(Mark(first, id, Guid.NewGuid().ToString("N"), etag),
            Mark(second, id, Guid.NewGuid().ToString("N"), etag));
        try
        {
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.PreconditionFailed);
            await using var db = context.Fixture.CreateDbContext();
            var row = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
            Assert.NotNull(row.ReadAt);
            Assert.NotEqual(etag, $"\"{Convert.ToBase64String(row.RowVersion)}\"");
            // The rejected command may retain an idempotency receipt; it must not read twice.
            var recipient = context.Catalog.Actors.Single(a => a.Key == "pm-a").Id;
            Assert.Equal(2, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient && r.Operation == "NotificationRead"));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task SuspendedRecipientCannotReplayCommittedReadWithExistingBearer()
    {
        await using var context = new SandboxScenarioContext();
        await context.InitializeCoreAsync();
        context.Fixture.GetOwnedSeedDatabaseIdentity();
        var id = await AddInbox(context);
        using var client = await context.LoginAsync("pm-a");
        using var read = await client.GetAsync($"/api/v1/notifications/{id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var etag = read.Headers.ETag!.ToString(); var key = Guid.NewGuid().ToString("N");
        using var committed = await Mark(client, id, key, etag);
        Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
        await using var db = context.Fixture.CreateDbContext();
        var original = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        var recipient = context.Catalog.Actors.Single(a => a.Key == "pm-a").Id;
        // Controlled current-account revocation, not an account-management command assertion.
        var user = await db.Users.SingleAsync(u => u.Id == recipient);
        user.Status = UserStatus.Suspended; await db.SaveChangesAsync();
        var receiptCount = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient && r.Operation == "NotificationRead");
        using var replay = await Mark(client, id, key, etag);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var deniedRead = await client.GetAsync($"/api/v1/notifications/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, deniedRead.StatusCode);
        var final = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.Equal(original.RowVersion, final.RowVersion); Assert.Equal(original.ReadAt, final.ReadAt);
        Assert.Equal(receiptCount, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient && r.Operation == "NotificationRead"));
    }

    [Fact]
    public async Task EndedProjectMembershipCannotReplayReadOrExposeScopeWithExistingBearer()
    {
        await using var context = new SandboxScenarioContext();
        await context.InitializeCoreAsync();
        context.Fixture.GetOwnedSeedDatabaseIdentity();
        var id = await AddInbox(context);
        using var client = await context.LoginAsync("pm-a");
        using var read = await client.GetAsync($"/api/v1/notifications/{id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var etag = read.Headers.ETag!.ToString(); var key = Guid.NewGuid().ToString("N");
        using var committed = await Mark(client, id, key, etag);
        Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
        await using var db = context.Fixture.CreateDbContext();
        var recipient = context.Catalog.Actors.Single(a => a.Key == "pm-a").Id;
        var original = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        var receipts = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient && r.Operation == "NotificationRead");
        var membership = await db.ProjectMembers.SingleAsync(m => m.UserId == recipient && m.ProjectId == RoadGuardSystem.Repositories.Seeding.Sandbox.SandboxFixtureIds.ProjectA);
        membership.Status = RoadGuardSystem.aBusinessObjects.Commons.ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        using var replay = await Mark(client, id, key, etag);
        using var fresh = await Mark(client, id, Guid.NewGuid().ToString("N"), committed.Headers.ETag!.ToString());
        using var deniedRead = await client.GetAsync($"/api/v1/notifications/{id}");
        using var deniedScope = await client.GetAsync($"/api/v1/notifications/{id}/scope");
        Assert.Equal(HttpStatusCode.NotFound, replay.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fresh.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deniedScope.StatusCode);
        var final = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.Equal(original.RowVersion, final.RowVersion); Assert.Equal(original.ReadAt, final.ReadAt);
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient && r.Operation == "NotificationRead"));
    }

    private static async Task<Guid> AddInbox(SandboxScenarioContext context)
    {
        await using var db = context.Fixture.CreateDbContext(); var id = Guid.NewGuid();
        var recipient = context.Catalog.Actors.Single(a => a.Key == "pm-a").Id;
        // Synthetic inbox fixture; no production producer or external delivery claim.
        db.Notifications.Add(Notification.Create(id, recipient, "Project",
            RoadGuardSystem.Repositories.Seeding.Sandbox.SandboxFixtureIds.ProjectA,
            "SyntheticConcurrency", "Synthetic", "Synthetic private notification", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(); return id;
    }

    private static async Task<HttpResponseMessage> Mark(HttpClient client, Guid id, string key, string etag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{id}/read");
        request.Headers.Add("Idempotency-Key", key); request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }
}
