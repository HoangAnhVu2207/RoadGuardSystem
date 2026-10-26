namespace RoadGuardSystem.DTOs.Repairs;

public sealed record NotificationSupervisorDto(Guid AssignmentId, Guid SupervisorId, string Status,
    DateTimeOffset AssignedAt, DateTimeOffset? RevokedAt, string Version);
