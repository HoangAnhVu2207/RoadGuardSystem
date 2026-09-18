using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace RoadGuardSystem.Repositories.Transactions;

public sealed class RoadGuardTransactionService
{
    private readonly RoadGuardDbContext _context;

    public RoadGuardTransactionService(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_context.Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }
}
