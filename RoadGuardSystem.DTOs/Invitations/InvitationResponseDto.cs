namespace RoadGuardSystem.DTOs.Invitations;

public sealed record InvitationResponseDto(
    Guid Id,
    string Status,
    DateTimeOffset ExpiresAt,
    string Version);
