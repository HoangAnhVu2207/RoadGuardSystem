using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

public sealed class ReceiptAccessGuardSqlTests(IdentitySqlServerFixture fixture)
    : IClassFixture<IdentitySqlServerFixture>
{
    private const string Operation = "shared.receipt.guard";
    private static readonly string Fingerprint = new('a', 64);

    // Removing any receipt-path guard must expose the stored outcome after revocation.
    [Theory]
    [InlineData("ordinary", false, false)]
    [InlineData("ordinary", true, false)]
    [InlineData("retry", false, false)]
    [InlineData("retry", true, false)]
    [InlineData("duplicate", false, false)]
    [InlineData("duplicate", true, false)]
    [InlineData("ack", false, false)]
    [InlineData("ack", true, false)]
    [InlineData("ordinary", false, true)]
    [InlineData("ordinary", true, true)]
    [InlineData("retry", false, true)]
    [InlineData("retry", true, true)]
    [InlineData("duplicate", false, true)]
    [InlineData("duplicate", true, true)]
    [InlineData("ack", false, true)]
    [InlineData("ack", true, true)]
    public async Task Every_receipt_path_checks_current_authority_before_outcome_or_conflict(
        string path, bool conflict, bool revoked)
    {
        var actor = await SeedActorAsync();
        var key = Guid.NewGuid().ToString();
        var denied = new UnauthorizedAccessException("Current authority denied");
        var guards = 0;
        var handlers = 0;
        var ack = new FailFirstCommittedInterceptor();
        var afterCommit = new OnCommitInterceptor(async () =>
        {
            if (revoked) await RevokeAsync(actor);
        });
        await using var db = path == "ack"
            ? fixture.CreateRetryingDbContext(afterCommit, ack)
            : fixture.CreateRetryingDbContext();
        // This preflight deliberately becomes stale; it cannot substitute for the guard.
        Assert.Equal(UserStatus.Active, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor)).Status);
        if (path == "ordinary")
        {
            await SeedReceiptAsync(actor, key, Fingerprint);
            if (revoked) await RevokeAsync(actor);
        }
        async Task Guard(CancellationToken token)
        {
            guards++;
            Assert.NotNull(db.Database.CurrentTransaction);
            Assert.NotNull(ExecutionStrategy.Current);
            Assert.Equal(CancellationToken.None, token);
            var status = await db.Database.SqlQueryRaw<byte>(
                "SELECT [Status] AS [Value] FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", actor).SingleAsync(token);
            if (status != (byte)UserStatus.Active) throw denied;
        }
        async Task<(Guid, string)> Handler(CancellationToken token)
        {
            handlers++;
            if (path == "retry")
            {
                await SeedReceiptAsync(actor, key, Fingerprint);
                if (revoked) await RevokeAsync(actor);
                throw new TestTransientException("Retry after competing durable receipt");
            }
            if (path == "duplicate")
            {
                await SeedReceiptAsync(actor, key, Fingerprint);
                if (revoked) await RevokeAsync(actor);
            }
            return (await WriteEffectAsync(db, actor, token), "{\"protected\":true}");
        }
        var requested = conflict && path != "ack" ? new string('b', 64) : Fingerprint;
        if (revoked)
        {
            var actual = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                ExecuteAsync(db, actor, key, requested, Handler, Guard));
            Assert.Same(denied, actual);
        }
        else
        {
            var result = await ExecuteAsync(db, actor, key, requested, Handler, Guard);
            Assert.Equal(conflict && path != "ack" ? IdempotencyOperationStatus.Conflict : IdempotencyOperationStatus.Replayed, result.Status);
            Assert.Equal("{\"protected\":true}", result.OutcomeJson);
        }
        // An ack receipt has the fingerprint of its own new command. Conflict is tested
        // on the subsequent ordinary receipt, not fabricated in the durable transaction.
        if (path == "ack" && conflict && !revoked)
        {
            var result = await ExecuteAsync(db, actor, key, new string('b', 64), Handler, Guard);
            Assert.Equal(IdempotencyOperationStatus.Conflict, result.Status);
        }
        Assert.Equal(path == "ack" && conflict && !revoked ? 2 : 1, guards);
        Assert.Equal(path == "ordinary" ? 0 : 1, handlers);
        if (path == "ack") Assert.Equal(1, ack.FailureCount);
        Assert.Null(db.Database.CurrentTransaction);
        await AssertCountsAsync(actor, key, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Guard_exception_including_retryable_exception_is_propagated_without_retry(bool transient)
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await SeedReceiptAsync(actor, key, Fingerprint);
        await using var db = fixture.CreateRetryingDbContext();
        Exception failure = transient ? new TestTransientException("Guard must not be retried") : new InvalidOperationException("Guard failed");
        var calls = 0;
        Task Guard(CancellationToken _) { calls++; throw failure; }
        var actual = await Assert.ThrowsAnyAsync<Exception>(() => ExecuteAsync(db, actor, key, new string('b', 64),
            _ => throw new InvalidOperationException("Handler must not run"), Guard));
        Assert.Same(failure, actual); Assert.Equal(1, calls);
        Assert.Null(db.Database.CurrentTransaction);
        await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Guard_cancellation_propagates_original_token_and_does_not_run_handler()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await SeedReceiptAsync(actor, key, Fingerprint);
        await using var db = fixture.CreateRetryingDbContext(); using var source = new CancellationTokenSource();
        var failure = new OperationCanceledException(source.Token);
        Task Guard(CancellationToken token)
        {
            Assert.Equal(source.Token, token); source.Cancel(); throw failure;
        }
        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => ExecuteAsync(db, actor, key, Fingerprint,
            _ => throw new InvalidOperationException("Handler must not run"), Guard, source.Token));
        Assert.Same(failure, actual); Assert.Null(db.Database.CurrentTransaction);
        await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Guard_unique_constraint_exception_is_not_mistaken_for_duplicate_receipt_recovery()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await SeedReceiptAsync(actor, key, Fingerprint);
        DbUpdateException failure;
        await using (var duplicate = fixture.CreateDbContext())
        {
            duplicate.Set<IdempotencyRecord>().Add(IdempotencyRecord.Create(actor, null, Operation, key,
                Fingerprint, Guid.NewGuid(), "{}", DateTimeOffset.UtcNow));
            failure = await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
            Assert.IsType<Microsoft.Data.SqlClient.SqlException>(failure.InnerException);
        }
        await using var db = fixture.CreateRetryingDbContext(); var calls = 0;
        var actual = await Assert.ThrowsAsync<DbUpdateException>(() => ExecuteAsync(db, actor, key, Fingerprint,
            _ => throw new InvalidOperationException("No handler"), _ => { calls++; throw failure; }));
        Assert.Same(failure, actual); Assert.Equal(1, calls);
        await AssertCountsAsync(actor, key, 1);
    }

    [Theory]
    [InlineData("retry")]
    [InlineData("duplicate")]
    [InlineData("ack")]
    public async Task Recovery_guard_cancellation_preserves_exception_and_never_reinvokes_handler(string path)
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await using var db = path == "ack" ? fixture.CreateRetryingDbContext(new FailFirstCommittedInterceptor()) : fixture.CreateRetryingDbContext();
        using var source = new CancellationTokenSource();
        var failure = new OperationCanceledException(source.Token); var handlers = 0; var guards = 0;
        async Task<(Guid, string)> Handler(CancellationToken token)
        {
            handlers++;
            if (path != "ack") await SeedReceiptAsync(actor, key, Fingerprint);
            if (path == "retry") throw new TestTransientException("Retry finds competing receipt");
            return (await WriteEffectAsync(db, actor, token), "{\"protected\":true}");
        }
        Task Guard(CancellationToken token)
        {
            guards++; Assert.Equal(source.Token, token); source.Cancel(); throw failure;
        }
        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => ExecuteAsync(db, actor, key, Fingerprint, Handler, Guard, source.Token));
        Assert.Same(failure, actual); Assert.Equal(1, handlers); Assert.Equal(1, guards);
        Assert.Null(db.Database.CurrentTransaction); await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Serializable_guarded_overload_checks_authority_on_conflict()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await SeedReceiptAsync(actor, key, Fingerprint); await RevokeAsync(actor);
        await using var db = fixture.CreateRetryingDbContext();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new IdempotencyOperationService(db).ExecuteSerializableAsync(
            actor, null, Operation, key, new string('b', 64), _ => throw new InvalidOperationException("No handler"),
            CancellationToken.None, token => ReadAuthorityAsync(db, actor, token)));
        await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Acknowledgement_recovery_uses_original_cancellation_token_after_disposal()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        using var source = new CancellationTokenSource();
        var cancel = new OnCommitInterceptor(() => { source.Cancel(); return Task.CompletedTask; });
        await using var db = fixture.CreateRetryingDbContext(cancel, new FailFirstCommittedInterceptor());
        var calls = 0;
        async Task<(Guid, string)> Handler(CancellationToken token)
        { calls++; return (await WriteEffectAsync(db, actor, token), "{\"protected\":true}"); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExecuteAsync(db, actor, key, Fingerprint, Handler,
            token => { Assert.Equal(source.Token, token); token.ThrowIfCancellationRequested(); return Task.CompletedTask; }, source.Token));
        Assert.Equal(1, calls); Assert.Null(db.Database.CurrentTransaction);
        await AssertCountsAsync(actor, key, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Durable_commit_recovery_checks_guard_before_conflicting_competitor_receipt(bool revoked)
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        var denied = new UnauthorizedAccessException("Current authority denied"); var guards = 0; var handlers = 0;
        var commitFault = new FailFirstCommitInterceptor();
        var reads = new ReceiptReadInterceptor(async (count, context) =>
        {
            if (count != 3) return;
            // The failed write transaction must be disposed before durable lookup starts.
            Assert.Null(context.Database.CurrentTransaction);
            await SeedReceiptAsync(actor, key, new string('b', 64));
            if (revoked) await RevokeAsync(actor);
        });
        await using var db = fixture.CreateRetryingDbContext(reads, commitFault);
        async Task<(Guid, string)> Handler(CancellationToken token)
        { handlers++; return (await WriteEffectAsync(db, actor, token), "{\"protected\":true}"); }
        async Task Guard(CancellationToken token)
        {
            guards++;
            try { await ReadAuthorityAsync(db, actor, token); }
            catch (UnauthorizedAccessException) { throw denied; }
        }
        if (revoked)
        {
            var actual = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExecuteAsync(db, actor, key, Fingerprint, Handler, Guard));
            Assert.Same(denied, actual);
        }
        else Assert.Equal(IdempotencyOperationStatus.Conflict, (await ExecuteAsync(db, actor, key, Fingerprint, Handler, Guard)).Status);
        Assert.Equal(1, handlers); Assert.Equal(1, guards); Assert.Equal(1, commitFault.FailureCount);
        Assert.Equal(3, reads.Count); await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Precommit_failure_rolls_back_business_audit_and_receipt()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        var fault = new FailFirstCommitInterceptor();
        await using var db = fixture.CreateDbContext(fault);
        var guards = 0;
        await Assert.ThrowsAsync<TestTransientException>(() => ExecuteAsync(db, actor, key, Fingerprint,
            async token => (await WriteEffectAsync(db, actor, token), "{\"protected\":true}"),
            _ => { guards++; return Task.CompletedTask; }));
        Assert.Equal(0, guards); Assert.Equal(1, fault.FailureCount);
        await AssertCountsAsync(actor, key, 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task No_guard_preserves_positional_token_and_existing_create_replay_conflict(bool serializable)
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await using var db = fixture.CreateRetryingDbContext(new FailFirstCommittedInterceptor());
        var service = new IdempotencyOperationService(db); var calls = 0;
        async Task<(Guid, string)> Handler(CancellationToken token)
        {
            calls++;
            if (serializable) Assert.Equal(System.Data.IsolationLevel.Serializable, db.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            return (await WriteEffectAsync(db, actor, token), "{\"protected\":true}");
        }
        Task<IdempotencyOperationResult> Call(string fingerprint) => serializable
            ? service.ExecuteSerializableAsync(actor, null, Operation, key, fingerprint, Handler, CancellationToken.None)
            : service.ExecuteAsync(actor, null, Operation, key, fingerprint, Handler, CancellationToken.None);
        Assert.Equal(IdempotencyOperationStatus.Replayed, (await Call(Fingerprint)).Status);
        Assert.Equal(IdempotencyOperationStatus.Replayed, (await Call(Fingerprint)).Status);
        Assert.Equal(IdempotencyOperationStatus.Conflict, (await Call(new string('b', 64))).Status);
        Assert.Equal(1, calls); await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Concurrent_same_key_has_one_durable_effect_with_ack_loss_and_guarded_loser()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var winnerDb = fixture.CreateRetryingDbContext(new FailFirstCommittedInterceptor());
        await using var loserDb = fixture.CreateRetryingDbContext();
        var winner = ExecuteAsync(winnerDb, actor, key, Fingerprint, async token =>
        {
            entered.SetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
            return (await WriteEffectAsync(winnerDb, actor, token), "{\"protected\":true}");
        }, token => ReadAuthorityAsync(winnerDb, actor, token));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var loser = ExecuteAsync(loserDb, actor, key, Fingerprint, async token =>
        {
            release.SetResult(); await winner.WaitAsync(TimeSpan.FromSeconds(15), token);
            return (await WriteEffectAsync(loserDb, actor, token), "{\"protected\":true}");
        }, token => ReadAuthorityAsync(loserDb, actor, token));
        var results = await Task.WhenAll(winner, loser).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.All(results, result => Assert.Equal(IdempotencyOperationStatus.Replayed, result.Status));
        Assert.Equal(results[0].OperationId, results[1].OperationId);
        await AssertCountsAsync(actor, key, 1);
    }

    [Fact]
    public async Task Guard_locks_remain_held_through_receipt_transaction_completion()
    {
        var actor = await SeedActorAsync(); var key = Guid.NewGuid().ToString();
        await SeedReceiptAsync(actor, key, Fingerprint);
        var committing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var db = fixture.CreateRetryingDbContext(new OnCommitInterceptor(async () =>
        { committing.SetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(20)); }, beforeCommit: true));
        var replay = ExecuteAsync(db, actor, key, Fingerprint, _ => throw new InvalidOperationException("No handler"),
            token => ReadAuthorityAsync(db, actor, token));
        Task? revoke = null;
        try
        {
            await committing.Task.WaitAsync(TimeSpan.FromSeconds(15));
            revoke = RevokeAsync(actor);
            await using var observer = fixture.CreateDbContext();
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var blocked = false;
            while (DateTime.UtcNow < deadline)
            {
                blocked = await observer.Database.SqlQueryRaw<int>("SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND wait_type LIKE 'LCK_M_%'").SingleAsync() > 0;
                if (blocked) break;
                await Task.Delay(50);
            }
            Assert.True(blocked, "Revoke must be blocked by authoritative locks through commit");
            Assert.False(revoke.IsCompleted); Assert.False(replay.IsCompleted);
        }
        finally { release.TrySetResult(); }
        Assert.Equal(IdempotencyOperationStatus.Replayed, (await replay.WaitAsync(TimeSpan.FromSeconds(15))).Status);
        if (revoke is not null) await revoke.WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExecuteAsync(db, actor, key, Fingerprint,
            _ => throw new InvalidOperationException("No handler"), token => ReadAuthorityAsync(db, actor, token)));
        await AssertCountsAsync(actor, key, 1);
    }

    private async Task<Guid> SeedActorAsync()
    {
        await using var db = fixture.CreateDbContext(); await fixture.SeedRolesAsync(db);
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = id.ToString(),
            NormalizedUserName = id.ToString().ToUpperInvariant(),
            DisplayName = "Isolated shared guard fixture",
            PasswordHash = "non-login-shared-fixture",
            SecurityStamp = id.ToString(),
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(); return id;
    }
    private async Task RevokeAsync(Guid actor)
    {
        await using var db = fixture.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [Status]=2 WHERE [Id]={actor}");
    }
    private static async Task ReadAuthorityAsync(RoadGuardDbContext db, Guid actor, CancellationToken token)
    {
        Assert.NotNull(db.Database.CurrentTransaction);
        Assert.NotNull(ExecutionStrategy.Current);
        var status = await db.Database.SqlQueryRaw<byte>("SELECT [Status] AS [Value] FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", actor).SingleAsync(token);
        if (status != (byte)UserStatus.Active) throw new UnauthorizedAccessException("Current authority denied");
    }
    private static async Task<Guid> WriteEffectAsync(RoadGuardDbContext db, Guid actor, CancellationToken token)
    {
        var id = Guid.NewGuid();
        db.Projects.Add(Project.Create(id, id.ToString(), actor.ToString(), null, null, null, null, DateTimeOffset.UtcNow));
        db.Set<AuditLog>().Add(AuditLog.Create(Guid.NewGuid(), actor, DateTimeOffset.UtcNow, Operation, "Project", id,
            null, null, null, "integration_test", actor));
        await db.SaveChangesAsync(token); return id;
    }
    private async Task SeedReceiptAsync(Guid actor, string key, string fingerprint)
    {
        await using var db = fixture.CreateDbContext();
        var result = await new IdempotencyOperationService(db).ExecuteAsync(actor, null, Operation, key, fingerprint,
            async token => (await WriteEffectAsync(db, actor, token), "{\"protected\":true}"));
        Assert.Equal(IdempotencyOperationStatus.Executed, result.Status);
    }
    private async Task AssertCountsAsync(Guid actor, string key, int count)
    {
        await using var db = fixture.CreateDbContext();
        Assert.Equal(count, await db.Projects.CountAsync(x => x.Name == actor.ToString()));
        Assert.Equal(count, await db.Set<AuditLog>().CountAsync(x => x.CorrelationId == actor));
        Assert.Equal(count, await db.Set<IdempotencyRecord>().CountAsync(x => x.ActorUserId == actor && x.Operation == Operation && x.IdempotencyKey == key));
    }
    private static Task<IdempotencyOperationResult> ExecuteAsync(RoadGuardDbContext db, Guid actor, string key, string fingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> handler,
        Func<CancellationToken, Task> guard, CancellationToken token = default)
        => new IdempotencyOperationService(db).ExecuteAsync(actor, null, Operation, key, fingerprint, handler, token, guard);
    private sealed class ReceiptReadInterceptor(Func<int, RoadGuardDbContext, Task> beforeRead) : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM [IdempotencyRecords]", StringComparison.Ordinal))
                await beforeRead(++Count, (RoadGuardDbContext)eventData.Context!);
            return result;
        }
    }
    private sealed class OnCommitInterceptor(Func<Task> callback, bool beforeCommit = false) : DbTransactionInterceptor
    {
        private int _armed = 1;
        private Task InvokeOnceAsync() => Interlocked.Exchange(ref _armed, 0) == 1 ? callback() : Task.CompletedTask;
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        { if (beforeCommit) await InvokeOnceAsync(); return result; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default) => beforeCommit ? Task.CompletedTask : InvokeOnceAsync();
    }
}
