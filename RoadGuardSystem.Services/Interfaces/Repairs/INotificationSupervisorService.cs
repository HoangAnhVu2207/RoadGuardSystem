using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Services.Repairs;

public interface INotificationSupervisorService
{
    Task<NotificationSupervisorResult> ExecuteAsync(NotificationSupervisorRequest request, CancellationToken ct);
}
