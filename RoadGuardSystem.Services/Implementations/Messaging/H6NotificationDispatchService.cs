using RoadGuardSystem.Repositories.Messaging;

namespace RoadGuardSystem.Services.Messaging;

// The repository owns the source-proof, current-recipient, fenced lease and atomic receipt boundary.
public sealed class H6NotificationDispatchService(IH6NotificationDispatchRepository repository)
{
    public async Task<H6DispatchOutcome> ProcessOneAsync(CancellationToken cancellationToken = default)
    {
        var claim = await repository.ClaimAsync(H6NotificationCatalog.MessageTypes, cancellationToken);
        if (claim is null) return new H6DispatchOutcome("IDLE");
        H6DispatchPlan plan;
        try
        {
            plan = H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson);
        }
        catch (H6NotificationProtocolException exception)
        {
            return await repository.RejectAsync(claim, exception.Code, cancellationToken);
        }
        return await repository.DispatchAsync(claim, plan, cancellationToken);
    }
}
