namespace RoadGuardSystem.Services.Authentication;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string IdempotencyKey,
    Guid? CorrelationId = null);
