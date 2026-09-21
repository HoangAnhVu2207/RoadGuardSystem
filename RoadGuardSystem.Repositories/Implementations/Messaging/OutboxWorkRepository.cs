using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class OutboxWorkRepository : IOutboxWorkRepository
{
    private readonly RoadGuardDbContext _context;

    public OutboxWorkRepository(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxWorkLease?> TryLeaseNextAsync(
        string workerReference,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerReference);
        if (leaseDuration <= TimeSpan.Zero || maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        OutboxWorkLease? result = null;
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            var normalizedNow = now.ToUniversalTime();
            var message = await _context.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT TOP (1) *
                    FROM [dbo].[OutboxMessages] WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE [DeliveryStatus] NOT IN ({(byte)OutboxDeliveryStatus.Completed}, {(byte)OutboxDeliveryStatus.DeadLetter})
                      AND [DeliveryAttemptCount] < {maxAttempts}
                      AND [NextAttemptAtUtc] <= {normalizedNow}
                      AND ([LeaseExpiresAtUtc] IS NULL OR [LeaseExpiresAtUtc] <= {normalizedNow})
                    ORDER BY [NextAttemptAtUtc], [OccurredAtUtc]
                    """)
                .FirstOrDefaultAsync(cancellationToken);
            if (message is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            message.AcquireLease(workerReference, normalizedNow, leaseDuration, maxAttempts);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result = new OutboxWorkLease(
                message.Id,
                message.MessageType,
                message.OccurredAtUtc,
                message.CorrelationId,
                message.PayloadJson,
                message.DeliveryAttemptCount,
                message.LeaseExpiresAtUtc!.Value);
        });
        return result;
    }

    public Task CompleteAsync(
        Guid messageId,
        string workerReference,
        CancellationToken cancellationToken = default)
        => ExecuteLeaseMutationAsync(
            messageId,
            workerReference,
            message => message.CompleteLease(workerReference),
            cancellationToken);

    public Task RetryAsync(
        Guid messageId,
        string workerReference,
        DateTimeOffset now,
        TimeSpan retryDelay,
        string errorCode,
        string errorMessage,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        }

        return ExecuteLeaseMutationAsync(
            messageId,
            workerReference,
            message => message.ScheduleRetry(
                workerReference,
                now.ToUniversalTime().Add(retryDelay),
                errorCode,
                errorMessage,
                maxAttempts),
            cancellationToken);
    }

    private async Task ExecuteLeaseMutationAsync(
        Guid messageId,
        string workerReference,
        Action<OutboxMessage> mutation,
        CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Outbox message id must not be empty.", nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(workerReference);
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var message = await _context.OutboxMessages
                .SingleOrDefaultAsync(candidate => candidate.Id == messageId, cancellationToken)
                ?? throw new InvalidOperationException("Outbox message was not found.");
            mutation(message);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
