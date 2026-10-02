using Microsoft.Data.SqlClient;
using System.Data;
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

    public Task<IdempotencyOperationResult> ExecuteAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(actorUserId, projectId, operation, idempotencyKey, requestFingerprint,
            operationHandler, null, null, cancellationToken);

    /// <param name="receiptAccessGuard">Current authoritative receipt access check, in a service-owned
    /// short transaction on the same scoped DbContext. Called before replay/conflict on every path;
    /// original cancellation token and guard exceptions are preserved. Must only read/lock rows.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last",
        Justification = "Keep the original seven-parameter overload, and place the additive guard after its positional CancellationToken.")]
    public Task<IdempotencyOperationResult> ExecuteAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task>? receiptAccessGuard)
        => ExecuteCoreAsync(actorUserId, projectId, operation, idempotencyKey, requestFingerprint,
            operationHandler, null, receiptAccessGuard, cancellationToken);

    public Task<IdempotencyOperationResult> ExecuteSerializableAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(actorUserId, projectId, operation, idempotencyKey, requestFingerprint,
            operationHandler, IsolationLevel.Serializable, null, cancellationToken);

    // The original seven-parameter overload remains available, including reflection callers.
    /// <param name="receiptAccessGuard">Same receipt guard contract as ExecuteAsync. Serializable
    /// isolation applies to new writes; the guard owns its authoritative read locks in the short
    /// service-owned receipt transaction.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last",
        Justification = "Keep the original seven-parameter overload, and place the additive guard after its positional CancellationToken.")]
    public Task<IdempotencyOperationResult> ExecuteSerializableAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task>? receiptAccessGuard)
        => ExecuteCoreAsync(actorUserId, projectId, operation, idempotencyKey, requestFingerprint,
            operationHandler, IsolationLevel.Serializable, receiptAccessGuard, cancellationToken);

    /// <summary>
    /// A supplied receipt guard is called before every replay/conflict, including retry and recovery.
    /// It must read/lock authoritative rows through this service's scoped DbContext, without opening
    /// or completing a transaction, saving writes, or using cached preflight authority. The service
    /// owns a short transaction inside the execution strategy and holds those locks through mapping
    /// and commit. It passes the original command token (also after acknowledgement loss). Guard
    /// exceptions, including cancellation and otherwise retryable exceptions, escape unchanged;
    /// they never become stored outcomes, conflict results, or create-handler invocations.
    /// Without a guard, the previous replay and transaction-isolation semantics remain unchanged.
    /// </summary>
    private async Task<IdempotencyOperationResult> ExecuteCoreAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        IsolationLevel? isolationLevel,
        Func<CancellationToken, Task>? receiptAccessGuard,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteOperationAsync(actorUserId, projectId, operation, idempotencyKey,
                requestFingerprint, operationHandler, isolationLevel, receiptAccessGuard, cancellationToken);
        }
        catch (ReceiptAccessGuardException exception)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.Cause).Throw();
            throw;
        }
    }

    private async Task<IdempotencyOperationResult> ExecuteOperationAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        IsolationLevel? isolationLevel,
        Func<CancellationToken, Task>? receiptAccessGuard,
        CancellationToken cancellationToken)
    {
        IdempotencyRecord.ValidateScope(operation, idempotencyKey, requestFingerprint);
        ArgumentNullException.ThrowIfNull(operationHandler);

        operation = operation.Trim();
        idempotencyKey = idempotencyKey.Trim();

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        Task<IdempotencyOperationResult?> LookupAsync() => FindAndMapExistingAsync(
            actorUserId, projectId, operation, idempotencyKey, requestFingerprint,
            receiptAccessGuard, cancellationToken);
        var existingResult = receiptAccessGuard is null
            ? await LookupAsync()
            : await executionStrategy.ExecuteAsync(LookupAsync);
        if (existingResult is not null)
        {
            return existingResult;
        }

        try
        {
            return await executionStrategy.ExecuteAsync(
                () => ExecuteAttemptAsync(
                    actorUserId,
                    projectId,
                    operation,
                    idempotencyKey,
                    requestFingerprint,
                    operationHandler,
                    isolationLevel,
                    receiptAccessGuard,
                    cancellationToken));
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var recovered = receiptAccessGuard is null
                ? await LookupAsync()
                : await executionStrategy.ExecuteAsync(LookupAsync);
            if (recovered is null)
            {
                throw;
            }

            return recovered;
        }
    }

    private async Task<IdempotencyOperationResult> ExecuteAttemptAsync(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> operationHandler,
        IsolationLevel? isolationLevel,
        Func<CancellationToken, Task>? receiptAccessGuard,
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
            return await MapAuthorizedAsync(existing, requestFingerprint, receiptAccessGuard, cancellationToken);
        }

        IdempotencyRecord record;
        Exception? commitFailure = null;
        await using (var transaction = isolationLevel is { } selectedIsolation
            ? await _context.Database.BeginTransactionAsync(selectedIsolation, cancellationToken)
            : await _context.Database.BeginTransactionAsync(cancellationToken))
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
            return await MapAuthorizedAsync(durableRecord, requestFingerprint, receiptAccessGuard, cancellationToken);
        }

        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(commitFailure!).Throw();
        throw new InvalidOperationException("Commit recovery did not produce an exception.");
    }

    private async Task<IdempotencyOperationResult?> FindAndMapExistingAsync(
        Guid? actorUserId, Guid? projectId, string operation, string idempotencyKey,
        string requestFingerprint, Func<CancellationToken, Task>? receiptAccessGuard,
        CancellationToken cancellationToken)
    {
        var record = await FindExistingAsync(actorUserId, projectId, operation, idempotencyKey, cancellationToken);
        return record is null ? null : await MapAuthorizedAsync(record, requestFingerprint, receiptAccessGuard, cancellationToken);
    }

    // Called only within an execution-strategy boundary when a guard is supplied.
    private async Task<IdempotencyOperationResult> MapAuthorizedAsync(
        IdempotencyRecord record, string requestFingerprint,
        Func<CancellationToken, Task>? receiptAccessGuard, CancellationToken cancellationToken)
    {
        if (receiptAccessGuard is null)
        {
            return MapExisting(record, requestFingerprint);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await receiptAccessGuard(cancellationToken);
        }
        catch (Exception exception)
        {
            // Do not let a guard's SQL/DbUpdate/transient error enter receipt recovery or retry.
            // Keep the cause outside InnerException so provider retry unwrapping cannot reach it.
            throw new ReceiptAccessGuardException(exception);
        }

        var result = MapExisting(record, requestFingerprint);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private sealed class ReceiptAccessGuardException(Exception cause) : Exception
    {
        public Exception Cause { get; } = cause;
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
