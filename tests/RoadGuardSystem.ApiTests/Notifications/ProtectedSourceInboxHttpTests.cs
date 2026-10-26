using RoadGuardSystem.Repositories.Repairs;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.ApiTests.Infrastructure;

using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;

namespace RoadGuardSystem.ApiTests.Notifications;

public sealed class ProtectedSourceInboxHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("REPAIR")]
    [InlineData("FIELD")]
    [InlineData("PASSWORD")]
    public async Task PersistedProtectedSourceUsesCurrentAuthorityForListGetReadAndReplay(string kind)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        Guid actor, notificationId, assignment = Guid.Empty;
        await using (var db = sql.CreateDbContext())
        {
            if (kind == "PASSWORD")
            {
                var user = await sql.CreateUserAsync("recovery-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
                actor = user.Id;
                var request = new PasswordRecoveryRequest { Id = Guid.NewGuid(), TargetUserId = actor, RequestedAtUtc = DateTimeOffset.UtcNow };
                var row = Notification.Create(Guid.NewGuid(), actor, "PasswordRecoveryRequest", request.Id, "password_recovery_requested", "Password recovery requested", "An active account requires password recovery review.", DateTimeOffset.UtcNow);
                db.AddRange(request, row); await db.SaveChangesAsync(); notificationId = row.Id;
            }
            else
            {
                Guid eventId, sourceId;
                if (kind == "REPAIR")
                {
                    var source = await Seed(true); actor = source.Crew; assignment = source.Assignment!.Value; eventId = source.Event; sourceId = source.Item;
                }
                else
                {
                    var source = await GenuineProtectedSource.Seed(db, sql); actor = source.Crew;
                    var defectVersion = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
                    var result = await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(
                        new(source.Project, null, "create", new FieldTaskCreateInputFact(source.Defect, defectVersion, null, "REPORTER", source.Route, source.Set, null, null, "PRE_MEASUREMENT", 1, "{}", "actual FIELD assignment", source.Crew, DateTimeOffset.UtcNow.AddDays(1)),
                            Guid.NewGuid().ToString("N"), null, new(source.Pm, UserRoleCode.ProjectManager, source.Pm, "DIRECT", true)), _ => Task.FromResult(true), default);
                    Assert.Equal(201, result.Status);
                    var fieldEvent = await db.FieldInspectionTaskEvents.SingleAsync(x => x.ProjectId == source.Project && x.Kind == "ASSIGNED"); eventId = fieldEvent.Id; sourceId = fieldEvent.TaskId;
                    assignment = (await db.FieldInspectionAssignments.SingleAsync(x => x.FieldInspectionTaskId == sourceId)).Id;
                }
                var claim = await Lease(db, eventId);
                var dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
                Assert.Equal("COMMITTED", dispatched.Status); Assert.Equal(1, dispatched.Delivered);
                notificationId = (await db.Notifications.SingleAsync(x => x.SourceEntityId == sourceId)).Id;
            }
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(); using var wrong = factory.CreateClient(); using var anonymous = factory.CreateClient();
        await using var query = sql.CreateDbContext();
        var email = (await query.Users.AsNoTracking().SingleAsync(x => x.Id == actor)).Email!;
        var other = await sql.CreateUserAsync("other-source-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await Login(client, email); await Login(wrong, other.Email!);
        var path = $"/api/v1/notifications/{notificationId}";
        var before = await query.Notifications.AsNoTracking().SingleAsync(x => x.Id == notificationId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/notifications")).StatusCode);
        var get = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var dto = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(notificationId, dto.GetProperty("id").GetGuid()); Assert.False(dto.GetProperty("read").GetBoolean());
        Assert.Equal(before.Body, dto.GetProperty("message").GetString());
        Assert.Equal(before.SourceEntityId, dto.GetProperty("resourceId").GetGuid());
        Assert.Equal(before.OccurredAtUtc, dto.GetProperty("occurredAt").GetDateTimeOffset());
        Assert.Equal(6, dto.EnumerateObject().Count()); // Finite public projection; no recipient, storage URL/key or private report metadata.
        var auditCount = await query.Set<H6NotificationAuditRow>().CountAsync(x => x.NotificationId == notificationId);
        Assert.True(auditCount > 0);
        var scope = await query.Set<H6NotificationScopeRow>().AsNoTracking().SingleAsync(x => x.NotificationId == notificationId);
        Assert.NotEqual("UNKNOWN_PROTECTED", scope.Classification);
        var version = dto.GetProperty("version").GetString()!; Assert.Equal("\"" + version + "\"", get.Headers.ETag!.Tag);
        Assert.Contains(notificationId.ToString(), await (await client.GetAsync("/api/v1/notifications?limit=1")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(notificationId.ToString(), await (await wrong.GetAsync("/api/v1/notifications")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await wrong.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(wrong, notificationId, Guid.NewGuid().ToString("N"), version)).StatusCode);
        Assert.Equal(before.RowVersion, (await query.Notifications.AsNoTracking().SingleAsync(x => x.Id == notificationId)).RowVersion);
        var key = Guid.NewGuid().ToString("N"); var read = await Read(client, notificationId, key, version);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(await read.Content.ReadAsStringAsync(), await (await Read(client, notificationId, key, version)).Content.ReadAsStringAsync());
        var marked = await query.Notifications.AsNoTracking().SingleAsync(x => x.Id == notificationId); Assert.NotNull(marked.ReadAt);
        var receipts = await query.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor);
        if (kind == "PASSWORD")
            await query.Users.Where(x => x.Id == actor).ExecuteUpdateAsync(u => u.SetProperty(x => x.RoleCode, UserRoleCode.ProjectManager));
        else
        {
            var binding = await query.FieldInspectionAssignments.SingleAsync(x => x.Id == assignment);
            binding.End(DateTimeOffset.UtcNow, "controlled current-source authority revocation"); await query.SaveChangesAsync();
        }
        if (kind == "PASSWORD")
        {
            // Role changes invalidate the old token before repository scope evaluation.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
            await Login(client, email);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.DoesNotContain(notificationId.ToString(), await (await client.GetAsync("/api/v1/notifications?limit=1")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(client, notificationId, key, version)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(client, notificationId, Guid.NewGuid().ToString("N"), version)).StatusCode);
        var final = await query.Notifications.AsNoTracking().SingleAsync(x => x.Id == notificationId);
        Assert.Equal(marked.RowVersion, final.RowVersion); Assert.Equal(marked.ReadAt, final.ReadAt);
        Assert.Equal(receipts, await query.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
        Assert.Equal(auditCount, await query.Set<H6NotificationAuditRow>().CountAsync(x => x.NotificationId == notificationId));
    }
    [Fact]
    public async Task UnknownProtectedSourceIsHiddenAndAuditedOnceThroughActualHttp()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var actor = await sql.CreateUserAsync("unknown-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var row = Notification.Create(Guid.NewGuid(), actor.Id, "Unregistered", Guid.NewGuid(), "created", "Legacy", "PRIVATE synthetic unknown payload", DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext(); db.Add(row); await db.SaveChangesAsync(); var version = Convert.ToBase64String(row.RowVersion);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString); using var client = factory.CreateClient(); await Login(client, actor.Email!);
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{row.Id}")).StatusCode);
            var list = await client.GetAsync("/api/v1/notifications"); Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.DoesNotContain(row.Id.ToString(), await list.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(row.Body, await list.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, (await Read(client, row.Id, Guid.NewGuid().ToString("N"), version)).StatusCode);
        }
        var audit = Assert.Single(await db.Set<H6NotificationAuditRow>().Where(x => x.NotificationId == row.Id).AsNoTracking().ToListAsync());
        Assert.Equal("UNKNOWN_PROTECTED", audit.Classification); Assert.Equal("notification_legacy_scope_unknown", audit.ReasonCode);
        var final = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == row.Id); Assert.Null(final.ReadAt); Assert.Equal(version, Convert.ToBase64String(final.RowVersion));
        Assert.Empty(await db.IdempotencyRecords.Where(x => x.ActorUserId == actor.Id).ToListAsync());
    }
    private static async Task Login(HttpClient client, string email)
    {
        var result = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Read(HttpClient client, Guid id, string key, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{id}/read"); request.Headers.Add("Idempotency-Key", key); request.Headers.Add("If-Match", "\"" + version + "\""); return client.SendAsync(request);
    }
    private sealed record Source(Guid Item, Guid Event, Guid Crew, Guid Supervisor, Guid? Assignment);
    private async Task<Source> Seed(bool assigned)
    {
        await using var db = sql.CreateDbContext(); var actual = await GenuineProtectedSource.Seed(db, sql);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var repository = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await repository.CreatePackageAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project,
            new(actual.Defect, defectVersion, [new("FORMAL_REPAIR", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "actual repair")], "actual package"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status); var packageView = Assert.IsType<RepairPackageFact>(created.Value);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageView.Id);
        var proposed = await repository.ProposeItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id,
            new(package.Obligations[0].Id, "NORMAL", "actual proposal", "checklist-v1", "actual proposal"), Guid.NewGuid().ToString(), packageView.Version), default);
        Assert.Equal(201, proposed.Status); var itemView = Assert.IsType<RepairItemFact>(proposed.Value);
        if (!assigned)
        {
            var proposal = await db.Set<RepairItemLifecycleEvent>().SingleAsync(row => row.ItemId == itemView.Id && row.Kind == "PROPOSED");
            return new(itemView.Id, proposal.Id, actual.Crew, actual.Supervisor, null);
        }
        var approved = await repository.ApproveItemAsync(new(actual.Supervisor, UserRoleCode.Supervisor, actual.Project, package.Id,
            itemView.Id, new("actual approval"), Guid.NewGuid().ToString(), itemView.Version), default);
        Assert.Equal(201, approved.Status); var approvalView = Assert.IsType<RepairItemFact>(approved.Value);
        var currentDefectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var task = new RepairFieldTaskData(actual.Defect, currentDefectVersion, null, "REPORTER", actual.Route, actual.Set, null, null,
            "POST_REPAIR", 1, "{}", null, actual.Crew, DateTimeOffset.UtcNow.AddDays(1));
        var assignedResult = await repository.AssignItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id,
            itemView.Id, new(task, null, "actual assignment"), Guid.NewGuid().ToString(), approvalView.Version), default);
        Assert.Equal(201, assignedResult.Status); var bindingView = Assert.IsType<RepairTaskBindingFact>(assignedResult.Value);
        var source = await db.Set<RepairItemLifecycleEvent>().SingleAsync(row => row.ItemId == itemView.Id && row.Kind == "ASSIGNED");
        return new(itemView.Id, source.Id, actual.Crew, actual.Supervisor, bindingView.AssignmentId);
    }
    private static H6DispatchPlan Plan(H6Claim claim) => H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson);
    private static async Task<H6Claim> Lease(RoadGuardDbContext db, Guid id)
    {
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == id); var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        return new(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
    }
}
