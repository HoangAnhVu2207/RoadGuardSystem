namespace RoadGuardSystem.Repositories.Messaging;

public interface IOutboxWorkRepository
{
    Task<OutboxWorkLease?> TryLeaseNextAsync(
        string workerReference,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid messageId,
        string workerReference,
        CancellationToken cancellationToken = default);

    Task RetryAsync(
        Guid messageId,
        string workerReference,
        DateTimeOffset now,
        TimeSpan retryDelay,
        string errorCode,
        string errorMessage,
        int maxAttempts,
        CancellationToken cancellationToken = default);
}
