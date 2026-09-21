using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Transactions;

namespace RoadGuardSystem.Repositories.Messaging;

public enum ConsumerEffectStatus
{
    Recorded = 1,
    Replayed = 2
}

public sealed record ConsumerEffectResult(ConsumerEffectStatus Status, Guid EffectId);

public sealed class ConsumerEffectService
{
    private readonly RoadGuardDbContext _context;

    public ConsumerEffectService(RoadGuardDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Commits one durable database effect and its receipt atomically. The callback must write through
    /// this service's DbContext or another resource enlisted in its current database transaction.
    /// </summary>
    public async Task<ConsumerEffectResult> ProcessAsync(
        Guid messageId,
        string consumerName,
        Guid effectId,
        Func<CancellationToken, Task> durableEffect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(durableEffect);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        if (consumerName.Length > 100)
        {
            throw new ArgumentException("Consumer name exceeds maximum length 100.", nameof(consumerName));
        }

        consumerName = consumerName.Trim();
        var existing = await FindExistingAsync(messageId, consumerName, cancellationToken);
        if (existing is not null)
        {
            return new ConsumerEffectResult(ConsumerEffectStatus.Replayed, existing.EffectId);
        }

        ConsumerEffectResult? result = null;
        try
        {
            var transactionService = new RoadGuardTransactionService(_context);
            await transactionService.ExecuteAsync(
                async operationCancellationToken =>
                {
                    existing = await FindExistingAsync(
                        messageId,
                        consumerName,
                        operationCancellationToken);
                    if (existing is not null)
                    {
                        result = new ConsumerEffectResult(ConsumerEffectStatus.Replayed, existing.EffectId);
                        return;
                    }

                    var receipt = ConsumerEffectReceipt.Create(
                        Guid.NewGuid(),
                        messageId,
                        consumerName,
                        effectId,
                        DateTimeOffset.UtcNow);
                    _context.Set<ConsumerEffectReceipt>().Add(receipt);
                    await durableEffect(operationCancellationToken);
                    result = new ConsumerEffectResult(ConsumerEffectStatus.Recorded, receipt.EffectId);
                },
                cancellationToken);

            return result ?? throw new InvalidOperationException("Consumer effect transaction produced no result.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            existing = await FindExistingAsync(messageId, consumerName, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return new ConsumerEffectResult(ConsumerEffectStatus.Replayed, existing.EffectId);
        }
    }

    private Task<ConsumerEffectReceipt?> FindExistingAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken)
        => _context.Set<ConsumerEffectReceipt>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt => receipt.MessageId == messageId && receipt.ConsumerName == consumerName,
                cancellationToken);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
