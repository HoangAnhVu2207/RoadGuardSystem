using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

[Trait("TaskId", "P2-31")]
public sealed class P231ProcessingPersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P231ProcessingPersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-31: one worker lease wins and completion prevents replay")]
    public async Task OutboxLease_OneWinnerAndCompletionIsReplaySafe()
    {
        await using var context = _fixture.CreateDbContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
        var message = OutboxMessage.Create(
            Guid.NewGuid(),
            "p2_31.processing.admitted",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "{\"jobId\":\"00000000-0000-0000-0000-000000000001\"}");
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var repository = new OutboxWorkRepository(context);
        var now = DateTimeOffset.UtcNow;
        var lease = await repository.TryLeaseNextAsync("worker-a", now, TimeSpan.FromMinutes(5), 3);

        lease.Should().NotBeNull();
        lease!.MessageId.Should().Be(message.Id);
        lease.AttemptCount.Should().Be(1);
        (await repository.TryLeaseNextAsync("worker-b", now, TimeSpan.FromMinutes(5), 3)).Should().BeNull();

        await repository.CompleteAsync(message.Id, "worker-a");
        (await repository.TryLeaseNextAsync("worker-c", now.AddMinutes(6), TimeSpan.FromMinutes(5), 3)).Should().BeNull();
    }

    [Fact(DisplayName = "P2-31: concurrent workers lease one outbox row only once")]
    public async Task OutboxLease_ConcurrentWorkersHaveOneWinner()
    {
        await using var setup = _fixture.CreateDbContext();
        await setup.OutboxMessages.ExecuteDeleteAsync();
        var message = OutboxMessage.Create(
            Guid.NewGuid(),
            "p2_31.processing.race",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "{\"schemaVersion\":1}");
        setup.OutboxMessages.Add(message);
        await setup.SaveChangesAsync();

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var results = await Task.WhenAll(
            new OutboxWorkRepository(firstContext).TryLeaseNextAsync(
                "worker-a", now, TimeSpan.FromMinutes(5), 3),
            new OutboxWorkRepository(secondContext).TryLeaseNextAsync(
                "worker-b", now, TimeSpan.FromMinutes(5), 3));

        results.Count(lease => lease is not null).Should().Be(
            1,
            "workers returned leases {0}",
            string.Join(", ", results.Select(lease => lease is null
                ? "null"
                : $"{lease.MessageId}:{lease.MessageType}")));
        results.Single(lease => lease is not null)!.MessageId.Should().Be(message.Id);
    }

    [Fact(DisplayName = "P2-31: retry exhaustion dead-letters without another lease")]
    public async Task OutboxLease_RetryExhaustionIsDurable()
    {
        await using var context = _fixture.CreateDbContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
        var message = OutboxMessage.Create(
            Guid.NewGuid(),
            "p2_31.processing.retry",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "{\"schemaVersion\":1}");
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var repository = new OutboxWorkRepository(context);
        var lease = await repository.TryLeaseNextAsync("worker-a", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), 1);
        lease.Should().NotBeNull();
        await repository.RetryAsync(
            message.Id,
            "worker-a",
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            "storage_timeout",
            "storage timed out",
            1);

        context.ChangeTracker.Clear();
        var persisted = await context.OutboxMessages.AsNoTracking().SingleAsync(item => item.Id == message.Id);
        persisted.DeliveryStatus.Should().Be(OutboxDeliveryStatus.DeadLetter);
        persisted.LastErrorCode.Should().Be("storage_timeout");
        (await repository.TryLeaseNextAsync("worker-b", DateTimeOffset.UtcNow.AddMinutes(2), TimeSpan.FromMinutes(1), 1)).Should().BeNull();
    }

}
