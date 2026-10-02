using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Idempotency;

public sealed class IdempotencyOperationService
{
    private readonly RoadGuardDbContext _context;

    public IdempotencyOperationService(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyOperationResult> ExecuteAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken = default)
    {
        IdempotencyRecord.ValidateScope(operation, idempotencyKey, requestFingerprint);
        ArgumentNullException.ThrowIfNull(operationHandler);

        operation = operation.Trim();
        idempotencyKey = idempotencyKey.Trim();

        var existing = await FindExistingAsync(
            actorUserId,
            projectId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return MapExisting(existing, requestFingerprint);
        }

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(
                () => ExecuteAttemptAsync(
                    actorUserId,
                    projectId,
                    operation,
                    idempotencyKey,
                    requestFingerprint,
                    operationHandler,
                    cancellationToken));
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            existing = await FindExistingAsync(
                actorUserId,
                projectId,
                operation,
                idempotencyKey,
                cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return MapExisting(existing, requestFingerprint);
        }
    }

    private async Task<IdempotencyOperationResult> ExecuteAttemptAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken)
    {
        // A commit acknowledgement can fail after the database transaction is durable. Each retry must
        // discard stale tracked state and prefer the durable outcome over invoking the handler again.
        _context.ChangeTracker.Clear();
        var existing = await FindExistingAsync(
            actorUserId,
            projectId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return MapExisting(existing, requestFingerprint);
        }

        IdempotencyRecord record;
        Exception? commitFailure = null;
        await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var outcome = await operationHandler(cancellationToken);
                record = IdempotencyRecord.Create(
                    actorUserId,
                    projectId,
                    operation,
                    idempotencyKey,
                    requestFingerprint,
                    outcome.OperationId,
                    outcome.OutcomeJson,
                    DateTimeOffset.UtcNow);
                _context.Set<IdempotencyRecord>().Add(record);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }

            try
            {
                await transaction.CommitAsync(cancellationToken);
                return new IdempotencyOperationResult(
                    IdempotencyOperationStatus.Executed,
                    record.OperationId,
                    record.OutcomeJson,
                    record.ActorUserId,
                    record.ProjectId,
                    record.Operation);
            }
            catch (Exception exception)
            {
                commitFailure = exception;
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch
                {
                    // The transaction may already be committed. Its durable state is checked below.
                }
            }
        }

        // A post-commit acknowledgement failure is indistinguishable from a failed commit until the
        // transaction is disposed. Query only after that boundary so uncommitted local writes cannot
        // be mistaken for a durable replay.
        _context.ChangeTracker.Clear();
        var durableRecord = await FindExistingAsync(
            actorUserId,
            projectId,
            operation,
            idempotencyKey,
            CancellationToken.None);
        if (durableRecord is not null)
        {
            return MapExisting(durableRecord, requestFingerprint);
        }

        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(commitFailure!).Throw();
        throw new InvalidOperationException("Commit recovery did not produce an exception.");
    }

    private Task<IdempotencyRecord?> FindExistingAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => _context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record =>
                    record.ActorUserId == actorUserId &&
                    record.ProjectId == projectId &&
                    record.Operation == operation &&
                    record.IdempotencyKey == idempotencyKey,
                cancellationToken);

    private static IdempotencyOperationResult MapExisting(
        IdempotencyRecord record,
        string requestFingerprint)
        => new(
            record.RequestFingerprint == requestFingerprint
                ? IdempotencyOperationStatus.Replayed
                : IdempotencyOperationStatus.Conflict,
            record.OperationId,
            record.OutcomeJson,
            record.ActorUserId,
            record.ProjectId,
            record.Operation);

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
