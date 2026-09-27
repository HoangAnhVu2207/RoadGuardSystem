namespace RoadGuardSystem.Services.Authentication;

public sealed record PasswordRecoveryCommand(
    string Email,
    Guid? CorrelationId = null);
