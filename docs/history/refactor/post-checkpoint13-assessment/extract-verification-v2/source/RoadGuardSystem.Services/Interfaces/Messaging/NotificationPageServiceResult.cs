using RoadGuardSystem.DTOs.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public sealed record NotificationPageServiceResult(
    NotificationServiceStatus Status,
    NotificationPageDto? Page = null);
