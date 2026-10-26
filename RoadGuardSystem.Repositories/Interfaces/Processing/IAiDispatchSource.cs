namespace RoadGuardSystem.Repositories.Processing;

public sealed record AiDispatchSource(Guid JobId, Guid AttemptId, string ManifestJson);
public interface IAiDispatchSource
{
    Task<AiDispatchSource?> ReadCurrentAsync(Guid outboxId, CancellationToken token = default);
}
