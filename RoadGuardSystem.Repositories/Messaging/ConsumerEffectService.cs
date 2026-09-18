using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;

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

    public async Task<ConsumerEffectResult> RecordAsync(
        Guid messageId,
        string consumerName,
        Guid effectId,
        CancellationToken cancellationToken = default)
    {
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

        try
        {
            var receipt = ConsumerEffectReceipt.Create(
                messageId,
                consumerName,
                effectId,
                DateTimeOffset.UtcNow);
            _context.Set<ConsumerEffectReceipt>().Add(receipt);
            await _context.SaveChangesAsync(cancellationToken);
            return new ConsumerEffectResult(ConsumerEffectStatus.Recorded, receipt.EffectId);
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
