using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record NotificationSupervisorRequest(Guid ActorId, UserRoleCode Role, Guid ProjectId,
    Guid PackageId, Guid ItemId, string Method, string? Key = null, string? IfMatch = null,
    string? IfNoneMatch = null, string Body = "");
public sealed record NotificationSupervisorReadRecord(Guid AssignmentId, Guid SupervisorId, string Status,
    DateTimeOffset AssignedAt, DateTimeOffset? RevokedAt, string Version);
public sealed record NotificationSupervisorResult(int Status, string? Code = null,
    NotificationSupervisorReadRecord? Value = null, string? Version = null);
public interface INotificationSupervisorRepository
{
    Task<NotificationSupervisorResult> ExecuteAsync(NotificationSupervisorRequest request, CancellationToken ct);
}
