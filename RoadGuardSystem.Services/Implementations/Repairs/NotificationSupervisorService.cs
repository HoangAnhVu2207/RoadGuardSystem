using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;
namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class NotificationSupervisorService(INotificationSupervisorRepository repository) : INotificationSupervisorService
{
    public Task<NotificationSupervisorResult> ExecuteAsync(NotificationSupervisorRequest request, CancellationToken ct)
        => repository.ExecuteAsync(request, ct);
}
