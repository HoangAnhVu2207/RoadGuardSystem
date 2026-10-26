using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

// Internal committed-outbox adapter; it does not invent an external AI transport.
public sealed class AiDispatchSourceRepository(RoadGuardDbContext db) : IAiDispatchSource
{
    public async Task<AiDispatchSource?> ReadCurrentAsync(Guid outboxId, CancellationToken token = default)
    {
        AiDispatchSource? result = null;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var message = await db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == outboxId && x.MessageType == "processing_job.dispatch", token);
            if (message is null) return;
            Guid jobId, attemptId;
            try
            {
                using var json = JsonDocument.Parse(message.PayloadJson);
                if (!json.RootElement.TryGetProperty("jobId", out var job) || !job.TryGetGuid(out jobId) ||
                    !json.RootElement.TryGetProperty("attemptId", out var attempt) || !attempt.TryGetGuid(out attemptId)) return;
            }
            catch (JsonException) { return; }
            var current = await db.ProcessingAttempts.AsNoTracking().Where(x => x.ProcessingJobId == jobId)
                .OrderByDescending(x => x.AttemptNo).FirstOrDefaultAsync(token);
            var source = await db.ProcessingJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == jobId &&
                (x.Status == ProcessingJobStatus.Queued || x.Status == ProcessingJobStatus.Running), token);
            if (source is not null && current?.Id == attemptId && current.EndedAt is null && message.CorrelationId == jobId)
                result = new(jobId, attemptId, source.ManifestJson);
            await tx.CommitAsync(token);
        });
        return result;
    }
}
