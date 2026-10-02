using RoadGuardSystem.DTOs.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public sealed record NotificationReadServiceResult(
    NotificationServiceStatus Status,
    NotificationDto? Notification = null);
