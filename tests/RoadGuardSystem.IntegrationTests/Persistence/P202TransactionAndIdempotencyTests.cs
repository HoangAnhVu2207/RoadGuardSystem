using System.Reflection;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202TransactionAndIdempotencyTests : IClassFixture<P202SqlServerFixture>
{
    private readonly P202SqlServerFixture _fixture;

    public P202TransactionAndIdempotencyTests(P202SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-02 Negative: same scoped key cannot persist a changed fingerprint")]
    public async Task SameScopedKeyWithChangedFingerprint_IsRejected()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var originalFingerprint = new string('a', 64);
        await InsertIdempotencyRecordAsync(actorId, projectId, "survey.submit", "retry-key", originalFingerprint);

        var action = () => InsertIdempotencyRecordAsync(
            actorId,
            projectId,
            "survey.submit",
            "retry-key",
            new string('b', 64));

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Number.Should().BeOneOf(2601, 2627);

        await using var context = _fixture.CreateDbContext();
        var persisted = await context.Database
            .SqlQueryRaw<string>(
                "SELECT [RequestFingerprint] AS [Value] FROM [IdempotencyRecords] WHERE [ActorUserId] = {0} AND [ProjectId] = {1} AND [Operation] = {2} AND [IdempotencyKey] = {3}",
                actorId,
                projectId,
                "survey.submit",
                "retry-key")
            .SingleAsync();
        persisted.Should().Be(originalFingerprint);
    }

    [Fact(DisplayName = "P2-02 Negative: invalid idempotency outcome JSON is rejected by SQL constraint")]
    public async Task InvalidIdempotencyOutcomeJson_IsRejected()
    {
        var action = () => ExecuteSqlAsync(
            """
            INSERT INTO [IdempotencyRecords]
                ([Id], [ActorUserId], [ProjectId], [Operation], [IdempotencyKey], [RequestFingerprint], [OperationId], [OutcomeJson], [CreatedAtUtc])
            VALUES
                (@id, @actorId, @projectId, 'survey.submit', 'bad-json-key', @fingerprint, @operationId, 'not-json', SYSUTCDATETIME())
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@actorId", Guid.NewGuid()),
            new SqlParameter("@projectId", Guid.NewGuid()),
            new SqlParameter("@fingerprint", new string('a', 64)),
            new SqlParameter("@operationId", Guid.NewGuid()));

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Message.Should().Contain("CK_IdempotencyRecords_OutcomeJson_Json");
    }

    [Fact(DisplayName = "P2-02 Negative: forced failure rolls back a staged domain write")]
    public async Task TransactionService_ForcedFailureRollsBackStagedWrite()
    {
        await using var context = _fixture.CreateDbContext();
        var serviceType = P202ProductionContract.RequireRepositoryType("Transactions.RoadGuardTransactionService");
        var service = Activator.CreateInstance(serviceType, context);
        service.Should().NotBeNull();
        var execute = P202ProductionContract.RequirePublicMethod(
            serviceType,
            "ExecuteAsync",
            isStatic: false,
            parameterCount: 2);
        var probeId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        await context.EnsureActorUserAsync(actorId);

        Func<CancellationToken, Task> operation = async cancellationToken =>
        {
            context.TransactionProbes.Add(new P202TransactionProbe { Id = probeId, Value = "must-rollback" });
            await context.SaveChangesAsync(cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [AuditLogs]
                    ([Id], [ActorUserId], [OccurredAtUtc], [EventType], [EntityType], [EntityId], [BeforeSnapshot], [AfterSnapshot], [Reason], [Source], [CorrelationId])
                VALUES
                    (@id, @actorId, SYSUTCDATETIME(), 'p2_02.rollback', 'P202TransactionProbe', @entityId, NULL, @afterSnapshot, NULL, 'integration_test', @correlationId)
                """,
                [
                    new SqlParameter("@id", Guid.NewGuid()),
                    new SqlParameter("@actorId", actorId),
                    new SqlParameter("@entityId", probeId),
                    new SqlParameter("@afterSnapshot", "{\"value\":\"must-rollback\"}"),
                    new SqlParameter("@correlationId", correlationId)
                ],
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [OutboxMessages]
                    ([Id], [MessageType], [OccurredAtUtc], [CorrelationId], [PayloadJson])
                VALUES
                    (@id, 'p2_02.rollback', SYSUTCDATETIME(), @correlationId, @payloadJson)
                """,
                [
                    new SqlParameter("@id", operationId),
                    new SqlParameter("@correlationId", correlationId),
                    new SqlParameter("@payloadJson", "{\"value\":\"must-rollback\"}")
                ],
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [IdempotencyRecords]
                    ([Id], [ActorUserId], [ProjectId], [Operation], [IdempotencyKey], [RequestFingerprint], [OperationId], [OutcomeJson], [CreatedAtUtc])
                VALUES
                    (@id, @actorId, @projectId, 'p2_02.rollback', 'rollback-key', @fingerprint, @operationId, @outcomeJson, SYSUTCDATETIME())
                """,
                [
                    new SqlParameter("@id", Guid.NewGuid()),
                    new SqlParameter("@actorId", actorId),
                    new SqlParameter("@projectId", projectId),
                    new SqlParameter("@fingerprint", new string('a', 64)),
                    new SqlParameter("@operationId", operationId),
                    new SqlParameter("@outcomeJson", "{\"accepted\":true}")
                ],
                cancellationToken);
            throw new InvalidOperationException("forced failure");
        };

        Func<Task> action = async () =>
        {
            try
            {
                await (Task)execute.Invoke(service, [operation, CancellationToken.None])!;
            }
            catch (Exception exception)
            {
                throw P202ProductionContract.UnwrapInvocation(exception);
            }
        };

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("forced failure");

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.CountAsync(probe => probe.Id == probeId)).Should().Be(0);
        (await verification.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [AuditLogs] WHERE [CorrelationId] = {0}",
                correlationId)
            .SingleAsync()).Should().Be(0);
        (await verification.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [OutboxMessages] WHERE [Id] = {0}",
                operationId)
            .SingleAsync()).Should().Be(0);
        (await verification.Database.SqlQueryRaw<int>(
                "SELECT CAST(COUNT(*) AS int) AS [Value] FROM [IdempotencyRecords] WHERE [IdempotencyKey] = {0}",
                "rollback-key")
            .SingleAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "P2-02 Negative: audit row cannot be updated or deleted through direct SQL")]
    public async Task AuditRow_DirectMutationIsRejected()
    {
        var auditId = Guid.NewGuid();
        await InsertAuditAsync(auditId);

        var update = () => ExecuteSqlAsync(
            "UPDATE [AuditLogs] SET [Reason] = N'changed' WHERE [Id] = @id",
            new SqlParameter("@id", auditId));
        var updateException = await update.Should().ThrowAsync<SqlException>();
        updateException.Which.Message.Should().Contain("AuditLogs are append-only");

        var delete = () => ExecuteSqlAsync(
            "DELETE FROM [AuditLogs] WHERE [Id] = @id",
            new SqlParameter("@id", auditId));
        var deleteException = await delete.Should().ThrowAsync<SqlException>();
        deleteException.Which.Message.Should().Contain("AuditLogs are append-only");
    }

    [Fact(DisplayName = "P2-02 Negative: audit row cannot be updated or deleted through EF application path")]
    public async Task AuditRow_EfMutationIsRejected()
    {
        var auditId = Guid.NewGuid();
        await InsertAuditAsync(auditId);

        await using (var updateContext = _fixture.CreateDbContext())
        {
            var auditEntityType = updateContext.Model.GetEntityTypes()
                .SingleOrDefault(entity => entity.GetTableName() == "AuditLogs");
            auditEntityType.Should().NotBeNull("AuditLog must be part of the production model");
            var audit = await updateContext.FindAsync(auditEntityType!.ClrType, auditId);
            audit.Should().NotBeNull();
            updateContext.Entry(audit!).Property("Reason").CurrentValue = "changed";

            var update = () => updateContext.SaveChangesAsync();
            await update.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("AuditLogs are append-only*");
        }

        await using (var deleteContext = _fixture.CreateDbContext())
        {
            var auditEntityType = deleteContext.Model.GetEntityTypes()
                .Single(entity => entity.GetTableName() == "AuditLogs");
            var audit = await deleteContext.FindAsync(auditEntityType.ClrType, auditId);
            audit.Should().NotBeNull();
            deleteContext.Remove(audit!);

            var delete = () => deleteContext.SaveChangesAsync();
            await delete.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("AuditLogs are append-only*");
        }
    }

    [Fact(DisplayName = "P2-02 Negative: invalid audit snapshot JSON is rejected by SQL constraint")]
    public async Task InvalidAuditSnapshotJson_IsRejected()
    {
        var action = () => ExecuteSqlAsync(
            """
            INSERT INTO [AuditLogs]
                ([Id], [ActorUserId], [OccurredAtUtc], [EventType], [EntityType], [EntityId], [BeforeSnapshot], [AfterSnapshot], [Reason], [Source], [CorrelationId])
            VALUES
                (@id, NULL, SYSUTCDATETIME(), 'p2_02.invalid_json', 'P202TransactionProbe', @entityId, 'not-json', NULL, NULL, 'integration_test', @correlationId)
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@entityId", Guid.NewGuid()),
            new SqlParameter("@correlationId", Guid.NewGuid()));

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Message.Should().Contain("CK_AuditLogs_BeforeSnapshot_Json");
    }

    private Task InsertIdempotencyRecordAsync(
        Guid actorId,
        Guid projectId,
        string operation,
        string key,
        string fingerprint)
        => ExecuteSqlAsync(
            """
            INSERT INTO [IdempotencyRecords]
                ([Id], [ActorUserId], [ProjectId], [Operation], [IdempotencyKey], [RequestFingerprint], [OperationId], [OutcomeJson], [CreatedAtUtc])
            VALUES
                (@id, @actorId, @projectId, @operation, @key, @fingerprint, @operationId, @outcomeJson, SYSUTCDATETIME())
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@actorId", actorId),
            new SqlParameter("@projectId", projectId),
            new SqlParameter("@operation", operation),
            new SqlParameter("@key", key),
            new SqlParameter("@fingerprint", fingerprint),
            new SqlParameter("@operationId", Guid.NewGuid()),
            new SqlParameter("@outcomeJson", "{\"accepted\":true}"));

    private Task InsertAuditAsync(Guid auditId)
        => ExecuteSqlAsync(
            """
            INSERT INTO [AuditLogs]
                ([Id], [ActorUserId], [OccurredAtUtc], [EventType], [EntityType], [EntityId], [BeforeSnapshot], [AfterSnapshot], [Reason], [Source], [CorrelationId])
            VALUES
                (@id, NULL, SYSUTCDATETIME(), 'p2_02.append_only', 'P202TransactionProbe', @entityId, NULL, @afterSnapshot, NULL, 'integration_test', @correlationId)
            """,
            new SqlParameter("@id", auditId),
            new SqlParameter("@entityId", Guid.NewGuid()),
            new SqlParameter("@afterSnapshot", "{\"value\":\"created\"}"),
            new SqlParameter("@correlationId", Guid.NewGuid()));

    private async Task ExecuteSqlAsync(string sql, params SqlParameter[] parameters)
    {
        await using var context = _fixture.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(sql, parameters);
    }
}
