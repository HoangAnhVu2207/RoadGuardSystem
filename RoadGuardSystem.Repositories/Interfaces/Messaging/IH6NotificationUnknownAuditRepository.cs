namespace RoadGuardSystem.Repositories.Messaging;

// This finite diagnostic scan is independent from leasing and never acknowledges a source event.
public interface IH6NotificationUnknownAuditRepository
{
    Task<int> AuditUnregisteredAsync(CancellationToken cancellationToken);
}
