namespace RoadGuardSystem.Services.Authentication;

public sealed record RegistrationIntentView(
    Guid Id,
    int ResendAfterSeconds);
