using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class Huy02NotificationAuthorityTests(IdentitySqlServerFixture fixture)
    : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InactiveRecipientCannotWriteOrReplay(bool replay)
    {
        var (actor, notification, version) = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
        if (replay)
            Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
                (await repository.MarkReadAsync(actor, notification, "read", new string('a', 64), version)).Status);
        await using (var revoke = fixture.CreateDbContext())
        {
            var user = await revoke.Users.SingleAsync(x => x.Id == actor);
            user.Status = UserStatus.Suspended;
            await revoke.SaveChangesAsync();
        }
        var denied = await repository.MarkReadAsync(actor, notification, "read", new string('a', 64), version);
        Assert.Equal("Unauthorized", denied.Status.ToString());
        Assert.Null(denied.Notification);
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(replay ? 1 : 0, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
        Assert.Equal(replay, (await verify.Notifications.SingleAsync(x => x.Id == notification)).ReadAt is not null);
    }

    [Theory]
    [InlineData("must-change", false)]
    [InlineData("must-change", true)]
    [InlineData("role-inactive", false)]
    [InlineData("role-inactive", true)]
    [InlineData("role-snapshot", false)]
    [InlineData("role-snapshot", true)]
    public async Task PrincipalDenialPrecedesWriteReplayAndConflict(string restriction, bool replay)
    {
        var (actor, notification, version) = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
        if (replay)
            Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
                (await repository.MarkReadAsync(actor, notification, "principal", new string('a', 64), version)).Status);
        await using (var change = fixture.CreateDbContext())
        {
            if (restriction == "must-change")
                (await change.Users.SingleAsync(x => x.Id == actor)).MustChangePassword = true;
            if (restriction == "role-inactive")
                (await change.Roles.SingleAsync(x => x.Code == UserRoleCode.ProjectManager)).IsActive = false;
            await change.SaveChangesAsync();
        }
        try
        {
            var role = restriction == "role-snapshot" ? UserRoleCode.RepairCrew : UserRoleCode.ProjectManager;
            foreach (var fingerprint in new[] { new string('a', 64), new string('b', 64) })
            {
                var result = await repository.MarkReadAsync(actor, notification, "principal", fingerprint, version, role);
                Assert.Equal(NotificationMarkReadPersistenceStatus.Unauthorized, result.Status);
                Assert.Null(result.Notification);
            }
            await using var verify = fixture.CreateDbContext();
            Assert.Equal(replay ? 1 : 0, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
        }
        finally
        {
            if (restriction == "role-inactive")
            {
                await using var restore = fixture.CreateDbContext();
                (await restore.Roles.SingleAsync(x => x.Code == UserRoleCode.ProjectManager)).IsActive = true;
                await restore.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task RecipientRelationLossDeniesReceiptWithoutPayload()
    {
        var (actor, notification, version) = await SeedAsync();
        var (other, _, _) = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
        var first = await repository.MarkReadAsync(actor, notification, "relation", new string('a', 64), version);
        Assert.Equal(NotificationMarkReadPersistenceStatus.Success, first.Status);
        await using (var change = fixture.CreateDbContext())
        {
            await change.Notifications.Where(x => x.Id == notification)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.RecipientUserId, other));
        }
        var denied = await repository.MarkReadAsync(actor, notification, "relation", new string('a', 64), version);
        Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound, denied.Status);
        Assert.Null(denied.Notification);
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
    }

    [Fact]
    public async Task MissingRowReceiptReplaysButDeletedSuccessfulPayloadDoesNot()
    {
        var (actor, notification, version) = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
        var missing = Guid.NewGuid();
        Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound,
            (await repository.MarkReadAsync(actor, missing, "missing", new string('a', 64), version)).Status);
        Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound,
            (await repository.MarkReadAsync(actor, missing, "missing", new string('a', 64), version)).Status);
        Assert.Equal(NotificationMarkReadPersistenceStatus.IdempotentConflict,
            (await repository.MarkReadAsync(actor, missing, "missing", new string('b', 64), version)).Status);
        Assert.Equal(NotificationMarkReadPersistenceStatus.Success,
            (await repository.MarkReadAsync(actor, notification, "present", new string('a', 64), version)).Status);
        await db.Notifications.Where(x => x.Id == notification).ExecuteDeleteAsync();
        var deleted = await repository.MarkReadAsync(actor, notification, "present", new string('a', 64), version);
        Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound, deleted.Status);
        Assert.Null(deleted.Notification);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentSameKeyOrCompetingCommandHasSingleReadEffect(bool sameKey)
    {
        var (actor, notification, version) = await SeedAsync();
        async Task<NotificationMarkReadPersistenceResult> ReadAsync(string key)
        {
            await using var db = fixture.CreateDbContext();
            return await new NotificationPersistenceService(db, new IdempotencyOperationService(db))
                .MarkReadAsync(actor, notification, key, new string('a', 64), version);
        }
        var results = await Task.WhenAll(ReadAsync("race"), ReadAsync(sameKey ? "race" : "competitor"));
        Assert.Single(results.Where(x => x.Status == NotificationMarkReadPersistenceStatus.Success));
        Assert.Single(results.Where(x => x.Status == (sameKey
            ? NotificationMarkReadPersistenceStatus.Replayed : NotificationMarkReadPersistenceStatus.StaleConcurrency)));
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(sameKey ? 1 : 2, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
        Assert.Equal(1, await verify.Notifications.CountAsync(x => x.RecipientUserId == actor));
        Assert.Empty(await verify.OutboxMessages.Where(x => x.CorrelationId == notification).ToArrayAsync());
        var readAt = (await verify.Notifications.AsNoTracking().SingleAsync(x => x.Id == notification)).ReadAt;
        Assert.NotNull(readAt);
        var replay = await ReadAsync("race");
        Assert.Equal(results.Single(x => x.Status == NotificationMarkReadPersistenceStatus.Success).Notification!.RowVersion,
            replay.Notification!.RowVersion);
        Assert.Equal(readAt, (await verify.Notifications.AsNoTracking().SingleAsync(x => x.Id == notification)).ReadAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureOrCancellationAfterReadSaveRollsBackEffectAndReceipt(bool cancel)
    {
        var (actor, notification, version) = await SeedAsync();
        using var cancellation = new CancellationTokenSource();
        await using (var db = fixture.CreateDbContext(new FailReceiptInterceptor(cancel ? cancellation : null)))
        {
            var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.MarkReadAsync(
                    actor, notification, "rollback", new string('a', 64), version, cancellation.Token));
            else
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.MarkReadAsync(
                    actor, notification, "rollback", new string('a', 64), version));
        }
        await using var verify = fixture.CreateDbContext();
        Assert.Null((await verify.Notifications.SingleAsync(x => x.Id == notification)).ReadAt);
        Assert.Empty(await verify.IdempotencyRecords.Where(x => x.ActorUserId == actor).ToArrayAsync());
    }

    [Fact]
    public async Task CancelledReplayCannotMutateOrExposeReceipt()
    {
        var (actor, notification, version) = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(db, new IdempotencyOperationService(db));
        await repository.MarkReadAsync(actor, notification, "cancel-replay", new string('a', 64), version);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.MarkReadAsync(
            actor, notification, "cancel-replay", new string('a', 64), version, cancellation.Token));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor));
    }

    private sealed class FailReceiptInterceptor(CancellationTokenSource? cancellation) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(x => x.State == EntityState.Added))
            {
                if (cancellation is null) throw new InvalidOperationException("Injected receipt failure");
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return ValueTask.FromResult(result);
        }
    }

    private async Task<(Guid Actor, Guid Notification, string Version)> SeedAsync()
    {
        await using var db = fixture.CreateDbContext();
        await fixture.SeedRolesAsync(db);
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"huy02_{Guid.NewGuid():N}",
            DisplayName = "Test recipient",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var notification = Notification.Create(Guid.NewGuid(), actor.Id, "Test", Guid.NewGuid(), "test.created",
            "Test", "Owned inbox", DateTimeOffset.UtcNow);
        db.AddRange(actor, notification);
        await db.SaveChangesAsync();
        return (actor.Id, notification.Id, Convert.ToBase64String(notification.RowVersion));
    }
}
