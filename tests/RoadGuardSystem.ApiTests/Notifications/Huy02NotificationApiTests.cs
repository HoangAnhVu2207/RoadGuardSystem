using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy02NotificationApiTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task ExistingHeadersErrorsExactBodyAndEtagReplayRemainCompatible()
    {
        var (actor, notification) = await SeedAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        await LoginAsync(client, actor);
        var detail = await client.GetAsync($"/api/v1/notifications/{notification.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var etag = detail.Headers.ETag!.ToString();
        var missing = await client.PostAsync($"/api/v1/notifications/{notification.Id}/read", null);
        await AssertProblemAsync(missing, (HttpStatusCode)428, "validation_error");
        await AssertProblemAsync(await ReadAsync(client, notification.Id, "stale", '"' + Convert.ToBase64String(new byte[8]) + '"'),
            HttpStatusCode.PreconditionFailed, "notification_concurrency_conflict");
        var first = await ReadAsync(client, notification.Id, "read-once", etag);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await first.Content.ReadAsStringAsync();
        var replay = await ReadAsync(client, notification.Id, "read-once", etag);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(body, await replay.Content.ReadAsStringAsync());
        Assert.Equal(first.Headers.ETag, replay.Headers.ETag);
        await AssertProblemAsync(await ReadAsync(client, notification.Id, "read-once", first.Headers.ETag!.ToString()),
            HttpStatusCode.Conflict, "duplicate_request");
        await AssertProblemAsync(await ReadAsync(client, Guid.NewGuid(), "missing", etag),
            HttpStatusCode.NotFound, "notification_not_found");
        var (other, _) = await SeedAsync();
        using var outsider = factory.CreateClient();
        await LoginAsync(outsider, other);
        await AssertProblemAsync(await ReadAsync(outsider, notification.Id, "other", etag),
            HttpStatusCode.NotFound, "notification_not_found");
        await AssertProblemAsync(await client.GetAsync("/api/v1/notifications?limit=101"), HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await client.GetAsync("/api/v1/notifications?cursor=invalid"), HttpStatusCode.BadRequest, "validation_error");
    }

    [Theory]
    [InlineData("user", false)]
    [InlineData("user", true)]
    [InlineData("role", false)]
    [InlineData("role", true)]
    [InlineData("must-change", false)]
    [InlineData("must-change", true)]
    [InlineData("snapshot", false)]
    [InlineData("snapshot", true)]
    public async Task AuthorityLossAfterRealAuthenticationDeniesHandlerAndReplay(string loss, bool replay)
    {
        var (actor, notification) = await SeedAsync();
        var armed = false;
        async Task RevokeAsync()
        {
            if (!armed) return;
            await using var db = fixture.CreateDbContext();
            if (loss == "user") (await db.Users.SingleAsync(x => x.Id == actor.Id)).Status = UserStatus.Suspended;
            if (loss == "must-change") (await db.Users.SingleAsync(x => x.Id == actor.Id)).MustChangePassword = true;
            if (loss == "role") (await db.Roles.SingleAsync(x => x.Code == actor.RoleCode)).IsActive = false;
            if (loss == "snapshot")
                await db.Users.Where(x => x.Id == actor.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.RoleCode, UserRoleCode.RepairCrew));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<INotificationService>();
                services.AddScoped<INotificationService>(sp => new AfterAuthenticationService(
                    new NotificationService(sp.GetRequiredService<INotificationRepository>()), RevokeAsync));
            });
        using var client = factory.CreateClient();
        await LoginAsync(client, actor);
        var etag = '"' + Convert.ToBase64String(notification.RowVersion) + '"';
        if (replay) Assert.Equal(HttpStatusCode.OK, (await ReadAsync(client, notification.Id, "authority", etag)).StatusCode);
        armed = true;
        try
        {
            await AssertProblemAsync(await ReadAsync(client, notification.Id, "authority", etag), HttpStatusCode.Unauthorized, "auth_unauthorized");
            await using var verify = fixture.CreateDbContext();
            Assert.Equal(replay ? 1 : 0, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor.Id));
            Assert.Equal(replay, (await verify.Notifications.SingleAsync(x => x.Id == notification.Id)).ReadAt is not null);
            Assert.Equal(1, await verify.Notifications.CountAsync(x => x.RecipientUserId == actor.Id));
            Assert.Empty(await verify.OutboxMessages.Where(x => x.CorrelationId == notification.Id).ToArrayAsync());
        }
        finally
        {
            if (loss == "role")
            {
                await using var db = fixture.CreateDbContext();
                (await db.Roles.SingleAsync(x => x.Code == actor.RoleCode)).IsActive = true;
                await db.SaveChangesAsync();
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SessionRevocationOrExpiryRejectsCommittedReplayAtRequestBoundary(bool expire)
    {
        var (actor, notification) = await SeedAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        await LoginAsync(client, actor);
        var etag = '"' + Convert.ToBase64String(notification.RowVersion) + '"';
        Assert.Equal(HttpStatusCode.OK, (await ReadAsync(client, notification.Id, "session", etag)).StatusCode);
        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.SingleAsync(x => x.UserId == actor.Id);
            if (expire)
            {
                session.ExpiresAt = session.IssuedAt.AddTicks(1);
            }
            else session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        var denied = await ReadAsync(client, notification.Id, "session", etag);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Null(denied.Headers.ETag);
        Assert.Equal("application/problem+json", denied.Content.Headers.ContentType?.MediaType);
        var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("auth_session_revoked", problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("correlationId", out _));
        Assert.False(problem.TryGetProperty("message", out _));
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor.Id));
    }

    private async Task<(RoadGuardSystem.BusinessObjects.Identity.ApplicationUser Actor, Notification Notification)> SeedAsync()
    {
        var actor = await fixture.CreateUserAsync($"h02_inbox_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"H02-{Guid.NewGuid():N}",
            Name = "Inbox source",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var notification = Notification.Create(Guid.NewGuid(), actor.Id, "Project", project.Id, "created",
            "Test", "Inbox content", DateTimeOffset.UtcNow);
        await using var db = fixture.CreateDbContext();
        db.Projects.Add(project);
        db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return (actor, notification);
    }

    private static async Task LoginAsync(HttpClient client, RoadGuardSystem.BusinessObjects.Identity.ApplicationUser actor)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }

    private static async Task<HttpResponseMessage> ReadAsync(HttpClient client, Guid id, string key, string etag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{id}/read");
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Null(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.True(body.TryGetProperty("correlationId", out _));
        Assert.False(body.TryGetProperty("message", out _));
    }

    // A module-local test seam runs after real JWT validation and before the real
    // service/repository call; production shared auth/fixtures remain untouched.
    private sealed class AfterAuthenticationService(NotificationService inner, Func<Task> revoke) : INotificationService
    {
        public Task<NotificationReadServiceResult> GetAsync(Guid actorUserId, Guid notificationId, CancellationToken cancellationToken = default)
            => inner.GetAsync(actorUserId, notificationId, cancellationToken);
        public Task<NotificationPageServiceResult> ListAsync(Guid actorUserId, string? cursor, int? limit, CancellationToken cancellationToken = default)
            => inner.ListAsync(actorUserId, cursor, limit, cancellationToken);
        public Task<NotificationReadServiceResult> MarkReadAsync(Guid actorUserId, Guid notificationId, string idempotencyKey,
            string expectedVersion, CancellationToken cancellationToken = default)
            => MarkReadAsync(actorUserId, notificationId, idempotencyKey, expectedVersion, null, cancellationToken);
        public async Task<NotificationReadServiceResult> MarkReadAsync(Guid actorUserId, Guid notificationId, string idempotencyKey,
            string expectedVersion, UserRoleCode? authenticatedRole, CancellationToken cancellationToken = default)
        {
            await revoke();
            return await inner.MarkReadAsync(actorUserId, notificationId, idempotencyKey, expectedVersion, authenticatedRole, cancellationToken);
        }
    }
}
