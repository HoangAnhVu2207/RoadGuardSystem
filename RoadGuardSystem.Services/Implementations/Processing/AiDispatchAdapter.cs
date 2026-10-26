using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Services.Processing;

public sealed class AiDispatchEnvelope(Guid jobId, Guid attemptId, string manifestJson, string bearer)
{
    public Guid JobId { get; } = jobId;
    public Guid AttemptId { get; } = attemptId;
    public string ManifestJson { get; } = manifestJson;
    public string Bearer { get; } = bearer;
    public override string ToString() => $"AI dispatch {JobId}/{AttemptId} (credential redacted)";
}

// Called by an internal transport adapter only. No mint HTTP route or human credentials.
public sealed class AiDispatchAdapter(IAiDispatchSource source, AiCallbackTokens tokens, AiServiceTokenOptions options)
{
    public async Task<AiDispatchEnvelope?> PrepareAsync(Guid committedOutboxId, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled) return null;
        var current = await source.ReadCurrentAsync(committedOutboxId, cancellationToken);
        return current is null ? null : new(current.JobId, current.AttemptId, current.ManifestJson,
            tokens.Mint(current.JobId, current.AttemptId));
    }
}
