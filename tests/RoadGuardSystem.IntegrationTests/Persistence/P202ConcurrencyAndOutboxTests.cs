using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202ConcurrencyAndOutboxTests : IClassFixture<P202SqlServerFixture>
{
    private readonly P202SqlServerFixture _fixture;

    public P202ConcurrencyAndOutboxTests(P202SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-02 Negative: stale rowversion update cannot overwrite winning value")]
    public async Task StaleRowVersionUpdate_IsRejected()
    {
        var id = Guid.NewGuid();
        await using (var setup = _fixture.CreateDbContext())
        {
            setup.TransactionProbes.Add(new P202TransactionProbe { Id = id, Value = "initial" });
            await setup.SaveChangesAsync();
        }

        await using var winner = _fixture.CreateDbContext();
        await using var stale = _fixture.CreateDbContext();
        var winnerProbe = await winner.TransactionProbes.SingleAsync(probe => probe.Id == id);
        var staleProbe = await stale.TransactionProbes.SingleAsync(probe => probe.Id == id);

        winnerProbe.Value = "winner";
        await winner.SaveChangesAsync();

        staleProbe.Value = "stale";
        var action = () => stale.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var verification = _fixture.CreateDbContext();
        (await verification.TransactionProbes.SingleAsync(probe => probe.Id == id))
            .Value.Should().Be("winner");
    }

    [Fact(DisplayName = "P2-02 Positive: fresh rowversion update commits and advances token")]
    public async Task FreshRowVersionUpdate_AdvancesToken()
    {
        var id = Guid.NewGuid();
        await using var context = _fixture.CreateDbContext();
        var probe = new P202TransactionProbe { Id = id, Value = "initial" };
        context.TransactionProbes.Add(probe);
        await context.SaveChangesAsync();
        var firstVersion = probe.RowVersion.ToArray();

        probe.Value = "updated";
        await context.SaveChangesAsync();

        probe.RowVersion.Should().NotBeEmpty();
        probe.RowVersion.Should().NotEqual(firstVersion);
    }

    [Fact(DisplayName = "P2-02 Negative: duplicate consumer delivery is rejected by durable uniqueness")]
    public async Task DuplicateConsumerDelivery_IsRejected()
    {
        var messageId = Guid.NewGuid();
        await InsertOutboxMessageAsync(messageId);
        await InsertConsumerReceiptAsync(messageId, "dashboard-projection", Guid.NewGuid());

        var action = () => InsertConsumerReceiptAsync(messageId, "dashboard-projection", Guid.NewGuid());

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Number.Should().BeOneOf(2601, 2627);
    }

    [Fact(DisplayName = "P2-02 Negative: invalid outbox JSON is rejected by SQL constraint")]
    public async Task InvalidOutboxJson_IsRejected()
    {
        var action = () => ExecuteSqlAsync(
            """
            INSERT INTO [OutboxMessages]
                ([Id], [MessageType], [OccurredAtUtc], [CorrelationId], [PayloadJson])
            VALUES
                (@id, 'p2_02.invalid_json', SYSUTCDATETIME(), @correlationId, 'not-json')
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@correlationId", Guid.NewGuid()));

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Message.Should().Contain("CK_OutboxMessages_PayloadJson_Json");
    }

    [Fact(DisplayName = "P2-02 Edge: oversized outbox event metadata is rejected by SQL mapping")]
    public async Task OversizedOutboxMessageType_IsRejected()
    {
        var action = () => ExecuteSqlAsync(
            """
            INSERT INTO [OutboxMessages]
                ([Id], [MessageType], [OccurredAtUtc], [CorrelationId], [PayloadJson])
            VALUES
                (@id, @messageType, SYSUTCDATETIME(), @correlationId, @payloadJson)
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@messageType", new string('e', 201)),
            new SqlParameter("@correlationId", Guid.NewGuid()),
            new SqlParameter("@payloadJson", "{\"schemaVersion\":1}"));

        await action.Should().ThrowAsync<SqlException>();
    }

    private Task InsertOutboxMessageAsync(Guid messageId)
        => ExecuteSqlAsync(
            """
            INSERT INTO [OutboxMessages]
                ([Id], [MessageType], [OccurredAtUtc], [CorrelationId], [PayloadJson])
            VALUES
                (@id, 'p2_02.consumer_test', SYSUTCDATETIME(), @correlationId, @payloadJson)
            """,
            new SqlParameter("@id", messageId),
            new SqlParameter("@correlationId", Guid.NewGuid()),
            new SqlParameter("@payloadJson", "{\"schemaVersion\":1}"));

    private Task InsertConsumerReceiptAsync(Guid messageId, string consumerName, Guid effectId)
        => ExecuteSqlAsync(
            """
            INSERT INTO [ConsumerEffectReceipts]
                ([Id], [MessageId], [ConsumerName], [EffectId], [ProcessedAtUtc])
            VALUES
                (@id, @messageId, @consumerName, @effectId, SYSUTCDATETIME())
            """,
            new SqlParameter("@id", Guid.NewGuid()),
            new SqlParameter("@messageId", messageId),
            new SqlParameter("@consumerName", consumerName),
            new SqlParameter("@effectId", effectId));

    private async Task ExecuteSqlAsync(string sql, params SqlParameter[] parameters)
    {
        await using var context = _fixture.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(sql, parameters);
    }
}
