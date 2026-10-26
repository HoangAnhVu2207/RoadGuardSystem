using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

[Trait("TaskId", "P2-001/P2-002/P2-062")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P2NotificationApiTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P2NotificationApiTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task NotificationEndpoints_EnforceOwnerAndReplayRead()
    {
        var owner = await _sql.CreateUserAsync($"p2_notification_owner_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var other = await _sql.CreateUserAsync($"p2_notification_other_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P2-INBOX-{Guid.NewGuid():N}",
            Name = "Inbox source",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var notification = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id, "ASSIGNED", "Survey assigned", "A survey task was assigned.", DateTimeOffset.UtcNow);
        await using (var context = _sql.CreateDbContext())
        {
            context.Projects.Add(project);
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = owner.Id,
                RoleCode = UserRoleCode.DroneOperator,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var ownerClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(ownerClient, owner.UserName!);
        await AuthenticateAsync(otherClient, other.UserName!);

        var list = await ownerClient.GetAsync("/api/v1/notifications");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Contain(notification.Id);

        var get = await ownerClient.GetAsync($"/api/v1/notifications/{notification.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString();
        get.Headers.ETag!.Tag.Should().Be($"\"{version}\"");

        var hidden = await otherClient.GetAsync($"/api/v1/notifications/{notification.Id}");
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string key = "p2-notification-read-001";
        var read = await MarkReadAsync(ownerClient, notification.Id, key, version!);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var replay = await MarkReadAsync(ownerClient, notification.Id, key, version!);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var readBody = await read.Content.ReadFromJsonAsync<JsonElement>();
        var nextVersion = readBody.GetProperty("version").GetString()!;
        var secondRead = await MarkReadAsync(ownerClient, notification.Id, "p2-notification-read-002", nextVersion);
        secondRead.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verify = _sql.CreateDbContext();
        (await verify.Notifications.FindAsync(notification.Id))!.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task InboxHttpValidatesPagingPreconditionsAndCurrentOwnerWithoutLeakingPrivatePayload()
    {
        _sql.GetOwnedSeedDatabaseIdentity();
        var owner = await _sql.CreateUserAsync("contract-owner-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var other = await _sql.CreateUserAsync("contract-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Private inbox", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = owner.Id,
            RoleCode = UserRoleCode.DroneOperator,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var notification = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id, "SYNTHETIC", "private-title", "private-body", now);
        await using (var db = _sql.CreateDbContext()) { db.AddRange(project, member, notification); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(); using var outsider = factory.CreateClient(); using var anonymous = factory.CreateClient();
        await AuthenticateAsync(client, owner.UserName!); await AuthenticateAsync(outsider, other.UserName!);
        var path = $"/api/v1/notifications/{notification.Id}";
        foreach (var endpoint in new[] { "/api/v1/notifications", path })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MarkReadAsync(anonymous, notification.Id, "anonymous", "invalid")).StatusCode);
        foreach (var suffix in new[] { "?limit=0", "?limit=101", "?cursor=not-base64" })
        {
            var invalid = await client.GetAsync("/api/v1/notifications" + suffix);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal("validation_error", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        var detail = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var etag = detail.Headers.ETag!.ToString();
        var version = (await detail.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString();
        Assert.Equal("\"" + version + "\"", etag);
        var page = await (await client.GetAsync("/api/v1/notifications?limit=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(notification.Id, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsync(path + "/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await MarkReadAsync(client, notification.Id, "stale", Convert.ToBase64String(new byte[8]))).StatusCode);
        var hidden = await outsider.GetAsync(path); Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.DoesNotContain("private-body", await hidden.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await MarkReadAsync(outsider, notification.Id, "wrong-owner", etag)).StatusCode);
        var outsidersPage = await (await outsider.GetAsync("/api/v1/notifications")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(outsidersPage.GetProperty("items").EnumerateArray());
        await using (var db = _sql.CreateDbContext())
        {
            var before = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
            Assert.Null(before.ReadAt); Assert.Equal(etag, "\"" + Convert.ToBase64String(before.RowVersion) + "\"");
        }
        var key = Guid.NewGuid().ToString("N");
        var committed = await MarkReadAsync(client, notification.Id, key, etag); Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
        var committedBody = await committed.Content.ReadAsStringAsync();
        var replay = await MarkReadAsync(client, notification.Id, key, etag); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(committedBody, await replay.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await MarkReadAsync(client, notification.Id, "late-writer", etag)).StatusCode);
        await using (var db = _sql.CreateDbContext())
        {
            var row = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
            Assert.NotNull(row.ReadAt); var stamp = row.RowVersion; var readAt = row.ReadAt;
            await db.ProjectMembers.Where(m => m.Id == member.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await MarkReadAsync(client, notification.Id, key, etag)).StatusCode);
            var final = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
            Assert.Equal(stamp, final.RowVersion); Assert.Equal(readAt, final.ReadAt);
            Assert.Equal("private-title", final.Title); Assert.Equal("private-body", final.Body);
            var revokedPage = await (await client.GetAsync("/api/v1/notifications")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(revokedPage.GetProperty("items").EnumerateArray());
        }
        // Synthetic inbox admission proves consumer contract only, never Fast Track producer or email delivery.
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager, true, false)]
    [InlineData(UserRoleCode.Supervisor, true, true)]
    [InlineData(UserRoleCode.DroneOperator, true, false)]
    [InlineData(UserRoleCode.Reporter, false, false)]
    [InlineData(UserRoleCode.RepairCrew, false, false)]
    public async Task ProjectInboxRoleAndProjectMatrixPreservesRecipientAndReadState(UserRoleCode role, bool visibleA, bool visibleB)
    {
        _sql.GetOwnedSeedDatabaseIdentity();
        var owner = await _sql.CreateUserAsync("matrix-owner-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await _sql.CreateUserAsync("matrix-other-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var now = DateTimeOffset.UtcNow;
        var a = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "TEST_ONLY A", null, null, null, null, now);
        var b = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "TEST_ONLY B", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = a.Id,
            UserId = owner.Id,
            RoleCode = role,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var first = Notification.Create(Guid.NewGuid(), owner.Id, "Project", a.Id, "SYNTHETIC", "private-A", "private-A", now);
        var second = Notification.Create(Guid.NewGuid(), owner.Id, "Project", a.Id, "SYNTHETIC_SECOND", "private-A2", "private-A2", now.AddSeconds(-1));
        var cross = Notification.Create(Guid.NewGuid(), owner.Id, "Project", b.Id, "SYNTHETIC", "private-B", "private-B", now.AddSeconds(-2));
        var foreign = Notification.Create(Guid.NewGuid(), other.Id, "Project", a.Id, "SYNTHETIC", "foreign", "foreign", now);
        await using (var db = _sql.CreateDbContext()) { db.AddRange(a, b, member, first, second, cross, foreign); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(); await AuthenticateAsync(client, owner.UserName!);
        byte[] version;
        await using (var db = _sql.CreateDbContext()) version = (await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id)).RowVersion;
        var detail = await client.GetAsync($"/api/v1/notifications/{first.Id}");
        Assert.Equal(visibleA ? HttpStatusCode.OK : HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(visibleB ? HttpStatusCode.OK : HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{cross.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{foreign.Id}")).StatusCode);
        var ids = new List<Guid>(); string? cursor = null;
        do
        {
            var page = await (await client.GetAsync("/api/v1/notifications?limit=1" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)))).Content.ReadFromJsonAsync<JsonElement>();
            ids.AddRange(page.GetProperty("items").EnumerateArray().Select(n => n.GetProperty("id").GetGuid()));
            cursor = page.TryGetProperty("nextCursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
            Assert.True(ids.Count <= 3, "Cursor must advance without duplicate rows.");
        } while (cursor is not null);
        Assert.Equal(visibleA ? (visibleB ? 3 : 2) : 0, ids.Count); Assert.Equal(ids.Count, ids.Distinct().Count()); Assert.DoesNotContain(foreign.Id, ids);
        await using (var db = _sql.CreateDbContext())
        {
            var row = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id);
            Assert.Equal(version, row.RowVersion); Assert.Null(row.ReadAt);
        }
        var key = Guid.NewGuid().ToString("N");
        var mark = await MarkReadAsync(client, first.Id, key, Convert.ToBase64String(version));
        Assert.Equal(visibleA ? HttpStatusCode.OK : HttpStatusCode.NotFound, mark.StatusCode);
        if (visibleA)
        {
            Assert.Equal(HttpStatusCode.OK, (await MarkReadAsync(client, first.Id, key, Convert.ToBase64String(version))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await MarkReadAsync(client, second.Id, key, Convert.ToBase64String(version))).StatusCode);
        }
        await using (var db = _sql.CreateDbContext())
        {
            var row = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id);
            Assert.Equal(visibleA, row.ReadAt.HasValue);
            Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == second.Id)).ReadAt);
            Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == foreign.Id)).ReadAt);
            var stamp = row.RowVersion;
            await db.ProjectMembers.Where(m => m.Id == member.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
            // Supervisor's global source scope is intentional current behavior, not a project-member restriction.
            var remains = role == UserRoleCode.Supervisor;
            Assert.Equal(remains ? HttpStatusCode.OK : HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{first.Id}")).StatusCode);
            Assert.Equal(remains ? HttpStatusCode.OK : HttpStatusCode.NotFound, (await MarkReadAsync(client, first.Id, key, Convert.ToBase64String(version))).StatusCode);
            Assert.Equal(stamp, (await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id)).RowVersion);
        }
    }

    [Fact]
    public async Task ConcurrentReadUsesOneVersionAndOneReadEffectWithoutReplayingAfterSuspension()
    {
        _sql.GetOwnedSeedDatabaseIdentity();
        var owner = await _sql.CreateUserAsync("concurrent-inbox-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "TEST_ONLY concurrent inbox", null, null, null, null, now);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, owner.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1));
        var item = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id, "TEST_ONLY", "private", "private", now);
        await using (var db = _sql.CreateDbContext()) { db.AddRange(project, member, item); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(); await AuthenticateAsync(client, owner.UserName!);
        var path = $"/api/v1/notifications/{item.Id}";
        var get = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var dto = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(item.Id, dto.GetProperty("id").GetGuid()); Assert.Equal("private", dto.GetProperty("message").GetString());
        Assert.Equal(project.Id, dto.GetProperty("resourceId").GetGuid()); Assert.False(dto.GetProperty("read").GetBoolean());
        Assert.Equal(now, dto.GetProperty("occurredAt").GetDateTimeOffset());
        var version = dto.GetProperty("version").GetString()!;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/notifications/not-a-guid")).StatusCode);
        // Nonmatching opaque If-Match is stale (412), including non-Base64 text, per current service contract.
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await MarkReadAsync(client, item.Id, "bad-version", "not-base64")).StatusCode);
        var keys = new[] { "first-" + Guid.NewGuid().ToString("N"), "second-" + Guid.NewGuid().ToString("N") };
        // Two HTTP commands intentionally contend on one disposable row; SQL fixture startup remains sequential.
        var results = await Task.WhenAll(keys.Select(key => MarkReadAsync(client, item.Id, key, version)));
        Assert.Single(results.Where(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Single(results.Where(r => r.StatusCode == HttpStatusCode.PreconditionFailed));
        var winner = Array.FindIndex(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await MarkReadAsync(client, item.Id, keys[winner], version)).StatusCode);
        await using var verify = _sql.CreateDbContext();
        var row = await verify.Notifications.AsNoTracking().SingleAsync(n => n.Id == item.Id);
        Assert.NotNull(row.ReadAt); Assert.NotEqual(version, Convert.ToBase64String(row.RowVersion));
        Assert.Equal(3, await verify.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id));
        var receipts = await verify.IdempotencyRecords.Where(r => r.ActorUserId == owner.Id).Select(r => r.OutcomeJson).ToArrayAsync();
        Assert.Single(receipts.Where(json => JsonDocument.Parse(json).RootElement.GetProperty("Notification").ValueKind != JsonValueKind.Null));
        await verify.Users.Where(u => u.Id == owner.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MarkReadAsync(client, item.Id, keys[winner], version)).StatusCode);
        var final = await verify.Notifications.AsNoTracking().SingleAsync(n => n.Id == item.Id);
        Assert.Equal(row.RowVersion, final.RowVersion); Assert.Equal(row.ReadAt, final.ReadAt);
        Assert.Equal(3, await verify.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id));
    }

    [Fact]
    public async Task ReceiptSaveFailureRollsBackReadAndAllowsSameCommandToRecoverOnce()
    {
        _sql.GetOwnedSeedDatabaseIdentity();
        var owner = await _sql.CreateUserAsync("rollback-inbox-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "TEST_ONLY rollback", null, null, null, null, now);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, owner.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1));
        var item = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id, "TEST_ONLY", "private", "private", now);
        await using (var db = _sql.CreateDbContext()) { db.AddRange(project, member, item); await db.SaveChangesAsync(); }
        var probe = new ReceiptSaveFailure(owner.Id, item.Id);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString, configureTestServices: services =>
            services.AddScoped(sp => new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>(
                sp.GetRequiredService<DbContextOptions<RoadGuardDbContext>>()).AddInterceptors(probe).Options)));
        using var client = factory.CreateClient(); await AuthenticateAsync(client, owner.UserName!);
        var get = await client.GetAsync($"/api/v1/notifications/{item.Id}"); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var version = (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;
        var key = Guid.NewGuid().ToString("N"); probe.Armed = true;
        var failed = await MarkReadAsync(client, item.Id, key, version);
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode); Assert.Equal(1, probe.Calls);
        Assert.True(probe.ReadSavedInsideTransaction, "Failure must follow the persisted read mutation, inside its transaction.");
        await using (var db = _sql.CreateDbContext())
        {
            var row = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == item.Id);
            Assert.Null(row.ReadAt); Assert.Equal(version, Convert.ToBase64String(row.RowVersion));
            Assert.Equal(0, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id));
        }
        var recovered = await MarkReadAsync(client, item.Id, key, version); Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        var body = await recovered.Content.ReadAsStringAsync();
        Assert.Equal(body, await (await MarkReadAsync(client, item.Id, key, version)).Content.ReadAsStringAsync());
        await using (var db = _sql.CreateDbContext())
        {
            Assert.NotNull((await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == item.Id)).ReadAt);
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id));
        }
    }

    // Failure injection is local to this HTTP host and receipt actor; no production hooks or shared SQL.
    private sealed class ReceiptSaveFailure(Guid actor, Guid item) : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public int Calls { get; private set; }
        public bool ReadSavedInsideTransaction { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            if (Armed && db.ChangeTracker.Entries<IdempotencyRecord>().Any(e => e.State == EntityState.Added && e.Entity.ActorUserId == actor))
            {
                Armed = false; Calls++;
                ReadSavedInsideTransaction = db.Database.CurrentTransaction is not null && db.ChangeTracker.Entries<Notification>()
                    .Any(e => e.Entity.Id == item && e.State == EntityState.Unchanged && e.Entity.ReadAt.HasValue);
                throw new InvalidOperationException("TEST_ONLY receipt persistence failure");
            }
            return ValueTask.FromResult(result);
        }
    }

    private static async Task<HttpResponseMessage> MarkReadAsync(HttpClient client, Guid notificationId, string key, string version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{notificationId}/read");
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
}
