using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6ProtectedInboxSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task CurrentProjectRecipientReadsButMembershipLossHidesListAndDetail()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext(); var repo = Repo(db);
        Assert.NotNull(await repo.GetAsync(seed.Actor, seed.Notification));
        await db.ProjectMembers.Where(x => x.Id == seed.Member).ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Null(await repo.GetAsync(seed.Actor, seed.Notification));
        Assert.Empty((await repo.ListAsync(seed.Actor, null, null, 10)).Items);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentScopeDenialPrecedesFreshReplayAndChangedPayloadConflict(bool replay)
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext(); var repo = Repo(db);
        if (replay) Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
            (await repo.MarkReadAsync(seed.Actor, seed.Notification, "read", Fingerprint("payload"), seed.Version, UserRoleCode.ProjectManager)).Status);
        await db.ProjectMembers.Where(x => x.Id == seed.Member).ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        foreach (var hash in new[] { Fingerprint("payload"), Fingerprint("changed") })
        {
            var result = await repo.MarkReadAsync(seed.Actor, seed.Notification, "read", hash, seed.Version, UserRoleCode.ProjectManager);
            Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound, result.Status); Assert.Null(result.Notification);
        }
        Assert.Equal(replay ? 1 : 0, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == seed.Actor));
    }
    [Fact]
    public async Task ProtectedUnknownSourceIsNotGuessedNonProjectAndGetsAudited()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var unknown = Notification.Create(Guid.NewGuid(), seed.Actor, "Unregistered", Guid.NewGuid(), "created", "Legacy", "Protected legacy content", DateTimeOffset.UtcNow);
        db.Notifications.Add(unknown); await db.SaveChangesAsync();
        Assert.Null(await Repo(db).GetAsync(seed.Actor, unknown.Id));
        Assert.Contains(await db.Set<H6NotificationAuditRow>().Where(x => x.NotificationId == unknown.Id).ToArrayAsync(), x => x.Classification == "UNKNOWN_PROTECTED");
    }
    [Fact]
    public async Task AuthorizationFilterRunsBeforePageLimitAndCannotHideOlderAuthorizedRow()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        for (var i = 0; i < 4; i++) db.Notifications.Add(Notification.Create(Guid.NewGuid(), seed.Actor, "Unregistered", Guid.NewGuid(), "created", "Unknown", "Unknown protected scope", DateTimeOffset.UtcNow.AddMinutes(1 + i)));
        await db.SaveChangesAsync();
        Assert.Equal(seed.Notification, Assert.Single((await Repo(db).ListAsync(seed.Actor, null, null, 1)).Items).Id);
    }
    [Fact]
    public async Task RealPasswordRecoverySourceRemainsNonProjectButOnlyCurrentSupervisorCanRead()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db); var user = User(UserRoleCode.Supervisor); db.Users.Add(user);
        var request = new PasswordRecoveryRequest { Id = Guid.NewGuid(), TargetUserId = user.Id, RequestedAtUtc = DateTimeOffset.UtcNow }; db.PasswordRecoveryRequests.Add(request);
        var notification = Notification.Create(Guid.NewGuid(), user.Id, "PasswordRecoveryRequest", request.Id, "password_recovery_requested", "Password recovery requested", "An active account requires password recovery review.", DateTimeOffset.UtcNow);
        db.Notifications.Add(notification); await db.SaveChangesAsync();
        Assert.NotNull(await Repo(db).GetAsync(user.Id, notification.Id));
        // Controlled external-authority revocation fixture; this is not a production role-change command.
        await db.Users.Where(value => value.Id == user.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.RoleCode, UserRoleCode.ProjectManager));
        Assert.Null(await Repo(db).GetAsync(user.Id, notification.Id));
    }
    [Fact]
    public async Task ChangedTargetCannotLaunderAReceiptFromARevokedOriginalProject()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext(); var repo = Repo(db);
        Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
            (await repo.MarkReadAsync(seed.Actor, seed.Notification, "original-resource", Fingerprint("original"), seed.Version, UserRoleCode.ProjectManager)).Status);
        var project = Project.Create(Guid.NewGuid(), "H6-" + Guid.NewGuid().ToString("N"), "Still authorized", null, null, null, null, DateTimeOffset.UtcNow);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, seed.Actor, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        var other = Notification.Create(Guid.NewGuid(), seed.Actor, "Project", project.Id, "project.event", "Other project", "Current authorized content", DateTimeOffset.UtcNow);
        db.AddRange(project, member, other); await db.SaveChangesAsync();
        await db.ProjectMembers.Where(value => value.Id == seed.Member).ExecuteUpdateAsync(update => update.SetProperty(value => value.Status, ProjectMemberStatus.Ended));
        Assert.NotNull(await repo.GetAsync(seed.Actor, other.Id));
        var result = await repo.MarkReadAsync(seed.Actor, other.Id, "original-resource", Fingerprint("changed-resource"), Convert.ToBase64String(other.RowVersion), UserRoleCode.ProjectManager);
        Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound, result.Status); Assert.Null(result.Notification);
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(value => value.ActorUserId == seed.Actor));
        Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(value => value.Id == other.Id)).ReadAt);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InactivePrincipalCannotReadFreshOrReplayProtectedContent(bool replay)
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext(); var repo = Repo(db);
        if (replay) Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
            (await repo.MarkReadAsync(seed.Actor, seed.Notification, "inactive", Fingerprint("payload"), seed.Version, UserRoleCode.ProjectManager)).Status);
        await db.Users.Where(x => x.Id == seed.Actor).ExecuteUpdateAsync(x => x.SetProperty(u => u.Status, UserStatus.Suspended));
        Assert.Null(await repo.GetAsync(seed.Actor, seed.Notification));
        Assert.Empty((await repo.ListAsync(seed.Actor, null, null, 10)).Items);
        var result = await repo.MarkReadAsync(seed.Actor, seed.Notification, "inactive", Fingerprint("changed"), seed.Version, UserRoleCode.ProjectManager);
        Assert.Equal(NotificationMarkReadPersistenceStatus.Unauthorized, result.Status); Assert.Null(result.Notification);
        Assert.Equal(replay ? 1 : 0, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == seed.Actor));
    }
    [Fact]
    public async Task ClaimedRoleMismatchCannotCreateProtectedReadReceipt()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var result = await Repo(db).MarkReadAsync(seed.Actor, seed.Notification, "role", Fingerprint("payload"), seed.Version, UserRoleCode.Supervisor);
        Assert.Equal(NotificationMarkReadPersistenceStatus.Unauthorized, result.Status);
        Assert.Empty(await db.IdempotencyRecords.Where(x => x.ActorUserId == seed.Actor).ToArrayAsync());
    }
    private async Task<(Guid Actor, Guid Member, Guid Notification, string Version)> Seed()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db); var user = User(UserRoleCode.ProjectManager); db.Users.Add(user);
        var project = Project.Create(Guid.NewGuid(), "H6-" + Guid.NewGuid().ToString("N"), "H6 isolated inbox", null, null, null, null, DateTimeOffset.UtcNow);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, user.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))); db.AddRange(project, member);
        var notification = Notification.Create(Guid.NewGuid(), user.Id, "Project", project.Id, "project.event", "Project event", "Protected project content", DateTimeOffset.UtcNow); db.Notifications.Add(notification);
        await db.SaveChangesAsync(); return (user.Id, member.Id, notification.Id, Convert.ToBase64String(notification.RowVersion));
    }
    private static ApplicationUser User(UserRoleCode role)
    {
        var email = "h6-" + Guid.NewGuid().ToString("N") + "@example.test";
        return new() { Id = Guid.NewGuid(), UserName = email, NormalizedUserName = email.ToUpperInvariant(),
            Email = email, NormalizedEmail = email.ToUpperInvariant(), DisplayName = "H6 fixture", RoleCode = role,
            Status = UserStatus.Active, PasswordHash = "fixture-no-login", MustChangePassword = false, CreatedAt = DateTimeOffset.UtcNow };
    }
    private static string Fingerprint(string payload) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    private static H6ProtectedNotificationRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
}
