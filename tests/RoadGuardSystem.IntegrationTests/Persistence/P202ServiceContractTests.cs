using System.Reflection;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202ServiceContractTests : IClassFixture<P202SqlServerFixture>
{
    private readonly P202SqlServerFixture _fixture;

    public P202ServiceContractTests(P202SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> InvalidScopes()
    {
        yield return [new string('o', 101), "valid-key", new string('a', 64), "operation"];
        yield return ["survey.submit", new string('k', 201), new string('a', 64), "idempotencyKey"];
        yield return ["survey.submit", "valid-key", "NOT-A-SHA256", "requestFingerprint"];
    }

    [Fact(DisplayName = "P2-02 Negative: invalid scoped idempotency input is rejected before callback")]
    public async Task InvalidIdempotencyInput_IsRejectedBeforeCallback()
    {
        await using var context = _fixture.CreateDbContext();
        var callbackInvoked = false;
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> callback = _ =>
        {
            callbackInvoked = true;
            return Task.FromResult((Guid.NewGuid(), "{\"accepted\":true}"));
        };

        var action = () => ExecuteIdempotentAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "survey.submit",
            " ",
            new string('a', 64),
            callback);

        var exception = await action.Should().ThrowAsync<ArgumentException>();
        exception.Which.ParamName.Should().Be("idempotencyKey");
        callbackInvoked.Should().BeFalse();
        (await CountIdempotencyAsync(context, " ")).Should().Be(0);
    }

    [Theory(DisplayName = "P2-02 Edge: oversized or malformed idempotency scope is rejected before callback")]
    [MemberData(nameof(InvalidScopes))]
    public async Task InvalidIdempotencyScope_IsRejectedBeforeCallback(
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        string expectedParameter)
    {
        await using var context = _fixture.CreateDbContext();
        var callbackInvoked = false;
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> callback = _ =>
        {
            callbackInvoked = true;
            return Task.FromResult((Guid.NewGuid(), "{\"accepted\":true}"));
        };

        var action = () => ExecuteIdempotentAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            operation,
            idempotencyKey,
            requestFingerprint,
            callback);

        var exception = await action.Should().ThrowAsync<ArgumentException>();
        exception.Which.ParamName.Should().Be(expectedParameter);
        callbackInvoked.Should().BeFalse();
    }

    [Fact(DisplayName = "P2-02 Negative: SQL interruption surfaces failure without false idempotency success")]
    public async Task SqlInterruption_DoesNotRecordFalseSuccess()
    {
        var isolatedDatabase = new SqlServerTestFixture();
        await isolatedDatabase.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<P202TestDbContext>()
                .UseSqlServer(isolatedDatabase.ConnectionString)
                .Options;
            await using var context = new P202TestDbContext(options);
            await isolatedDatabase.DropDatabaseAsync();
            var callbackInvoked = false;

            var action = () => ExecuteIdempotentAsync(
                context,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "survey.submit",
                "interruption-key",
                new string('a', 64),
                _ =>
                {
                    callbackInvoked = true;
                    return Task.FromResult((Guid.NewGuid(), "{\"accepted\":true}"));
                });

            var exception = await action.Should().ThrowAsync<Exception>();
            FindSqlException(exception.Which).Should().NotBeNull();
            callbackInvoked.Should().BeFalse();
            (await isolatedDatabase.DatabaseExistsAsync(isolatedDatabase.DatabaseName)).Should().BeFalse();
        }
        finally
        {
            await isolatedDatabase.DisposeAsync();
        }
    }

    [Fact(DisplayName = "P2-02 Positive: transaction boundary supports configured SQL retry execution strategy")]
    public async Task TransactionBoundary_SupportsRetryExecutionStrategy()
    {
        var options = new DbContextOptionsBuilder<P202TestDbContext>()
            .UseSqlServer(_fixture.ConnectionString, sql => sql.EnableRetryOnFailure(1))
            .Options;
        await using var context = new P202TestDbContext(options);
        var serviceType = P202ProductionContract.RequireRepositoryType("Transactions.RoadGuardTransactionService");
        var service = Activator.CreateInstance(serviceType, context);
        service.Should().NotBeNull();
        var execute = P202ProductionContract.RequirePublicMethod(serviceType, "ExecuteAsync", isStatic: false, parameterCount: 2);
        var probeId = Guid.NewGuid();
        Func<CancellationToken, Task> operation = cancellationToken =>
        {
            context.TransactionProbes.Add(new P202TransactionProbe { Id = probeId, Value = "retry-strategy" });
            return Task.CompletedTask;
        };

        await (Task)execute.Invoke(service, [operation, CancellationToken.None])!;

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => probe.Id == probeId)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Positive: idempotency transaction supports configured SQL retry execution strategy")]
    public async Task IdempotencyBoundary_SupportsRetryExecutionStrategy()
    {
        var options = new DbContextOptionsBuilder<P202TestDbContext>()
            .UseSqlServer(_fixture.ConnectionString, sql => sql.EnableRetryOnFailure(1))
            .Options;
        await using var context = new P202TestDbContext(options);

        var result = await ExecuteIdempotentAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "survey.submit",
            "retry-strategy-key",
            new string('a', 64),
            _ => Task.FromResult((Guid.NewGuid(), "{\"accepted\":true}")));

        ReadProperty(result, "Status").ToString().Should().Be("Executed");
        (await CountIdempotencyAsync(context, "retry-strategy-key")).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Positive: domain audit outbox and idempotency outcome commit atomically")]
    public async Task AtomicWrite_CommitsDomainAuditOutboxAndOutcome()
    {
        await using var context = _fixture.CreateDbContext();
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var probeId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var sensitiveValue = $"secret-{Guid.NewGuid():N}";
        var snapshot = BuildSnapshot(
            $"{{\"safe\":\"committed\",\"token\":\"{sensitiveValue}\"}}",
            ["safe", "token"],
            4096);
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> callback = async cancellationToken =>
        {
            context.TransactionProbes.Add(new P202TransactionProbe { Id = probeId, Value = "committed" });
            await context.SaveChangesAsync(cancellationToken);
            await InsertAuditAsync(context, actorId, probeId, correlationId, snapshot, cancellationToken);
            await InsertOutboxAsync(context, operationId, correlationId, snapshot, cancellationToken);
            return (operationId, "{\"accepted\":true}");
        };

        var result = await ExecuteIdempotentAsync(
            context,
            actorId,
            projectId,
            "survey.submit",
            "atomic-key",
            new string('a', 64),
            callback);

        ReadProperty(result, "Status").ToString().Should().Be("Executed");
        ReadProperty<Guid>(result, "OperationId").Should().Be(operationId);
        ReadProperty<Guid?>(result, "ActorUserId").Should().Be(actorId);
        ReadProperty<Guid?>(result, "ProjectId").Should().Be(projectId);

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => probe.Id == probeId)).Should().Be(1);
        (await CountAuditAsync(verification, correlationId)).Should().Be(1);
        (await CountOutboxAsync(verification, operationId)).Should().Be(1);
        (await CountIdempotencyAsync(verification, "atomic-key")).Should().Be(1);
        (await verification.Database.SqlQueryRaw<string>(
                "SELECT [AfterSnapshot] AS [Value] FROM [AuditLogs] WHERE [CorrelationId] = {0}",
                correlationId)
            .SingleAsync()).Should().NotContain(sensitiveValue);
        (await verification.Database.SqlQueryRaw<string>(
                "SELECT [PayloadJson] AS [Value] FROM [OutboxMessages] WHERE [Id] = {0}",
                operationId)
            .SingleAsync()).Should().NotContain(sensitiveValue);
    }

    [Fact(DisplayName = "P2-02 Positive: same scope key and fingerprint replays stable outcome without callback")]
    public async Task SameFingerprintReplay_ReturnsStableOutcomeWithoutCallback()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var fingerprint = new string('c', 64);
        await using (var firstContext = _fixture.CreateDbContext())
        {
            await ExecuteIdempotentAsync(
                firstContext,
                actorId,
                projectId,
                "survey.submit",
                "replay-key",
                fingerprint,
                _ => Task.FromResult((operationId, "{\"accepted\":true}")));
        }

        var callbackInvoked = false;
        await using var replayContext = _fixture.CreateDbContext();
        var replay = await ExecuteIdempotentAsync(
            replayContext,
            actorId,
            projectId,
            "survey.submit",
            "replay-key",
            fingerprint,
            _ =>
            {
                callbackInvoked = true;
                return Task.FromResult((Guid.NewGuid(), "{\"accepted\":false}"));
            });

        callbackInvoked.Should().BeFalse();
        ReadProperty(replay, "Status").ToString().Should().Be("Replayed");
        ReadProperty<Guid>(replay, "OperationId").Should().Be(operationId);
        ReadProperty<string>(replay, "OutcomeJson").Should().Be("{\"accepted\":true}");
        (await CountIdempotencyAsync(replayContext, "replay-key")).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Negative: changed fingerprint returns conflict without callback or overwrite")]
    public async Task ChangedFingerprintReplay_ReturnsConflictWithoutCallback()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await using (var firstContext = _fixture.CreateDbContext())
        {
            await ExecuteIdempotentAsync(
                firstContext,
                actorId,
                projectId,
                "survey.submit",
                "conflict-key",
                new string('d', 64),
                _ => Task.FromResult((operationId, "{\"accepted\":true}")));
        }

        var callbackInvoked = false;
        await using var conflictContext = _fixture.CreateDbContext();
        var conflict = await ExecuteIdempotentAsync(
            conflictContext,
            actorId,
            projectId,
            "survey.submit",
            "conflict-key",
            new string('e', 64),
            _ =>
            {
                callbackInvoked = true;
                return Task.FromResult((Guid.NewGuid(), "{\"accepted\":false}"));
            });

        callbackInvoked.Should().BeFalse();
        ReadProperty(conflict, "Status").ToString().Should().Be("Conflict");
        ReadProperty<Guid>(conflict, "OperationId").Should().Be(operationId);
        (await CountIdempotencyAsync(conflictContext, "conflict-key")).Should().Be(1);
        (await conflictContext.Database.SqlQueryRaw<string>(
                "SELECT [RequestFingerprint] AS [Value] FROM [IdempotencyRecords] WHERE [IdempotencyKey] = {0}",
                "conflict-key")
            .SingleAsync()).Should().Be(new string('d', 64));
    }

    [Fact(DisplayName = "P2-02 Positive: identical key in different actor project or operation scopes is independent")]
    public async Task SameKeyInDistinctScopes_IsAllowed()
    {
        var actor1 = Guid.NewGuid();
        var actor2 = Guid.NewGuid();
        var project1 = Guid.NewGuid();
        var project2 = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();

        await ExecuteIdempotentAsync(context, actor1, project1, "survey.submit", "shared-key", new string('f', 64), AcceptedOutcome);
        context.ChangeTracker.Clear();
        await ExecuteIdempotentAsync(context, actor2, project1, "survey.submit", "shared-key", new string('f', 64), AcceptedOutcome);
        context.ChangeTracker.Clear();
        await ExecuteIdempotentAsync(context, actor1, project2, "survey.submit", "shared-key", new string('f', 64), AcceptedOutcome);
        context.ChangeTracker.Clear();
        await ExecuteIdempotentAsync(context, actor1, project1, "survey.cancel", "shared-key", new string('f', 64), AcceptedOutcome);

        (await CountIdempotencyAsync(context, "shared-key")).Should().Be(4);
    }

    [Fact(DisplayName = "P2-02 Positive: concurrent first submissions commit one outcome and one effect")]
    public async Task ConcurrentFirstSubmissions_CommitOneOutcome()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var callbackCount = 0;
        var tasks = Enumerable.Range(0, 2).Select(async index =>
        {
            await using var context = _fixture.CreateDbContext();
            return await ExecuteIdempotentAsync(
                context,
                actorId,
                projectId,
                "survey.submit",
                "race-key",
                new string('1', 64),
                async cancellationToken =>
                {
                    Interlocked.Increment(ref callbackCount);
                    context.TransactionProbes.Add(new P202TransactionProbe
                    {
                        Id = Guid.NewGuid(),
                        Value = $"race-{index}"
                    });
                    await context.SaveChangesAsync(cancellationToken);
                    await Task.Delay(50, cancellationToken);
                    return (operationId, "{\"accepted\":true}");
                });
        });

        var results = await Task.WhenAll(tasks);

        results.Select(result => ReadProperty(result, "Status").ToString())
            .Should().BeEquivalentTo(["Executed", "Replayed"]);
        results.Select(result => ReadProperty<Guid>(result, "OperationId")).Should().OnlyContain(id => id == operationId);
        callbackCount.Should().BeInRange(1, 2);

        await using var verification = _fixture.CreateDbContext();
        (await CountIdempotencyAsync(verification, "race-key")).Should().Be(1);
        (await verification.TransactionProbes.CountAsync(probe => probe.Value.StartsWith("race-"))).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Negative F-02: forced consumer failure rolls back durable effect and receipt")]
    public async Task ConsumerFailure_RollsBackDurableEffectAndReceipt()
    {
        var messageId = Guid.NewGuid();
        var effectId = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();
        await InsertOutboxAsync(context, messageId, Guid.NewGuid(), "{\"schemaVersion\":1}", CancellationToken.None);

        var action = () => ProcessConsumerEffectAsync(
            context,
            messageId,
            "dashboard-projection",
            effectId,
            async cancellationToken =>
            {
                context.TransactionProbes.Add(new P202TransactionProbe { Id = effectId, Value = "must-rollback" });
                await context.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("forced consumer failure");
            });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("forced consumer failure");

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => probe.Id == effectId)).Should().Be(0);
        (await CountConsumerReceiptAsync(verification, messageId, "dashboard-projection")).Should().Be(0);
    }

    [Fact(DisplayName = "P2-02 Negative F-02: replay does not execute or persist another durable effect")]
    public async Task ConsumerReplay_DoesNotExecuteDurableEffectAgain()
    {
        var messageId = Guid.NewGuid();
        var firstEffectId = Guid.NewGuid();
        var replayEffectId = Guid.NewGuid();
        await using (var setup = _fixture.CreateDbContext())
        {
            await InsertOutboxAsync(setup, messageId, Guid.NewGuid(), "{\"schemaVersion\":1}", CancellationToken.None);
            await ProcessConsumerEffectAsync(
                setup,
                messageId,
                "dashboard-projection",
                firstEffectId,
                cancellationToken =>
                {
                    setup.TransactionProbes.Add(new P202TransactionProbe { Id = firstEffectId, Value = "first-effect" });
                    return Task.CompletedTask;
                });
        }

        var replayCallbackInvoked = false;
        await using var replayContext = _fixture.CreateDbContext();
        var replay = await ProcessConsumerEffectAsync(
            replayContext,
            messageId,
            "dashboard-projection",
            replayEffectId,
            cancellationToken =>
            {
                replayCallbackInvoked = true;
                replayContext.TransactionProbes.Add(new P202TransactionProbe { Id = replayEffectId, Value = "duplicate-effect" });
                return Task.CompletedTask;
            });

        replayCallbackInvoked.Should().BeFalse();
        ReadProperty(replay, "Status").ToString().Should().Be("Replayed");
        ReadProperty<Guid>(replay, "EffectId").Should().Be(firstEffectId);

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(
            probe => probe.Id == firstEffectId || probe.Id == replayEffectId)).Should().Be(1);
        (await CountConsumerReceiptAsync(verification, messageId, "dashboard-projection")).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Edge F-02: concurrent consumer deliveries commit one durable effect and receipt")]
    public async Task ConcurrentConsumerDeliveries_CommitOneDurableEffectAndReceipt()
    {
        var messageId = Guid.NewGuid();
        var effectIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await using (var setup = _fixture.CreateDbContext())
        {
            await InsertOutboxAsync(setup, messageId, Guid.NewGuid(), "{\"schemaVersion\":1}", CancellationToken.None);
        }

        var deliveries = effectIds.Select(async (effectId, index) =>
        {
            await using var context = _fixture.CreateDbContext();
            return await ProcessConsumerEffectAsync(
                context,
                messageId,
                "dashboard-projection",
                effectId,
                async cancellationToken =>
                {
                    context.TransactionProbes.Add(new P202TransactionProbe
                    {
                        Id = effectId,
                        Value = $"concurrent-effect-{index}"
                    });
                    await Task.Delay(75, cancellationToken);
                });
        });

        var results = await Task.WhenAll(deliveries);

        results.Select(result => ReadProperty(result, "Status").ToString())
            .Should().BeEquivalentTo(["Recorded", "Replayed"]);

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => effectIds.Contains(probe.Id))).Should().Be(1);
        (await CountConsumerReceiptAsync(verification, messageId, "dashboard-projection")).Should().Be(1);
    }

    [Fact(DisplayName = "P2-02 Positive F-02: first consumer delivery commits durable effect and receipt atomically")]
    public async Task FirstConsumerDelivery_CommitsDurableEffectAndReceipt()
    {
        var messageId = Guid.NewGuid();
        var effectId = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();
        await InsertOutboxAsync(context, messageId, Guid.NewGuid(), "{\"schemaVersion\":1}", CancellationToken.None);

        var result = await ProcessConsumerEffectAsync(
            context,
            messageId,
            "dashboard-projection",
            effectId,
            cancellationToken =>
            {
                context.TransactionProbes.Add(new P202TransactionProbe { Id = effectId, Value = "committed-effect" });
                return Task.CompletedTask;
            });

        ReadProperty(result, "Status").ToString().Should().Be("Recorded");
        ReadProperty<Guid>(result, "EffectId").Should().Be(effectId);

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => probe.Id == effectId)).Should().Be(1);
        (await CountConsumerReceiptAsync(verification, messageId, "dashboard-projection")).Should().Be(1);
    }

    private static Task<(Guid OperationId, string OutcomeJson)> AcceptedOutcome(CancellationToken _)
        => Task.FromResult((Guid.NewGuid(), "{\"accepted\":true}"));

    private static async Task<object> ExecuteIdempotentAsync(
        P202TestDbContext context,
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> callback)
    {
        var serviceType = P202ProductionContract.RequireRepositoryType("Idempotency.IdempotencyOperationService");
        var service = Activator.CreateInstance(serviceType, context);
        service.Should().NotBeNull();
        var method = P202ProductionContract.RequirePublicMethod(serviceType, "ExecuteAsync", isStatic: false, parameterCount: 7);

        return await InvokeResultAsync(
            method,
            service,
            [actorUserId, projectId, operation, idempotencyKey, requestFingerprint, callback, CancellationToken.None]);
    }

    private static async Task<object> ProcessConsumerEffectAsync(
        P202TestDbContext context,
        Guid messageId,
        string consumerName,
        Guid effectId,
        Func<CancellationToken, Task> durableEffect)
    {
        var serviceType = P202ProductionContract.RequireRepositoryType("Messaging.ConsumerEffectService");
        var service = Activator.CreateInstance(serviceType, context);
        service.Should().NotBeNull();
        var method = P202ProductionContract.RequirePublicMethod(serviceType, "ProcessAsync", isStatic: false, parameterCount: 5);

        return await InvokeResultAsync(
            method,
            service,
            [messageId, consumerName, effectId, durableEffect, CancellationToken.None]);
    }

    private static async Task<object> InvokeResultAsync(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            var task = (Task)method.Invoke(target, arguments)!;
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        catch (Exception exception)
        {
            throw P202ProductionContract.UnwrapInvocation(exception);
        }
    }

    private static string BuildSnapshot(string json, string[] allowList, int maxUtf8Bytes)
    {
        var type = P202ProductionContract.RequireRepositoryType("Auditing.AuditSnapshotBuilder");
        var method = P202ProductionContract.RequirePublicMethod(type, "Build", isStatic: true, parameterCount: 3);
        return (string)method.Invoke(null, [json, allowList, maxUtf8Bytes])!;
    }

    private static async Task InsertAuditAsync(
        P202TestDbContext context,
        Guid actorId,
        Guid entityId,
        Guid correlationId,
        string afterSnapshot,
        CancellationToken cancellationToken)
    {
        await context.EnsureActorUserAsync(actorId);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO [AuditLogs]
                ([Id], [ActorUserId], [OccurredAtUtc], [EventType], [EntityType], [EntityId], [BeforeSnapshot], [AfterSnapshot], [Reason], [Source], [CorrelationId])
            VALUES
                (@id, @actorId, SYSUTCDATETIME(), 'p2_02.transaction_probe_committed', 'P202TransactionProbe', @entityId, NULL, @afterSnapshot, NULL, 'integration_test', @correlationId)
            """,
            [
                new SqlParameter("@id", Guid.NewGuid()),
                new SqlParameter("@actorId", actorId),
                new SqlParameter("@entityId", entityId),
                new SqlParameter("@afterSnapshot", afterSnapshot),
                new SqlParameter("@correlationId", correlationId)
            ],
            cancellationToken);
    }

    private static async Task InsertOutboxAsync(
        P202TestDbContext context,
        Guid messageId,
        Guid correlationId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO [OutboxMessages]
                ([Id], [MessageType], [OccurredAtUtc], [CorrelationId], [PayloadJson])
            VALUES
                (@id, 'p2_02.transaction_probe_committed', SYSUTCDATETIME(), @correlationId, @payloadJson)
            """,
            [
                new SqlParameter("@id", messageId),
                new SqlParameter("@correlationId", correlationId),
                new SqlParameter("@payloadJson", payloadJson)
            ],
            cancellationToken);
    }

    private static Task<int> CountAuditAsync(P202TestDbContext context, Guid correlationId)
        => context.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [AuditLogs] WHERE [CorrelationId] = {0}",
                correlationId)
            .SingleAsync();

    private static Task<int> CountOutboxAsync(P202TestDbContext context, Guid messageId)
        => context.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [OutboxMessages] WHERE [Id] = {0}",
                messageId)
            .SingleAsync();

    private static Task<int> CountIdempotencyAsync(P202TestDbContext context, string idempotencyKey)
        => context.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [IdempotencyRecords] WHERE [IdempotencyKey] = {0}",
                idempotencyKey)
            .SingleAsync();

    private static Task<int> CountConsumerReceiptAsync(
        P202TestDbContext context,
        Guid messageId,
        string consumerName)
        => context.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [ConsumerEffectReceipts] WHERE [MessageId] = {0} AND [ConsumerName] = {1}",
                messageId,
                consumerName)
            .SingleAsync();

    private static object ReadProperty(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    private static T ReadProperty<T>(object instance, string propertyName)
        => (T)ReadProperty(instance, propertyName);

    private static SqlException? FindSqlException(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }
}
