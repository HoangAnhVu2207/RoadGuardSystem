namespace RoadGuardSystem.DTOs.Authentication;

public sealed record AuthActorResponseDto(
    Guid Id,
    string DisplayName,
    string Role,
    string Version);
