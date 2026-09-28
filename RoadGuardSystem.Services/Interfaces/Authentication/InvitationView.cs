namespace RoadGuardSystem.Services.Authentication;

public sealed record InvitationView(
    Guid Id,
    string Status,
    DateTimeOffset ExpiresAt,
    byte[] RowVersion);
