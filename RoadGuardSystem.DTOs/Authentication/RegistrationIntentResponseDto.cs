namespace RoadGuardSystem.DTOs.Authentication;

public sealed record RegistrationIntentResponseDto(
    Guid IntentId,
    string Status,
    int ResendAfterSeconds);
