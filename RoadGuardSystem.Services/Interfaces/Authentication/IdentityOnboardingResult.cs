namespace RoadGuardSystem.Services.Authentication;

public sealed record IdentityOnboardingResult(
    IdentityOnboardingStatus Status,
    RegistrationIntentView? RegistrationIntent = null,
    InvitationView? Invitation = null,
    AuthTokens? Tokens = null);
