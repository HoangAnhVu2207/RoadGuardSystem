using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

public sealed partial class AiVideoWorkRepository
{
    // Each retry owns a fresh tracker. An aborted insert must not survive into the
    // next attempt, and this worker must never save unrelated caller mutations.
    private async Task<T> RunTransactionAsync<T>(IsolationLevel isolation, Func<AiVideoWorkRepository, Task<T>> body, CancellationToken ct)
    {
        var options = (DbContextOptions)db.GetService<IDbContextOptions>();
        List<WriteReceipt>? receipt = null;
        T result = default!;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var attempt = new RoadGuardDbContext(options);
            if (receipt is not null)
            {
                // A dropped commit response is not evidence of a rollback. Compare
                // every generated identity and row version before replaying writes.
                var committed = true;
                var rolledBack = true;
                foreach (var write in receipt)
                {
                    var entity = await attempt.FindAsync(write.Type, write.Key, ct);
                    var current = entity is null ? null : attempt.Entry(entity).CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => attempt.Entry(entity).Property(p.Name).CurrentValue);
                    committed &= SameValues(current, write.After);
                    rolledBack &= SameValues(current, write.Before);
                }
                if (committed) return result;
                if (!rolledBack)
                    throw new InvalidOperationException("Ambiguous REAL worker commit changed concurrently; reconcile durable state before retrying.");
                attempt.ChangeTracker.Clear();
                receipt = null;
            }

            await using var transaction = await attempt.Database.BeginTransactionAsync(isolation, ct);
            result = await body(new AiVideoWorkRepository(attempt));
            attempt.ChangeTracker.DetectChanges();
            var changed = attempt.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified).ToArray();
            var before = changed.Select(e => e.State == EntityState.Added ? null : e.OriginalValues.Properties
                .ToDictionary(p => p.Name, p => CopyValue(e.OriginalValues[p.Name]))).ToArray();
            await attempt.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken: ct);
            var pendingReceipt = new List<WriteReceipt>();
            for (var i = 0; i < changed.Length; i++)
            {
                var entry = changed[i];
                // SQL rounds decimal precision and can normalize other scalars.
                // Read inside this transaction so the receipt matches stored values.
                var stored = await entry.GetDatabaseValuesAsync(ct)
                    ?? throw new InvalidOperationException("Worker write has no persisted after-image.");
                pendingReceipt.Add(new WriteReceipt(entry.Metadata.ClrType,
                    entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray(), before[i],
                    stored.Properties.ToDictionary(p => p.Name, p => CopyValue(stored[p.Name]))));
            }
            receipt = pendingReceipt;
            await transaction.CommitAsync(ct);
            attempt.ChangeTracker.AcceptAllChanges();
            return result;
        });
    }

    private sealed record WriteReceipt(Type Type, object?[] Key, Dictionary<string, object?>? Before, Dictionary<string, object?> After);
    private static object? CopyValue(object? value) => value is byte[] bytes ? bytes.ToArray() : value;
    private static bool SameValues(Dictionary<string, object?>? current, Dictionary<string, object?>? expected)
        => current is null || expected is null ? current is null && expected is null :
            current.Count == expected.Count && expected.All(p => current.TryGetValue(p.Key, out var actual) &&
                (actual is byte[] a && p.Value is byte[] b ? a.SequenceEqual(b) : Equals(actual, p.Value)));
}
